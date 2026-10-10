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
    public float BodyRadius => navAgent.radius;
    [SerializeField] private Transform[] patrolPoint;
    [SerializeField, Min(0f)] private float patrolWaitTime = 1f;
    private int currentPoint = 0;
    private float patrolWaitTimer;
    public bool HasPatrolPoints => patrolPoint != null && patrolPoint.Length > 0;

    //strafe
    private int strafeDirection = 1;
    private float strafeDirectionTimer;
    private float strafeDestinationTimer;
    private bool hasStrafeDestination;

    //넉백
    private readonly KnockbackMotion knockbackMotion = new();
    private int knockbackReactionHash;
    private readonly AttackReachMotion attackReachMotion = new();

    private Player attackPressureTarget;
    private Vector3 contactDirection;
    private DefenseType pressureDefense;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        data = GetComponent<EnemyStats>().Data;
        owner = GetComponent<Enemy>();
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

        Vector3 frameDisplacement = knockbackMotion.Start(Time.deltaTime, transform.position);
        if (frameDisplacement.sqrMagnitude > 0f && navAgent.Raycast(navAgent.nextPosition + frameDisplacement, out NavMeshHit hit))
        {
            frameDisplacement = hit.position - navAgent.nextPosition;
            frameDisplacement.y = 0f;
        }
        if (frameDisplacement.sqrMagnitude > 0f)
            navAgent.Move(frameDisplacement);
    }

    /// <summary>지정한 위치로 이동</summary>
    private void MoveTo(Vector3 destination)
    {
        if (!CanUseAgent()) return;

        navAgent.speed = data.patrolSpeed;
        navAgent.stoppingDistance = 0f;
        navAgent.isStopped = false;
        navAgent.updateRotation = true;
        navAgent.SetDestination(destination);
    }

    public void Patrol(bool startPatrol)
    {
        if (!CanUseAgent() || !HasPatrolPoints) return;
        // 추격에서 순찰로 돌아올 때는 이전 목적지까지 남은 거리를 기다리지 않습니다.
        if (startPatrol)
        {
            MoveTo(patrolPoint[currentPoint].position);
            return;
        }

        if (!navAgent.isStopped)
        {
            if (navAgent.pathPending || navAgent.remainingDistance > navAgent.stoppingDistance + data.arrivalTolerance) return;

            Stop();
            patrolWaitTimer = patrolWaitTime;
            return;
        }

        patrolWaitTimer -= Time.deltaTime;
        if (patrolWaitTimer > 0f) return;

        currentPoint = (currentPoint + 1) % patrolPoint.Length;
        MoveTo(patrolPoint[currentPoint].position);
    }

    /// <summary>지정한 방향으로 회전</summary>
    public void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, data.combatRotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 추격
    /// </summary>
    public void Chase(Vector3 destination, float stoppingDistance)
    {
        if (!CanUseAgent()) return;

        navAgent.speed = data.chaseSpeed;
        navAgent.stoppingDistance = Mathf.Max(0f, stoppingDistance);
        navAgent.isStopped = false;
        navAgent.updateRotation = true;
        navAgent.angularSpeed = data.chaseAngularSpeed;
        navAgent.SetDestination(destination);
    }

    ///<summary>대상을 바라보면서 교전 거리를 유지하도록 좌우 이동</summary>
    public void Strafe(Vector3 targetPosition, float distance, bool moveAroundTarget = true)
    {
        if (!CanUseAgent()) return;

        navAgent.isStopped = false;
        navAgent.updateRotation = false;
        navAgent.stoppingDistance = data.arrivalTolerance;

        strafeDirectionTimer -= Time.deltaTime;
        strafeDestinationTimer -= Time.deltaTime;

        if (strafeDirectionTimer <= 0f)
        {
            strafeDirection = Random.value > 0.5f ? 1 : -1;
            strafeDirectionTimer = Random.Range(data.minStrafeTime, data.maxStrafeTime);
            strafeDestinationTimer = 0f;
        }

        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0f;
        if (moveAroundTarget)
            distance = directionToTarget.magnitude;
        if (directionToTarget.sqrMagnitude < 0.0001f)
            directionToTarget = transform.forward;
        directionToTarget.Normalize();

        //목적지에 도달했는지
        bool reachedDestination = !navAgent.pathPending && navAgent.hasPath && navAgent.remainingDistance <= navAgent.stoppingDistance + data.arrivalTolerance;

        if (!hasStrafeDestination || strafeDestinationTimer <= 0f || reachedDestination)
        {
            Vector3 nextDirection = -directionToTarget;
            if (moveAroundTarget)
                nextDirection = Quaternion.AngleAxis(strafeDirection * data.strafeArcAngle, Vector3.up) * nextDirection;
            Vector3 destination = targetPosition + nextDirection * distance;

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, data.strafeSampleDistance, NavMesh.AllAreas))
            {
                navAgent.speed = data.strafeSpeed;
                navAgent.SetDestination(hit.position);
                hasStrafeDestination = true;
                strafeDestinationTimer = data.strafeRefreshTime;
            }
            else
            {
                strafeDirection *= -1;
                strafeDirectionTimer = data.strafeRetryTime;
                strafeDestinationTimer = 0f;
            }
        }

        RotateTowards(directionToTarget);
    }

    /// <summary>
    /// 정지
    /// </summary>
    public void Stop()
    {
        if (!CanUseAgent()) return;

        navAgent.isStopped = true;
        if (navAgent.hasPath) navAgent.ResetPath();
        navAgent.velocity = Vector3.zero;
        hasStrafeDestination = false;
        strafeDestinationTimer = 0f;
    }

    /// <summary>
    /// 현재 이동 속도를 애니메이션용 로컬 축 값으로 변환
    /// </summary>
    public Vector2 GetNormalizedVelocity()
    {
        if (!CanUseAgent()) return Vector2.zero;

        float safeSpeed = navAgent.speed > 0f ? navAgent.speed : 1f;
        Vector3 localVelocity = transform.InverseTransformDirection(navAgent.velocity);
        return new Vector2(localVelocity.x / safeSpeed, localVelocity.z / safeSpeed);
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

        deltaPosition = knockbackMotion.ConstrainRootMotion(deltaPosition);
        deltaPosition = ApplyAttackReach(deltaPosition, attackTarget);
        if (attackTarget != null)
            deltaPosition = ClampAttackAdvance(deltaPosition, attackTarget);
        else
            StopAttackPressure();

        Vector3 previousPosition = navAgent.nextPosition;
        navAgent.Move(deltaPosition);
        if (attackPressureTarget != null && CanContinueAttackPressure())
            attackPressureTarget.Motor.AddAttackAdvance(transform, navAgent.nextPosition - previousPosition);
        transform.rotation *= deltaRotation;
    }

    public void RecordAttackContact(Player target, in DamageResult result)
    {
        attackReachMotion.RecordContact(transform.position, target != null ? target.transform : null, result.ContactDistance, result.Request.SourceWeapon.HitGroup);
    }

    public bool CanAssistNextAttack(EnemyAttackData attack)
    {
        Player target = Player.LocalPlayer;
        return attack.HasHitWindow && ValidateAttackReach()
            && attackReachMotion.CanBegin(transform.position, target.transform, attack.HitGroup, attack.MaxDistance, CombatSettings.Current.AttackReachDistanceLimit);
    }

    public void BeginAttackReach(EnemyAttackData attack)
    {
        Transform target = CanAssistNextAttack(attack) ? Player.LocalPlayer.transform : null;
        attackReachMotion.Begin(transform.position, target, attack.HitGroup, attack.MaxDistance, CombatSettings.Current.AttackReachDistanceLimit);
    }

    private Vector3 ApplyAttackReach(Vector3 deltaPosition, Transform attackTarget)
    {
        if (attackReachMotion.Target == null) return deltaPosition;

        Player target = Player.LocalPlayer;
        if (!ValidateAttackReach() || attackTarget != attackReachMotion.Target
            || owner.StateMachine.CurrentState is not EnemyAttackState attackState || !attackState.CanAssistAttackReach)
        {
            attackReachMotion.Stop();
            return deltaPosition;
        }

        float bodyWidth = navAgent.radius + target.Motor.Controller.radius;
        return attackReachMotion.Apply(deltaPosition, transform.position, bodyWidth, CombatSettings.Current.AttackReachMaxForwardMultiplier);
    }

    private bool ValidateAttackReach()
    {
        if (attackReachMotion.TrackedTarget == null) return false;

        Player target = Player.LocalPlayer;
        if (owner.StateMachine.CurrentState is not EnemyAttackState || target == null || target.transform != attackReachMotion.TrackedTarget
            || !target.isActiveAndEnabled || !target.Motor.Controller.enabled || target.Stats.IsDead || target.Stats.IsHealthDepleted
            || target.Stats.IsPostureBroken || target.Stats.IsInvincible || target.Execution.IsExecuting
            || target.StateMachine.CurrentState is PlayerDodgeState
            || owner.Stats.IsDead || owner.Stats.IsHealthDepleted || owner.Stats.IsPostureBroken || owner.IsBeingExecuted
            || !owner.Sense.HasClearPathTo(target.cameraRoot.position)
            || !attackReachMotion.ValidateContact(transform.position, navAgent.radius + target.Motor.Controller.radius, CombatSettings.Current.AttackReachDistanceLimit))
        {
            ClearAttackReach();
            return false;
        }
        return true;
    }

    public void ClearAttackReach()
    {
        attackReachMotion.Clear();
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

        if (Vector3.Dot(deltaPosition, TargetDir) > 0f && navAgent.Raycast(transform.position + deltaPosition, out NavMeshHit hit))
        {
            float verticalDistance = deltaPosition.y;
            deltaPosition = hit.position - transform.position;
            deltaPosition.y = verticalDistance;
        }

        return KnockBackPolicy.ClampAdvance(deltaPosition, TargetDir, currentDistance, minimumDistance);
    }

    public void FollowAttackPressure(Transform source, float gap)
    {
        knockbackMotion.FollowAttack(source, gap);
    }

    public void AddAttackAdvance(Transform source, Vector3 actualDelta)
    {
        knockbackMotion.AddAttackAdvance(source, actualDelta);
    }

    public void StopFollowingAttack(Transform source)
    {
        knockbackMotion.StopFollowingAttack(source);
    }

    private bool ValidateAttackPressure()
    {
        if (attackPressureTarget == null)
            return false;

        Vector3 direction = attackPressureTarget.transform.position - transform.position;
        direction.y = 0f;
        float currentDistance = direction.magnitude;
        float lateralDistance = Vector3.ProjectOnPlane(direction, contactDirection).magnitude;
        if (currentDistance < 0.0001f || Vector3.Dot(direction, contactDirection) <= 0f || !CanContinueAttackPressure()
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
        return CanUseAgent() && owner.isActiveAndEnabled && !owner.Stats.IsDead && !owner.Stats.IsHealthDepleted && !owner.Stats.IsPostureBroken && !owner.IsBeingExecuted
            && owner.StateMachine?.CurrentState is EnemyAttackState attackState && (requireActive ? attackState.CanApplyAttackPressure : attackState.CanMaintainAttackPressure);
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
            StopKnockback();
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
        StopKnockback();
        ClearAttackReach();
        StopAttackPressure();

        navAgent.enabled = false;
    }

    /// <summary>
    /// NavAgent 사용 가능한지
    /// </summary>
    private bool CanUseAgent()
    {
        return navAgent != null && navAgent.enabled && navAgent.isOnNavMesh;
    }
}
