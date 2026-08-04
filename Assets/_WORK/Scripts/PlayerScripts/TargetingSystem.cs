using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(PlayerExecution))]
public class TargetingSystem : MonoBehaviour
{
    [Header("검색 레이어")]
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("락온 검색")]
    [Tooltip("락온을 허용하는 반각")]
    [SerializeField, Range(0f, 180f)] private float cameraHalfFov = 45f;
    [Tooltip("락온 최대 거리")]
    [SerializeField, Min(0.1f)] private float lockOnRange = 10f;

    [Header("인살 검색")]
    [Tooltip("인살 가능 타겟 검색 반경")]
    [SerializeField, Min(0.1f)] private float nearbyDeathblowSearchRadius = 1.8f;

    [Header("타겟 전환")]
    [SerializeField, Min(0f)] private float targetSwitchCooldown = 0.25f;
    [SerializeField, Min(0f)] private float minimumSwitchAngle = 25f;
    [Tooltip("스틱/마우스가 이 값을 넘을 때 한 번만 타깃을 전환")]
    [SerializeField, Range(0f, 1f)] private float targetSwitchThreshold = 0.5f;
    [Tooltip("다음 타깃 전환을 허용하기 위해 입력이 돌아와야 하는 값")]
    [SerializeField, Range(0f, 1f)] private float targetSwitchRearmThreshold = 0.2f;

    [Header("락온 유지")]
    [Tooltip("포착 거리보다 약간 멀어져도 락온을 유지하는 거리 배율")]
    [SerializeField, Min(1f)] private float unlockRangeMultiplier = 1.15f;
    [Tooltip("장애물에 잠시 가려져도 락온을 유지하는 시간")]
    [SerializeField, Min(0f)] private float occlusionGraceTime = 0.35f;

    [Header("후보 점수")]
    [SerializeField, Min(0f)] private float distanceWeight = 1f;
    [SerializeField, Min(0f)] private float angleWeight = 0.2f;

    /// <summary>
    /// 락온 대상이 변할때 호출
    /// </summary>
    public event Action<ITargetable> TargetChanged;

    private ITargetable currentTarget;

    public ITargetable CurrentTarget => IsNull(currentTarget) ? null : currentTarget;
    public bool HasTarget => CurrentTarget != null;

    private readonly Collider[] overlapBuffer = new Collider[20];
    private readonly List<ITargetable> validTargets = new();
    private readonly HashSet<Enemy> canDeathblowTargets = new();

    private Transform cameraTransform;
    private PlayerExecution playerExecution;
    private float lastSwitchTime;
    private float occludedDuration;
    private bool switchInputArmed = true;

    [Header("인살 UI 갱신")]
    [SerializeField, Min(0f)] private float deathblowUiRefreshInterval = 0.12f;
    private DeathblowPlan cachedUiPlan;
    private bool cachedUiPlanValid;
    private float lastUiEvalTime = float.NegativeInfinity;

    private void Awake()
    {
        ConnectCamera();
        cameraHalfFov = Mathf.Cos(cameraHalfFov * Mathf.Deg2Rad);
        playerExecution = GetComponent<PlayerExecution>();
    }

    private void Update()
    {
        if (currentTarget != null)
            ValidateCurrentTarget();
    }

    /// <summary>
    /// 현재 타켓이 유효한지 검사
    /// </summary>
    private void ValidateCurrentTarget()
    {
        ITargetable target = CurrentTarget;

        if (target == null || target.IsDead)
        {
            DeselectTarget();
            return;
        }

        float unlockRange = lockOnRange * unlockRangeMultiplier;
        float maxDistance = unlockRange * unlockRange;
        float distance = (transform.position - target.TargetTransform.position).sqrMagnitude;
        if (distance > maxDistance)
        {
            DeselectTarget();
            return;
        }

        if (!ConnectCamera())
            return;

        if (!IsOccluded(target))
        {
            occludedDuration = 0f;
            return;
        }

        occludedDuration += Time.deltaTime;
        if (occludedDuration >= occlusionGraceTime)
            DeselectTarget();
    }


    /// <summary>
    /// 락온 on/off 함수
    /// </summary>
    public void ToggleTarget()
    {
        if (CurrentTarget != null)
        {
            DeselectTarget();
            return;
        }

        ITargetable target = FindBestTarget(0f);
        if (target != null)
            SelectTarget(target);
    }

    /// <summary>
    /// 타겟 변경
    /// </summary>
    public void UpdateTargetSwitch(float searchDirection)
    {
        float inputMagnitude = Mathf.Abs(searchDirection);
        if (inputMagnitude <= targetSwitchRearmThreshold)
        {
            switchInputArmed = true;
            return;
        }

        if (CurrentTarget == null || !switchInputArmed) return;
        if (inputMagnitude < targetSwitchThreshold) return;

        // 임계값을 한 번 넘긴 입력은 쿨다운 중이어도 소비한다.
        // 중립으로 돌아오기 전에는 누르고 있는 입력이 반복 전환되지 않는다.
        switchInputArmed = false;
        if (Time.time < lastSwitchTime + targetSwitchCooldown) return;

        ITargetable nextTarget = FindBestTarget(Mathf.Sign(searchDirection));
        if (nextTarget != null)
            SelectTarget(nextTarget);
    }


    /// <summary>
    /// validTarget 중에서 가장 적합한 타겟 선정
    /// </summary>
    private ITargetable FindBestTarget(float searchDirection)
    {
        if (!ConnectCamera()) return null;

        CollectValidTargets();
        ITargetable bestTarget = null;
        float bestScore = float.PositiveInfinity;

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        foreach (ITargetable target in validTargets)
        {
            if (target == CurrentTarget) continue;

            Vector3 direction = target.TargetTransform.position - cameraTransform.position;
            direction.y = 0f;
            float distance = direction.magnitude;
            float angle = Vector3.SignedAngle(cameraForward, direction, Vector3.up);

            if (searchDirection > 0f && angle < minimumSwitchAngle) continue;
            if (searchDirection < 0f && angle > -minimumSwitchAngle) continue;

            float score = distance * distanceWeight + Mathf.Abs(angle) * angleWeight;
            if (score >= bestScore) continue;

            bestScore = score;
            bestTarget = target;
        }

        return bestTarget;
    }

    /// <summary>
    /// 락온 가능한 타겟들 판단
    /// </summary>
    private void CollectValidTargets()
    {
        validTargets.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, lockOnRange, overlapBuffer, targetLayer, QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider candidateCollider = overlapBuffer[i];
            if (candidateCollider == null) continue;

            candidateCollider.TryGetComponent(out ITargetable target);
            if (target == null || target.IsDead) continue;

            //시야 범위 외 제외
            Vector3 direction = target.TargetTransform.position - cameraTransform.position;
            if (Vector3.Dot(cameraTransform.forward, direction.normalized) < cameraHalfFov) continue;

            //장애물 있을 시 제외
            if (IsOccluded(target)) continue;

            validTargets.Add(target);
        }
    }

    /// <summary>
    /// 락온 타겟으로 선택
    /// </summary>
    private void SelectTarget(ITargetable target)
    {
        if (target == null || target.IsDead || CurrentTarget == target) return;

        currentTarget = target;
        lastSwitchTime = Time.time;
        occludedDuration = 0f;
        switchInputArmed = false;
        TargetChanged?.Invoke(CurrentTarget);
    }

    /// <summary>
    /// 락온 지정 해제
    /// </summary>
    public void DeselectTarget()
    {
        if (ReferenceEquals(currentTarget, null)) return;

        currentTarget = null;
        occludedDuration = 0f;
        switchInputArmed = true;
        TargetChanged?.Invoke(null);
    }

    //==================인살 관련 타겟팅 =============================

    /// <summary>
    /// 인살 계획 반환
    /// 락온 대상이 가능하면 우선. 없다면 주변 후보 중 가장 가까운 대상을 선택
    /// </summary>
    public bool GetDeathblowPlan(out DeathblowPlan plan) => EvaluateDeathblowPlan(out plan);

    /// <summary>
    /// UI에서 인살계획 가져감
    /// </summary>
    public bool GetDeathblowPlanForUI(out DeathblowPlan plan)
    {
        if (Time.time - lastUiEvalTime >= deathblowUiRefreshInterval)
        {
            cachedUiPlanValid = EvaluateDeathblowPlan(out cachedUiPlan);
            lastUiEvalTime = Time.time;
        }
        plan = cachedUiPlan;
        return cachedUiPlanValid;
    }


    /// <summary>
    /// 인살 가능한 녀석들 검색함
    /// </summary>
    private bool EvaluateDeathblowPlan(out DeathblowPlan plan)
    {
        plan = default;
        if (playerExecution.IsExecuting) return false;

        if (CurrentTarget is Enemy lockedEnemy && playerExecution.CreateDeathblowPlan(lockedEnemy, out plan))
        {
            return true;
        }

        //락온이 아닌놈들
        canDeathblowTargets.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, nearbyDeathblowSearchRadius, overlapBuffer, targetLayer, QueryTriggerInteraction.Collide);

        bool found = false;
        float bestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider candidateCollider = overlapBuffer[i];
            if (candidateCollider == null) continue;

            Enemy enemy = candidateCollider.GetComponentInParent<Enemy>();
            if (enemy == null || !canDeathblowTargets.Add(enemy)) continue;

            float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance) continue;

            if (!playerExecution.CreateDeathblowPlan(enemy, out DeathblowPlan candidate))
                continue;

            bestSqrDistance = sqrDistance;
            plan = candidate;
            found = true;
        }

        return found;
    }



    //================유틸리티=======================

    /// <summary>
    /// 카메라 연결
    /// </summary>
    private bool ConnectCamera()
    {
        if (cameraTransform != null) return true;

        UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
        if (mainCamera == null) return false;

        cameraTransform = mainCamera.transform;
        return true;
    }

    private bool IsOccluded(ITargetable target)
    {
        Transform aimPoint = target.LockOnPoint != null
            ? target.LockOnPoint
            : target.TargetTransform;

        return Physics.Linecast(
            cameraTransform.position,
            aimPoint.position,
            obstacleLayer,
            QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// NULL 인지 아닌지 판별
    /// </summary>
    private static bool IsNull(ITargetable target) => (target as UnityEngine.Object) == null;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, nearbyDeathblowSearchRadius);
    }
}
