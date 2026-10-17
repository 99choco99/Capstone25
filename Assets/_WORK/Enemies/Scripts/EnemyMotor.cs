using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy의 움직임을 정의
/// </summary>
[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMotor : MonoBehaviour
{
    private EnemyData data;
    private Enemy owner;

    private NavMeshAgent navAgent;
    private float defaultAcceleration;
    public float BodyRadius => navAgent.radius;

    private NavMeshPath returnPath;
    private bool hasChaseProgress;
    private bool chaseRequested;
    private Vector3 chaseProgressPosition;
    private Vector3 chaseProgressDestination;
    private float chaseStallTime;
    private float chasePathFailureTime;

    public float EstimateChaseTime(Vector3 destination)
    {
        float distance = Vector3.Distance(transform.position, destination);
        if (CanUseAgent() && navAgent.CalculatePath(destination, returnPath)
            && returnPath.status == NavMeshPathStatus.PathComplete)
        {
            Vector3[] corners = returnPath.corners;
            distance = 0f;
            for (int i = 1; i < corners.Length; i++)
                distance += Vector3.Distance(corners[i - 1], corners[i]);
        }
        return distance / Mathf.Max(0.1f, data.chaseSpeed);
    }

    /// <summary>현재 뒤쪽 NavMesh 경계까지 직선으로 확보할 수 있는 수평 후퇴 거리</summary>
    public float GetAvailableRetreatDistance(float distance)
    {
        if (!CanUseAgent() || distance <= 0f)
            return 0f;

        Vector3 backward = -transform.forward;
        backward.y = 0f;
        if (backward.sqrMagnitude < 0.0001f)
            return 0f;
        backward.Normalize();
        if (!navAgent.Raycast(navAgent.nextPosition + backward * distance, out NavMeshHit hit))
            return distance;

        float available = Vector3.Dot(hit.position - navAgent.nextPosition, backward);
        return Mathf.Clamp(available - Mathf.Max(0.05f, data.arrivalTolerance), 0f, distance);
    }

    public float EstimateSpacingTime(float distance)
    {
        float speed = Mathf.Max(0.1f, data.EffectiveSpacingSpeed);
        return Mathf.Max(0f, distance) / speed + speed / Mathf.Max(1f, defaultAcceleration);
    }

    //strafe
    private EnemyCombatPositioning positioning;

    //넉백
    private readonly KnockbackMotion knockbackMotion = new();
    private int knockbackReactionHash;
    private readonly AttackReachMotion attackReachMotion = new();
    private float retreatMotionScale = 1f;
    private float retreatMotionRemaining;

    private Player attackPressureTarget;
    private Vector3 contactDirection;
    private DefenseType pressureDefense;

    private void Awake()
    {
        returnPath = new NavMeshPath();
        navAgent = GetComponent<NavMeshAgent>();
        defaultAcceleration = navAgent.acceleration;
        data = GetComponent<EnemyStats>().Data;
        owner = GetComponent<Enemy>();
        positioning = new EnemyCombatPositioning(navAgent, transform, data);
    }

    private void LateUpdate()
    {
        ValidateKnockbackReaction();
        ValidateAttackReach();
        ValidateAttackPressure();
        if (!CanUseAgent())
        {
            StopKnockback();
            return;
        }

        Vector3 frameDisplacement = knockbackMotion.Start(Time.deltaTime);
        if (frameDisplacement.sqrMagnitude > 0f
            && navAgent.Raycast(
            navAgent.nextPosition + frameDisplacement,
            out NavMeshHit hit))
        {
            frameDisplacement = hit.position - navAgent.nextPosition;
            frameDisplacement.y = 0f;
        }
        if (frameDisplacement.sqrMagnitude > 0f)
            navAgent.Move(frameDisplacement);
    }

    /// <summary>지정한 방향으로 회전</summary>
    public void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            data.combatRotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 추격
    /// </summary>
    public bool Chase(Vector3 destination, float stoppingDistance, bool faceTarget = false, float stallTimeout = 0f)
    {
        if (!CanUseAgent())
            return false;

        float targetTolerance = Mathf.Max(0.5f, data.distanceTolerance);
        if (!hasChaseProgress
            || (destination - chaseProgressDestination).sqrMagnitude > targetTolerance * targetTolerance)
        {
            ClearChaseProgress();
            hasChaseProgress = true;
            chaseProgressPosition = transform.position;
            chaseProgressDestination = destination;
        }

        float movementTolerance = Mathf.Max(0.05f, data.arrivalTolerance);
        if ((transform.position - chaseProgressPosition).sqrMagnitude >= movementTolerance * movementTolerance)
        {
            chaseProgressPosition = transform.position;
            chaseStallTime = 0f;
        }
        else if (chaseRequested)
            chaseStallTime += Time.deltaTime;

        float arrivalDistance = Mathf.Max(0f, stoppingDistance) + data.arrivalTolerance;
        bool arrived = (destination - transform.position).sqrMagnitude <= arrivalDistance * arrivalDistance;
        if (arrived)
            chaseStallTime = chasePathFailureTime = 0f;
        else if (chaseRequested && !navAgent.pathPending)
            chasePathFailureTime = !navAgent.hasPath || navAgent.pathStatus != NavMeshPathStatus.PathComplete
                ? chasePathFailureTime + Time.deltaTime : 0f;

        float timeout = stallTimeout > 0f ? stallTimeout : Mathf.Max(2f, data.attackPreparationTimeout);
        if (!arrived && (chaseStallTime >= timeout || chasePathFailureTime >= Mathf.Max(0.5f, data.approachRetryDelay)))
        {
            Stop();
            ClearChaseProgress();
            return false;
        }

        positioning.Reset();
        bool burstApproach = owner.AIController.IsBurstApproach && data.burstApproachSpeed > 0f;
        navAgent.speed = burstApproach ? data.burstApproachSpeed : data.chaseSpeed;
        navAgent.acceleration = burstApproach ? data.burstApproachAcceleration : defaultAcceleration;
        navAgent.stoppingDistance = Mathf.Max(0f, stoppingDistance);
        navAgent.isStopped = false;
        navAgent.updateRotation = !faceTarget;
        navAgent.angularSpeed = data.chaseAngularSpeed;
        bool needsDestination = !chaseRequested
            || !navAgent.destination.Equals(destination)
            || navAgent.isPathStale
            || !navAgent.pathPending
                && (!navAgent.hasPath || navAgent.pathStatus != NavMeshPathStatus.PathComplete);
        if (needsDestination)
            chaseRequested = navAgent.SetDestination(destination);
        if (!chaseRequested)
        {
            Stop();
            ClearChaseProgress();
            return false;
        }
        if (faceTarget)
            RotateTowards(destination - transform.position);
        return true;
    }

    private void ClearChaseProgress()
    {
        hasChaseProgress = chaseRequested = false;
        chaseStallTime = chasePathFailureTime = 0f;
    }

    public void ResetPosition(Vector3 position, Quaternion rotation)
    {
        Stop();
        StopKnockback();
        ClearAttackReach();
        StopAttackPressure();
        ClearChaseProgress();

        bool agentEnabled = navAgent != null && navAgent.enabled;
        if (agentEnabled && navAgent.Warp(position))
        {
            transform.SetPositionAndRotation(position, rotation);
            return;
        }

        if (agentEnabled)
            navAgent.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        if (agentEnabled)
            navAgent.enabled = true;
    }

    ///<summary>대상을 바라보면서 교전 거리를 유지하도록 좌우 이동</summary>
    public void Strafe(Vector3 targetPosition, float distance, bool moveAroundTarget = true)
    {
        chaseRequested = false;
        if (!CanUseAgent())
            return;

        navAgent.updateRotation = false;
        navAgent.acceleration = defaultAcceleration;
        positioning.Move(targetPosition, distance, moveAroundTarget);
        Vector3 directionToTarget = targetPosition - transform.position;
        RotateTowards(directionToTarget);
    }

    /// <summary>
    /// 정지
    /// </summary>
    public void Stop()
    {
        chaseRequested = false;
        if (!CanUseAgent())
            return;

        navAgent.updateRotation = false;
        navAgent.isStopped = true;
        if (navAgent.hasPath)
            navAgent.ResetPath();
        navAgent.velocity = Vector3.zero;
        positioning.Reset();
    }

    /// <summary>
    /// 현재 이동 속도를 애니메이션용 로컬 축 값으로 변환
    /// </summary>
    public Vector2 GetNormalizedVelocity()
    {
        if (!CanUseAgent())
            return Vector2.zero;

        Vector3 localVelocity = transform.InverseTransformDirection(navAgent.velocity);
        Vector2 horizontalVelocity = new(localVelocity.x, localVelocity.z);
        float speed = horizontalVelocity.magnitude;
        if (speed < 0.0001f)
            return Vector2.zero;

        float walkSpeed = Mathf.Max(0.01f, data.locomotionWalkSpeed);
        float runSpeed = Mathf.Max(walkSpeed + 0.01f, data.locomotionRunSpeed);
        float magnitude = speed <= walkSpeed ? speed / walkSpeed
            : 1f + Mathf.Clamp01((speed - walkSpeed) / (runSpeed - walkSpeed));
        return horizontalVelocity / speed * magnitude;
    }

    /// <summary>루트 모션의 이동, 회전을 적용. 공격 중에는 대상과의 최소 간격을 유지</summary>
    public void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation, Transform attackTarget)
    {
        ValidateKnockbackReaction();
        if (!CanUseAgent())
        {
            ClearAttackReach();
            StopAttackPressure();
            return;
        }

        if (knockbackReactionHash != 0)
            deltaPosition = knockbackMotion.ConstrainRootMotion(deltaPosition);
        deltaPosition = ApplyRetreatRootMotion(deltaPosition);
        EnemyAttackData attack = owner.Combat.CurrentAttack;
        if (attack != null && attack.AdvanceMotionMultiplier > 1f)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            float advance = Mathf.Max(0f, Vector3.Dot(deltaPosition, forward));
            deltaPosition += forward * (advance * (attack.AdvanceMotionMultiplier - 1f));
        }
        deltaPosition = ApplyAttackReach(deltaPosition, attackTarget);
        if (attackTarget != null)
            deltaPosition = ClampAttackAdvance(deltaPosition, attackTarget);
        else
            StopAttackPressure();

        navAgent.Move(deltaPosition);
        transform.rotation *= deltaRotation;
    }

    public void RecordAttackContact(Player target, in DamageResult result)
    {
        float gap = target != null
            ? KnockBackPolicy.GetAttackContactGap(
            result,
            navAgent.radius + target.Motor.Controller.radius,
            target.Motor.Controller.radius)
            : result.ContactDistance;
        attackReachMotion.RecordContact(
            transform.position,
            target != null ? target.transform : null,
            gap,
            result.Request.SourceWeapon.HitGroup);
    }

    public bool CanAssistNextAttack(EnemyAttackData attack)
    {
        Player target = Player.LocalPlayer;
        return attack.HasHitWindow
            && ValidateAttackReach()
            && attackReachMotion.CanBegin(
                transform.position,
                target.transform,
                attack.HitGroup,
                attack.MaxDistance,
                CombatSettings.Current.AttackReachExtraDistance);
    }

    public void BeginAttackReach(EnemyAttackData attack)
    {
        retreatMotionScale = 1f;
        retreatMotionRemaining = 0f;
        if (attack == data.retreatAttack)
        {
            StopKnockback();
            float distance = GetAvailableRetreatDistance(owner.AIController.GetRetreatDistance(attack));
            if (distance > 0f && attack.RetreatMotionDistance > 0f)
            {
                retreatMotionScale = distance / attack.RetreatMotionDistance;
                retreatMotionRemaining = distance;
            }
        }
        Transform target = CanAssistNextAttack(attack) ? Player.LocalPlayer.transform : null;
        attackReachMotion.Begin(
            transform.position,
            target,
            attack.HitGroup,
            attack.MaxDistance,
            CombatSettings.Current.AttackReachExtraDistance);
    }

    private Vector3 ApplyRetreatRootMotion(Vector3 deltaPosition)
    {
        if (owner.Combat.CurrentAttack != data.retreatAttack
            || data.retreatAttack == null
            || data.retreatAttack.RetreatDistance <= 0f)
            return deltaPosition;
        if (data.retreatAttack.RetreatMotionDistance <= 0f)
            return deltaPosition;

        Vector3 horizontal = new(deltaPosition.x, 0f, deltaPosition.z);
        horizontal *= retreatMotionScale;
        float distance = horizontal.magnitude;
        if (distance > retreatMotionRemaining && distance > 0f)
            horizontal *= retreatMotionRemaining / distance;
        if (horizontal.sqrMagnitude > 0f && navAgent.Raycast(navAgent.nextPosition + horizontal, out NavMeshHit hit))
        {
            horizontal = hit.position - navAgent.nextPosition;
            horizontal.y = 0f;
        }
        retreatMotionRemaining = Mathf.Max(0f, retreatMotionRemaining - horizontal.magnitude);
        return new Vector3(horizontal.x, deltaPosition.y, horizontal.z);
    }

    private Vector3 ApplyAttackReach(Vector3 deltaPosition, Transform attackTarget)
    {
        if (attackReachMotion.Target == null)
            return deltaPosition;

        Player target = Player.LocalPlayer;
        if (!ValidateAttackReach() || attackTarget != attackReachMotion.Target
            || owner.StateMachine.CurrentState is not EnemyAttackState attackState || !attackState.CanAssistAttackReach)
        {
            attackReachMotion.Stop();
            return deltaPosition;
        }

        float bodyWidth = navAgent.radius + target.Motor.Controller.radius;
        return attackReachMotion.Apply(
            deltaPosition,
            transform.position,
            bodyWidth,
            CombatSettings.Current.AttackReachMaxForwardMultiplier);
    }

    private bool ValidateAttackReach()
    {
        if (attackReachMotion.TrackedTarget == null)
            return false;

        Player target = Player.LocalPlayer;
        if (owner.StateMachine.CurrentState is not EnemyAttackState
            || target == null
            || target.transform != attackReachMotion.TrackedTarget
            || !target.isActiveAndEnabled
            || !target.Motor.Controller.enabled
            || target.Stats.IsDead
            || target.Stats.IsHealthDepleted
            || target.Stats.IsPostureBroken
            || target.Stats.IsInvincible
            || target.Execution.IsExecuting
            || target.StateMachine.CurrentState is PlayerDodgeState
            || owner.Stats.IsDead
            || owner.Stats.IsHealthDepleted
            || owner.Stats.IsPostureBroken
            || owner.IsBeingExecuted
            || !owner.Sense.HasClearPathTo(target.cameraRoot.position)
            || !attackReachMotion.ValidateContact(
                transform.position,
                navAgent.radius + target.Motor.Controller.radius,
                CombatSettings.Current.AttackReachExtraDistance))
        {
            ClearAttackReach();
            return false;
        }
        return true;
    }

    public void ClearAttackReach()
    {
        attackReachMotion.Clear();
        retreatMotionScale = 1f;
        retreatMotionRemaining = 0f;
    }

    /// <summary>
    /// 실제 이동량과 허용 이동량을 구해서 루트모션시 대상과의 최소 간격을 유지
    /// </summary>
    private Vector3 ClampAttackAdvance(Vector3 deltaPosition, Transform target)
    {
        Vector3 TargetDir = target.position - transform.position;
        TargetDir.y = 0f;

        float currentDistance = TargetDir.magnitude;
        if (currentDistance < 0.0001f)
            return deltaPosition;

        TargetDir /= currentDistance;
        float minimumDistance = navAgent.radius + Player.LocalPlayer.Motor.Controller.radius;

        if (attackPressureTarget != null && target != attackPressureTarget.transform)
            StopAttackPressure();
        ValidateAttackPressure();

        if (Vector3.Dot(deltaPosition, TargetDir) > 0f
            && navAgent.Raycast(
            transform.position + deltaPosition,
            out NavMeshHit hit))
        {
            float verticalDistance = deltaPosition.y;
            deltaPosition = hit.position - transform.position;
            deltaPosition.y = verticalDistance;
        }

        if (attackPressureTarget != null)
        {
            Vector3 targetPosition = target.position;
            targetPosition += attackPressureTarget.Motor.ApplyAttackPressureAdvance(transform, deltaPosition);
            TargetDir = targetPosition - transform.position;
            TargetDir.y = 0f;
            currentDistance = TargetDir.magnitude;
            if (currentDistance < 0.0001f)
                return deltaPosition;
            TargetDir /= currentDistance;
        }

        return KnockBackPolicy.ClampAdvance(deltaPosition, TargetDir, currentDistance, minimumDistance);
    }

    public void FollowAttackPressure(Transform source, float gap)
    {
        knockbackMotion.FollowAttack(source, gap, transform.position);
    }

    public Vector3 ApplyAttackPressureAdvance(Transform source, Vector3 requestedDelta)
    {
        ValidateKnockbackReaction();
        if (!CanUseAgent())
            return Vector3.zero;

        Vector3 displacement = knockbackMotion.GetAttackPressureAdvance(source, requestedDelta, transform.position);
        if (displacement.sqrMagnitude <= 0f)
            return Vector3.zero;
        if (navAgent.Raycast(navAgent.nextPosition + displacement, out NavMeshHit hit))
        {
            displacement = hit.position - navAgent.nextPosition;
            displacement.y = 0f;
        }
        Vector3 previousPosition = navAgent.nextPosition;
        navAgent.Move(displacement);
        return navAgent.nextPosition - previousPosition;
    }

    public void StopFollowingAttack(Transform source)
    {
        knockbackMotion.StopFollowingAttack(source);
    }

    private bool ValidateAttackPressure()
    {
        if (attackPressureTarget == null)
            return false;

        if (!KnockBackPolicy.TryGetAttackPressureGeometry(
            transform.position,
            attackPressureTarget.transform.position,
            contactDirection,
            out Vector3 direction,
            out float currentDistance,
            out float lateralDistance)
            || !CanContinueAttackPressure()
            || !attackPressureTarget.Combat.CanReceiveAttackPressure(direction / currentDistance, pressureDefense)
            || !attackPressureTarget.Motor.IsFollowingAttack(transform)
            || lateralDistance > navAgent.radius + attackPressureTarget.Motor.Controller.radius)
        {
            StopAttackPressure();
            return false;
        }

        return true;
    }

    private bool CanContinueAttackPressure(bool requireActive = false)
    {
        return CanUseAgent()
            && owner.isActiveAndEnabled
            && !owner.Stats.IsDead
            && !owner.Stats.IsHealthDepleted
            && !owner.Stats.IsPostureBroken
            && !owner.IsBeingExecuted
            && owner.StateMachine?.CurrentState is EnemyAttackState attackState
            && (requireActive
            ? attackState.CanApplyAttackPressure
            : attackState.CanMaintainAttackPressure);
    }

    public void StartAttackPressure(Player target, in DamageResult result)
    {
        Vector3 direction = target != null ? target.transform.position - transform.position : Vector3.zero;
        direction.y = 0f;
        if (target == null || direction.sqrMagnitude < 0.0001f || !CanContinueAttackPressure(requireActive: true)
            || !target.Combat.CanReceiveAttackPressure(direction.normalized, result.DefenseType))
        {
            StopAttackPressure();
            return;
        }

        StopAttackPressure();
        attackPressureTarget = target;
        float bodyWidth = navAgent.radius + target.Motor.Controller.radius;
        float goalGap = KnockBackPolicy.GetAttackPressureGap(result, bodyWidth, target.Motor.Controller.radius);
        contactDirection = direction.normalized;
        pressureDefense = result.DefenseType;
        target.Motor.FollowAttackPressure(transform, goalGap);
    }

    public void StopAttackPressure()
    {
        if (attackPressureTarget != null)
            attackPressureTarget.Motor.StopFollowingAttack(transform);
        attackPressureTarget = null;
    }

    private void OnDisable()
    {
        ClearChaseProgress();
        ClearAttackReach();
        StopAttackPressure();
        StopKnockback();
    }

    //=============================넉백 코드==============================

    /// <summary>
    /// 방향과 최종 넉백 사양을 받아 NavMeshAgent 이동을 시작
    /// </summary>
    public void StartKnockback(Vector3 direction, KnockbackSpec spec)
    {
        knockbackMotion.Ready(direction, spec);
        knockbackReactionHash = owner.AnimationController.ReactionHash;
    }

    public void StartKnockback(in DamageResult result)
    {
        StartKnockback(result.HitDirection, KnockBackPolicy.DefenderKnockBack(result));
    }

    /// <summary>
    /// 넉백 멈춤
    /// </summary>
    public void StopKnockback()
    {
        knockbackMotion.Stop();
        knockbackReactionHash = 0;
    }

    private void ValidateKnockbackReaction()
    {
        if (knockbackReactionHash != 0 && knockbackReactionHash != owner.AnimationController.ReactionHash)
        {
            knockbackMotion.StopFollowingAttack();
            knockbackReactionHash = 0;
        }
    }

    public bool IsFollowingAttack(Transform source)
    {
        ValidateKnockbackReaction();
        return knockbackMotion.IsFollowingAttack(source);
    }

    //==============================Agent 코드 ========================
    /// <summary>Navagent 종료</summary>
    public void DisableAgent()
    {
        ClearChaseProgress();
        StopKnockback();
        ClearAttackReach();
        StopAttackPressure();

        navAgent.enabled = false;
    }

    /// <summary>
    /// NavAgent 사용 가능한지
    /// </summary>
    /// <summary>행동을 선택할 때만 확인하는 이동 여유. 실제 이동은 기존 NavMesh와 루트 모션이 처리합니다.</summary>
    public bool HasDecisionSpace(float angle, float distance)
    {
        if (!CanUseAgent())
            return false;
        Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
        return !navAgent.Raycast(navAgent.nextPosition + direction * distance, out _);
    }

    private bool CanUseAgent()
    {
        return navAgent != null && navAgent.enabled && navAgent.isOnNavMesh;
    }
}
