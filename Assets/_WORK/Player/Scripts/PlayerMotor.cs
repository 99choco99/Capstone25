using UnityEngine;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    public CharacterController Controller { get; private set; }

    [Header("움직임 설정")]
    public float MoveSpeed = 5f;
    public float SprintSpeed = 8f;
    public float GuardSpeed = 2f;
    public float rotationSpeed = 15f;

    [Header("점프 및 중력처리")]
    [Tooltip("캐릭터가 뛸 최고 높이")]
    [SerializeField] float jumpHeight = 2.0f;
    [Tooltip("점프 후 정점까지 도달하는 시간")]
    [SerializeField] float timeToJump = 0.4f;
    [Tooltip("떨어질 때 묵직함을 주는 배수")]
    [SerializeField] float fallMultiplier = 2.5f;
    [Tooltip("경사로에서 허공에 안 뜨게 잡아주는 힘")]
    [SerializeField] float groundedGravity = -5.0f;

    [Header("지면 체크")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float groundCheckDistance = 0.15f;

    // 내부 계산용 변수
    public float Gravity { get; private set; }
    private Vector3 verticalVelocity;
    private Vector3 inputVelocity;

    private readonly KnockbackMotion knockbackMotion = new();
    private int knockbackReactionHash;
    private readonly AttackReachMotion attackReachMotion = new();
    private Player player;
    private Enemy attackReachTarget;
    private float attackReachContactEndTime;
    private Enemy attackPressureTarget;
    private Vector3 contactDirection;
    private DefenseType pressureDefense;
    public float CurrentVerticalVelocity => verticalVelocity.y;

    public bool IsGrounded { get; private set; }
    private bool hasNearbyGround;

    private void Awake()
    {
        Controller = GetComponent<CharacterController>();
        player = GetComponent<Player>();
        Gravity = -(2 * jumpHeight) / (timeToJump * timeToJump);
    }

    private void LateUpdate()
    {
        ValidateKnockbackReaction();
        ValidateAttackReach();
        ValidateAttackPressure();
        if (!Controller.enabled)
        {
            StopKnockback();
            return;
        }

        Vector3 displacement = knockbackMotion.Start(Time.deltaTime);
        if (displacement.sqrMagnitude > 0f)
            Controller.Move(displacement);
    }

    /// <summary>
    /// 중력
    /// </summary>
    private Vector3 UpdateVerticalVelocity()
    {
        if (hasNearbyGround && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedGravity;
        }
        else
        {
            float multiplier = verticalVelocity.y < 0f ? fallMultiplier : 1f;
            verticalVelocity.y += Gravity * multiplier * Time.deltaTime;
        }

        return verticalVelocity;
    }

    /// <summary>
    /// 점프
    /// </summary>
    public void Jump()
    {
        if (IsGrounded)
        {
            verticalVelocity.y = Mathf.Abs(Gravity) * timeToJump;
        }
    }

    /// <summary>
    /// 지면 체크
    /// </summary>
    public void HandleGroundCheck()
    {
        hasNearbyGround = false;
        if (!Controller.enabled)
        {
            IsGrounded = false;
            return;
        }

        Vector3 scale = transform.lossyScale;
        float horizontalScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float radius = Controller.radius * horizontalScale * 0.9f;
        float skinWidth = Controller.skinWidth * horizontalScale;
        Vector3 capsuleBottom = transform.TransformPoint(Controller.center)
            - Vector3.up * (Controller.height * Mathf.Abs(scale.y) * 0.5f);
        float bottomOffset = Vector3.Dot(capsuleBottom - transform.position, Vector3.up);
        float probeOffset = skinWidth + 0.05f;
        Vector3 sphereStart = transform.position + Vector3.up * (radius + probeOffset);
        float castDistance = probeOffset + Mathf.Max(0f, groundCheckDistance);
        float contactDistance = Mathf.Min(Mathf.Max(0f, groundCheckDistance), Mathf.Max(0.02f, skinWidth));
        if (Physics.SphereCast(
            sphereStart,
            radius,
            Vector3.down,
            out RaycastHit hit,
            castDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Angle(hit.normal, Vector3.up) <= Controller.slopeLimit)
            {
                hasNearbyGround = true;
                IsGrounded = verticalVelocity.y <= 0f && hit.distance + bottomOffset <= probeOffset + contactDistance;
                return;
            }
        }
        IsGrounded = false;
    }

    /// <summary>
    /// 움직임 최종 적용
    /// </summary>
    public void ApplyMovement()
    {
        float deltaTime = Time.deltaTime;
        Vector3 velocity = inputVelocity + UpdateVerticalVelocity();
        Vector3 frameDisplacement = velocity * deltaTime;

        if ((Controller.Move(frameDisplacement) & CollisionFlags.Sides) != 0)
            StopAttackReach();

        inputVelocity = Vector3.zero;
    }

    //기본적인 움직임
    public void SetMovement(Vector3 moveDirection)
    {
        if (attackReachTarget != null
            && Vector3.Dot(
            moveDirection,
            attackReachTarget.transform.position - transform.position) < 0f)
            ClearAttackReach();
        inputVelocity = moveDirection;
    }

    /// <summary>
    /// 정해진 방향과 거리로 넉백 시작
    /// </summary>
    public void StartKnockback(Vector3 direction, KnockbackSpec spec)
    {
        knockbackMotion.Ready(direction, spec);
        knockbackReactionHash = player.AnimatorController.ReactionHash;
    }

    public void StartKnockback(in DamageResult result)
    {
        if (result.DefenseType == DefenseType.None)
        {
            ClearAttackReach();
        }
        StartKnockback(result.HitDirection, KnockBackPolicy.DefenderKnockBack(result));
    }

    /// <summary>
    /// 넉백 종료
    /// </summary>
    public void StopKnockback()
    {
        knockbackMotion.Stop();
        knockbackReactionHash = 0;
    }

    private void ValidateKnockbackReaction()
    {
        if (knockbackReactionHash != 0 && knockbackReactionHash != player.AnimatorController.ReactionHash)
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

    public void FollowAttackPressure(Transform source, float gap)
    {
        knockbackMotion.FollowAttack(source, gap, transform.position);
    }

    public Vector3 ApplyAttackPressureAdvance(Transform source, Vector3 requestedDelta)
    {
        ValidateKnockbackReaction();
        if (!Controller.enabled)
            return Vector3.zero;

        Vector3 displacement = knockbackMotion.GetAttackPressureAdvance(source, requestedDelta, transform.position);
        if (displacement.sqrMagnitude <= 0f)
            return Vector3.zero;
        Vector3 previousPosition = transform.position;
        Controller.Move(displacement);
        return transform.position - previousPosition;
    }

    public void StopFollowingAttack(Transform source)
    {
        knockbackMotion.StopFollowingAttack(source);
    }

    /// <summary>
    /// 루트모션시 움직임
    /// </summary>
    public void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        ValidateKnockbackReaction();
        if (knockbackReactionHash != 0)
            deltaPosition = knockbackMotion.ConstrainRootMotion(deltaPosition);
        deltaPosition = ApplyAttackReach(deltaPosition);
        if (attackPressureTarget != null || attackReachMotion.Target != null)
            deltaPosition = ClampAttackAdvance(deltaPosition);

        if ((Controller.Move(deltaPosition) & CollisionFlags.Sides) != 0)
            StopAttackReach();
        transform.rotation *= deltaRotation;
    }

    public void RecordAttackContact(Enemy target, in DamageResult result)
    {
        attackReachTarget = target;
        float gap = target != null
            ? KnockBackPolicy.GetAttackContactGap(
            result,
            Controller.radius + target.Motor.BodyRadius,
            target.Motor.BodyRadius)
            : result.ContactDistance;
        attackReachMotion.RecordContact(
            transform.position,
            target != null ? target.transform : null,
            gap,
            result.Request.SourceWeapon.HitGroup);
        attackReachContactEndTime = Time.time + CombatSettings.Current.AttackReachContactMemoryDuration;
    }

    public void BeginAttackReach(PlayerAttackData attack)
    {
        Enemy target = player.IsLockOn ? player.TargetingSystem.CurrentTarget as Enemy : null;
        if (attackReachTarget != target)
            ClearAttackReach();
        attackReachTarget = target;
        if (!attack.HasHitWindow || !ValidateAttackReach())
        {
            ClearAttackReach();
            return;
        }

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        float bodyWidth = Controller.radius + target.Motor.BodyRadius;
        Weapon weapon = player.Combat.CurrentWeapon;
        float fallbackGap = Mathf.Max(
            bodyWidth,
            weapon.GetForwardReach(transform, direction.normalized) + target.Motor.BodyRadius + CombatSettings.Current.GuardContactReach);
        attackReachMotion.Begin(
            transform.position,
            target.transform,
            weapon.HitGroup,
            attack.MaxDistance,
            CombatSettings.Current.AttackReachExtraDistance,
            preserveContact: true,
            fallbackGap: fallbackGap);
    }

    private Vector3 ApplyAttackReach(Vector3 deltaPosition)
    {
        if (attackReachMotion.Target == null)
            return deltaPosition;

        if (!ValidateAttackReach()
            || player.StateMachine.CurrentState is not PlayerAttackState attackState
            || !attackState.CanAssistAttackReach)
        {
            attackReachMotion.Stop();
            return deltaPosition;
        }

        float bodyWidth = Controller.radius + attackReachTarget.Motor.BodyRadius;
        return attackReachMotion.Apply(
            deltaPosition,
            transform.position,
            bodyWidth,
            CombatSettings.Current.AttackReachMaxForwardMultiplier);
    }

    private bool ValidateAttackReach()
    {
        if (attackReachTarget == null)
            return false;

        if (attackReachMotion.ContactTarget != null && Time.time >= attackReachContactEndTime)
        {
            attackReachMotion.Clear();
            attackReachContactEndTime = 0f;
        }

        if (player.StateMachine.CurrentState is not PlayerAttackState
            && player.StateMachine.CurrentState is not PlayerGuardState
            && player.StateMachine.CurrentState is not PlayerGroundedState
            || !player.IsLockOn
            || attackReachMotion.TrackedTarget != null
            && attackReachMotion.TrackedTarget != attackReachTarget.transform
            || attackReachMotion.ContactTarget != null
            && !attackReachMotion.HasContact(
                attackReachTarget.transform,
                player.Combat.CurrentWeapon.HitGroup)
            || player.TargetingSystem.CurrentTarget?.TargetTransform != attackReachTarget.transform
            || !Controller.enabled
            || player.Stats.IsDead
            || player.Stats.IsHealthDepleted
            || player.Stats.IsPostureBroken
            || player.Execution.IsExecuting
            || !attackReachTarget.isActiveAndEnabled
            || attackReachTarget.Stats.IsDead
            || attackReachTarget.Stats.IsHealthDepleted
            || attackReachTarget.Stats.IsPostureBroken
            || attackReachTarget.Stats.IsInvincible
            || attackReachTarget.Combat.CurrentAttack == attackReachTarget.Stats.Data.retreatAttack
            && attackReachTarget.Combat.CurrentAttack != null
            || !attackReachTarget.Sense.HasClearPathTo(player.cameraRoot.position)
            || !attackReachMotion.ValidateContact(
                transform.position,
                Controller.radius + attackReachTarget.Motor.BodyRadius,
                CombatSettings.Current.AttackReachExtraDistance))
        {
            ClearAttackReach();
            return false;
        }
        if (player.StateMachine.CurrentState is not PlayerAttackState)
            StopAttackReach();
        return true;
    }

    public void StopAttackReach()
    {
        attackReachMotion.Stop();
    }

    public void ClearAttackReach()
    {
        attackReachMotion.Clear();
        attackReachTarget = null;
        attackReachContactEndTime = 0f;
    }

    private Vector3 ClampAttackAdvance(Vector3 deltaPosition)
    {
        Enemy target = ValidateAttackPressure()
            ? attackPressureTarget
            : attackReachMotion.Target != null
            ? attackReachTarget
            : null;
        if (target == null)
            return deltaPosition;

        Vector3 targetPosition = target.transform.position;
        if (attackPressureTarget != null)
            targetPosition += target.Motor.ApplyAttackPressureAdvance(transform, deltaPosition);
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        float currentDistance = direction.magnitude;
        if (currentDistance < 0.0001f)
            return deltaPosition;
        direction /= currentDistance;
        float bodyWidth = Controller.radius + target.Motor.BodyRadius;
        return KnockBackPolicy.ClampAdvance(deltaPosition, direction, currentDistance, bodyWidth);
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
            || player.IsLockOn && player.TargetingSystem.CurrentTarget.TargetTransform != attackPressureTarget.transform
            || !attackPressureTarget.Combat.CanReceiveAttackPressure(direction / currentDistance, pressureDefense)
            || !attackPressureTarget.Motor.IsFollowingAttack(transform)
            || lateralDistance > Controller.radius + attackPressureTarget.Motor.BodyRadius)
        {
            StopAttackPressure();
            return false;
        }

        return true;
    }

    private bool CanContinueAttackPressure(bool requireActive = false)
    {
        return Controller.enabled
            && player.isActiveAndEnabled
            && !player.Stats.IsDead
            && !player.Stats.IsHealthDepleted
            && !player.Stats.IsPostureBroken
            && !player.Execution.IsExecuting
            && player.StateMachine?.CurrentState is PlayerAttackState attackState
            && (requireActive
            ? attackState.CanApplyAttackPressure
            : attackState.CanMaintainAttackPressure);
    }

    public void StartAttackPressure(Enemy target, in DamageResult result)
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
        float bodyWidth = Controller.radius + target.Motor.BodyRadius;
        float goalGap = KnockBackPolicy.GetAttackPressureGap(result, bodyWidth, target.Motor.BodyRadius);
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

    /// <summary>
    /// 정해진 위치로 transform 설정
    /// </summary>
    public void SetTransform(Vector3 position, Quaternion rotation)
    {
        inputVelocity = Vector3.zero;
        verticalVelocity = Vector3.zero;
        StopKnockback();
        ClearAttackReach();
        StopAttackPressure();

        Controller.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        Controller.enabled = true;
    }

    /// <summary>
    /// 특정 방향으로 회전처리
    /// </summary>
    public void RotateToDirection(Vector3 direction)
    {
        direction.y = 0;
        if (direction.sqrMagnitude < 0.01f)
            return;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
