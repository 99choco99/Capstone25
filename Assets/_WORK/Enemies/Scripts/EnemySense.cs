using UnityEngine;

/// <summary>
/// 현재 전투 대상의 상태를 전달하는 구조체
/// </summary>
public readonly struct EnemyTargetInfo
{
    public bool HasTarget { get; }
    public Vector3 TargetPosition { get; }
    public float Distance { get; }
    public bool CanSeeTarget { get; }

    public EnemyTargetInfo(bool hasTarget, Vector3 targetPosition, float distance, bool canSeeTarget)
    {
        HasTarget = hasTarget;
        TargetPosition = targetPosition;
        Distance = distance;
        CanSeeTarget = canSeeTarget;
    }
}

public class EnemySense : MonoBehaviour
{
    [Header("시야 기준점")]
    [SerializeField] private Transform eyeTransform;

    [Header("시야 판정")]
    [Tooltip("최초 발견 시야 거리")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 16f;
    [Tooltip("이미 인지한 대상의 시각 추적 거리. 최초 발견 거리보다 짧게 적용하지 않습니다.")]
    [SerializeField, Min(0f)] private float alertedDetectionRadius = 30f;
    [Tooltip("전체 시야각")]
    [SerializeField, Range(0f, 360f)] private float detectionAngle = 150f;
    [Tooltip("시야 판정 프레임 간격")]
    [SerializeField, Min(0.1f)] private float senseInterval = 0.1f;
    [Tooltip("장애물 레이어")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("시각 인지")]
    [Tooltip("가까운 정면의 대상을 확신하는 시간. 0이면 시야에 들어온 즉시 인지합니다.")]
    [SerializeField, Min(0f)] private float visualNoticeDelay = 0.2f;
    [Tooltip("시야의 최대 거리에서 대상을 확신하는 시간")]
    [SerializeField, Min(0f)] private float distantNoticeDelay = 1.8f;
    [Tooltip("가까운 정면의 인지 시간을 적용하는 거리")]
    [SerializeField, Min(0f)] private float closeRecognitionDistance = 4f;
    [Tooltip("시야 가장자리에서 인지하는 데 걸리는 시간 배율")]
    [SerializeField, Min(1f)] private float peripheralNoticeMultiplier = 1.6f;
    [Tooltip("시야를 벗어나면 초당 감소하는 인지 진행도")]
    [SerializeField, Min(0f)] private float awarenessDecay = 0.65f;

    [Header("근거리 기척 감지")]
    [Tooltip("시야각 밖에서도 기척을 알아차리는 거리. 0이면 사용하지 않습니다.")]
    [SerializeField, Min(0f)] private float nearbyDetectionRadius = 2.8f;
    [Tooltip("가까운 곳에 가만히 있는 플레이어를 알아차리는 시간")]
    [SerializeField, Min(0.05f)] private float nearbyNoticeDelay = 1.5f;
    [Tooltip("가까운 곳에서 발걸음이 이어질 때 알아차리는 시간")]
    [SerializeField, Min(0.05f)] private float movingNoticeDelay = 0.8f;
    [Tooltip("발걸음으로 볼 최소 수평 이동 속도. 이동 입력이 아니라 실제 이동으로 판단합니다.")]
    [SerializeField, Min(0.01f)] private float footstepSpeedThreshold = 0.15f;

    [Header("대상 기억")]
    [Tooltip("마지막 목격 위치를 기억하는 시간")]
    [SerializeField, Min(0f)] private float loseTargetTime = 3f;

    /// <summary>현재 플레이어가 보이는지</summary>
    public bool CanSeeTarget { get; private set; }

    /// <summary>
    /// 적이 플레이어를 인지하고 있는지
    /// </summary>
    public bool IsAlerted { get; private set; }

    /// <summary>
    /// 플레이어의 마지막 위치
    /// </summary>
    public Vector3 LastTargetPosition { get; private set; }

    /// <summary>발견 확정 전의 인지 진행도. 인지한 대상은 1입니다.</summary>
    public float Awareness => IsAlerted ? 1f : Mathf.Max(visualAwareness, nearbyAwareness);
    public bool IsSuspicious => !IsAlerted && visualAwareness >= 0.2f;
    public Vector3 LastSuspiciousPosition { get; private set; }

    /// <summary>AI와 State가 이번 프레임에 사용할 감지 결과</summary>
    public EnemyTargetInfo CurrentTargetInfo
    {
        get
        {
            Vector3 direction = LastTargetPosition - transform.position;
            direction.y = 0f;
            float distance = IsAlerted ? direction.magnitude : float.PositiveInfinity;

            return new EnemyTargetInfo(IsAlerted, LastTargetPosition, distance, CanSeeTarget);
        }
    }

    private Player combatTarget;
    private Transform targetTransform;
    private float loseTargetTimer;
    private float nextSenseTime;
    private float minimumViewDot;
    private bool canNoticeNearbyTarget;
    private float nearbyAwareness;
    private float visualAwareness;
    private float sampledVisualNoticeDelay;
    private EnemyTerritory territory;

    private Vector3 EyePosition => eyeTransform != null ? eyeTransform.position : transform.position + Vector3.up;
    private float CurrentDetectionRadius => IsAlerted
        ? Mathf.Max(detectionRadius, alertedDetectionRadius)
        : detectionRadius;

    internal void SetTerritory(EnemyTerritory value) => territory = value;

    internal void ForgetTarget()
    {
        ClearAlert();
        nextSenseTime = Time.time;
    }

    private void Awake()
    {
        minimumViewDot = Mathf.Cos(detectionAngle * 0.5f * Mathf.Deg2Rad);
    }

    private void OnEnable()
    {
        Player.OnLocalPlayerSpawned += BindCombatTarget;
        if (Player.LocalPlayer != null)
            BindCombatTarget(Player.LocalPlayer);
    }

    private void OnDisable()
    {
        Player.OnLocalPlayerSpawned -= BindCombatTarget;
        combatTarget = null;
        targetTransform = null;
        ClearAlert();
    }

    /// <summary>
    /// 로컬 플레이어를 전투 대상으로 연결
    /// </summary>
    private void BindCombatTarget(Player player)
    {
        if (player == null || !player.IsLocalPlayer)
            return;

        if (combatTarget == player)
            return;

        combatTarget = player;
        targetTransform = player.cameraRoot;

        ClearAlert();
        nextSenseTime = Time.time + senseInterval;
    }

    private void Update()
    {
        if (combatTarget == null || combatTarget.Stats == null || combatTarget.Stats.IsDead
            || !CanTrackTarget(combatTarget.transform.position))
        {
            ClearAlert();
            return;
        }

        if (senseInterval <= 0f || Time.time >= nextSenseTime)
        {
            CanSeeTarget = CanSeeCombatTarget(out canNoticeNearbyTarget);
            nextSenseTime = Time.time + senseInterval;
        }

        if (CanSeeTarget)
        {
            if (IsAlerted || sampledVisualNoticeDelay <= 0f)
            {
                Alert(combatTarget.transform.position);
            }
            else
            {
                LastSuspiciousPosition = combatTarget.transform.position;
                visualAwareness = Mathf.Clamp01(visualAwareness + Time.deltaTime / sampledVisualNoticeDelay);
                nearbyAwareness = 0f;
                if (visualAwareness >= 1f)
                    Alert(combatTarget.transform.position);
            }
        }
        else if (IsAlerted)
        {
            loseTargetTimer -= Time.deltaTime;
            if (loseTargetTimer <= 0f)
                ClearAlert();
        }
        else
        {
            visualAwareness = Mathf.MoveTowards(visualAwareness, 0f, awarenessDecay * Time.deltaTime);
            if (canNoticeNearbyTarget && IsWithinNearbyRange())
            {
                nearbyAwareness += Time.deltaTime / GetNearbyNoticeDelay();
                if (nearbyAwareness >= 1f)
                    Alert(combatTarget.transform.position);
            }
            else
            {
                nearbyAwareness = 0f;
            }
        }
    }

    /// <summary>공격자를 인지하고 위치와 기억 시간을 갱신</summary>
    public void Alert(Vector3 targetPosition)
    {
        if (!CanTrackTarget(targetPosition))
            return;

        IsAlerted = true;
        LastTargetPosition = targetPosition;
        loseTargetTimer = loseTargetTime;
        canNoticeNearbyTarget = false;
        nearbyAwareness = 0f;
        visualAwareness = 0f;
    }

    private bool CanTrackTarget(Vector3 position)
    {
        return territory == null || !territory.isActiveAndEnabled || territory.CanTrackTarget(position, IsAlerted);
    }

    /// <summary>
    /// 플레이어 감지 함수
    /// 장애물이 앞에 있는지 검사
    /// </summary>
    private bool CanSeeCombatTarget(out bool canNoticeNearby)
    {
        canNoticeNearby = false;
        Vector3 eyePosition = EyePosition;
        Vector3 targetPosition = targetTransform != null
            ? targetTransform.position
            : combatTarget.transform.position + Vector3.up;
        Vector3 direction = targetPosition - eyePosition;

        //거리 벗어났는지 확인
        float sqrDistance = direction.sqrMagnitude;
        float radius = CurrentDetectionRadius;
        float maxSqrDistance = radius * radius;
        bool withinView = !(sqrDistance < 0.0001f || sqrDistance > maxSqrDistance);
        float viewDot = 1f;

        if (withinView && !IsAlerted)
        {
            viewDot = Vector3.Dot(transform.forward, direction.normalized);
            if (viewDot < minimumViewDot)
                withinView = false;
        }

        bool withinNearbyRange = !IsAlerted && IsWithinNearbyRange();
        if (!withinView && !withinNearbyRange)
            return false;

        bool pathClear = !IsPathBlocked(targetPosition);
        if (withinView && pathClear && !IsAlerted)
            sampledVisualNoticeDelay = GetVisualNoticeDelay(Mathf.Sqrt(sqrDistance), viewDot);
        canNoticeNearby = !withinView && withinNearbyRange && pathClear;
        return withinView && pathClear;
    }

    private float GetVisualNoticeDelay(float distance, float viewDot)
    {
        if (visualNoticeDelay <= 0f)
            return 0f;

        float nearDistance = Mathf.Min(closeRecognitionDistance, detectionRadius);
        float distanceRatio = Mathf.InverseLerp(nearDistance, detectionRadius, distance);
        float delay = Mathf.Lerp(visualNoticeDelay, Mathf.Max(visualNoticeDelay, distantNoticeDelay), distanceRatio);
        float peripheralRatio = Mathf.InverseLerp(1f, minimumViewDot, viewDot);
        return Mathf.Max(0.05f, delay * Mathf.Lerp(1f, peripheralNoticeMultiplier, peripheralRatio));
    }

    private bool IsWithinNearbyRange()
    {
        return nearbyDetectionRadius > 0f
            && (combatTarget.transform.position - transform.position).sqrMagnitude <= nearbyDetectionRadius * nearbyDetectionRadius;
    }

    private float GetNearbyNoticeDelay()
    {
        PlayerMotor motor = combatTarget.Motor;
        if (motor != null && motor.IsGrounded && motor.Controller != null && motor.Controller.enabled)
        {
            Vector3 velocity = motor.Controller.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > footstepSpeedThreshold * footstepSpeedThreshold)
                return Mathf.Max(0.05f, movingNoticeDelay);
        }

        return Mathf.Max(0.05f, nearbyNoticeDelay);
    }

    /// <summary>
    /// 두 지점 사이에 장애물이 있는지 검사
    /// </summary>
    private bool IsPathBlocked(Vector3 targetPosition)
    {
        Vector3 origin = EyePosition;

        return Physics.Linecast(origin, targetPosition, obstacleLayer, QueryTriggerInteraction.Ignore);
    }

    public bool HasClearPathTo(Vector3 targetPosition) => !IsPathBlocked(targetPosition);

    /// <summary>
    /// 인지상태 초기화
    /// </summary>
    private void ClearAlert()
    {
        IsAlerted = false;
        CanSeeTarget = false;
        loseTargetTimer = 0f;
        canNoticeNearbyTarget = false;
        nearbyAwareness = 0f;
        visualAwareness = 0f;
        sampledVisualNoticeDelay = 0f;
        LastSuspiciousPosition = default;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, CurrentDetectionRadius);
        if (nearbyDetectionRadius > 0f)
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, nearbyDetectionRadius);
        }
    }
}
