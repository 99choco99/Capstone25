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
    [Tooltip("시야 거리")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 15f;
    [Tooltip("전체 시야각")]
    [SerializeField, Range(0f, 360f)] private float detectionAngle = 90f;
    [Tooltip("시야 판정 프레임 간격")]
    [SerializeField, Min(0.1f)] private float senseInterval = 0.1f;
    [Tooltip("장애물 레이어")]
    [SerializeField] private LayerMask obstacleLayer;

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

    /// <summary>AI와 State가 이번 프레임에 사용할 감지 결과</summary>
    public EnemyTargetInfo CurrentTargetInfo
    {
        get
        {
            float distance = IsAlerted ? Vector3.Distance(transform.position, LastTargetPosition) : float.PositiveInfinity;

            return new EnemyTargetInfo(IsAlerted, LastTargetPosition, distance, CanSeeTarget);
        }
    }

    private Player combatTarget;
    private Transform targetTransform;
    private float loseTargetTimer;
    private float nextSenseTime;
    private float minimumViewDot;

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
        if (combatTarget == null || combatTarget.Stats == null || combatTarget.Stats.IsDead)
        {
            ClearAlert();
            return;
        }

        if (senseInterval <= 0f || Time.time >= nextSenseTime)
        {
            EvaluateVisibility();
            nextSenseTime = Time.time + senseInterval;
        }

        if (CanSeeTarget)
        {
            LastTargetPosition = combatTarget.transform.position;
            loseTargetTimer = loseTargetTime;
            IsAlerted = true;
            return;
        }
        else if (IsAlerted)
        {
            loseTargetTimer -= Time.deltaTime;
            if (loseTargetTimer <= 0f)
                ClearAlert();
        }
    }

    /// <summary>
    /// 플레이어 감지 함수
    /// </summary>
    private void EvaluateVisibility()
    {
        CanSeeTarget = CanSeeCombatTarget();

        if (!CanSeeTarget)
            return;

        IsAlerted = true;
        if (combatTarget != null)
        {
            LastTargetPosition = combatTarget.transform.position;
        }
        loseTargetTimer = loseTargetTime;
    }

    /// <summary>
    /// 장애물이 앞에 있는지 검사
    /// </summary>
    private bool CanSeeCombatTarget()
    {
        Vector3 eyePosition = eyeTransform.position;
        Vector3 targetPosition = targetTransform.position;
        Vector3 direction = targetPosition - eyePosition;

        //거리 벗어났는지 확인
        float sqrDistance = direction.sqrMagnitude;
        float maxSqrDistance = detectionRadius * detectionRadius;
        if (sqrDistance < 0.0001f || sqrDistance > maxSqrDistance)
            return false;


        if (!IsAlerted)
        {
            float viewDot = Vector3.Dot(transform.forward, direction.normalized);
            if (viewDot < minimumViewDot)
                return false;
        }

        return !IsPathBlocked(targetPosition);
    }

    /// <summary>
    /// 두 지점 사이에 장애물이 있는지 검사
    /// </summary>
    public bool IsPathBlocked(Vector3 targetPosition)
    {
        Vector3 origin = eyeTransform != null ? eyeTransform.position: transform.position + Vector3.up;

        return Physics.Linecast(origin,targetPosition, obstacleLayer, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// 인지상태 초기화
    /// </summary>
    private void ClearAlert()
    {
        IsAlerted = false;
        CanSeeTarget = false;
        loseTargetTimer = 0f;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
