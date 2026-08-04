using System.Collections;
using UnityEngine;

public class PlayerGuardState : PlayerState
{
    private const float ParryTime = 0.2f;       // 첫 입력의 패링 판정 창: 약 12프레임
    private const float SecondParryTime = 0.1f; // 빠른 재입력: 약 6프레임
    private const float MinParryTime = 0.067f;  // 세 번째 재입력: 약 4프레임
    private const float SpamResetTime = 0.5f;   // 이 시간 동안 재입력이 없으면 첫 창으로 복구
    private const float GuardLockTime = 0.12f;  // 이 동안 제자리에 멈춘다

    private float guardTimer;
    private float currentParryTime;   // 연타 페널티가 반영된 실제 패링 창
    private float lastGuardPressTime = -10f;
    private int spamCount = 0;             // 연타 횟수


    private float HitReactionTimer;


    public PlayerGuardState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override bool UseRootMotion => false;
    public override PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.GuardBoosted;

    public override void HandleDamage(DamageResult result)
    {
        switch (result.DefenseType)
        {
            case DefenseType.Parry:
                player.AnimatorController.PlayReaction(AnimHash.Parry, 0.03f);
                player.Motor.StopKnockback();
                HitReactionTimer = 0f;
                spamCount = 0;
                lastGuardPressTime = -10f;
                guardTimer = currentParryTime + 0.001f;
                return;
            case DefenseType.NormalGuard:
                player.AnimatorController.PlayReaction(AnimHash.GuardHit, 0.03f);
                GuardKnockBack(result);
                return;
            default:
                base.HandleDamage(result);
                return;
        }
    }


    public override void Enter()
    {
        guardTimer = 0f;
        HitReactionTimer = 0f;
        player.Motor.StopKnockback();

        if (Time.unscaledTime - lastGuardPressTime > SpamResetTime) spamCount = 0;
        else spamCount++;
        lastGuardPressTime = Time.unscaledTime;

        currentParryTime = spamCount switch
        {
            0 => ParryTime,
            1 => SecondParryTime,
            2 => MinParryTime,
            _ => 0f
        };

        player.Motor.SetMovement(Vector3.zero);
        player.AnimatorController.ForceStopLocomotion();
        player.AnimatorController.PlayReaction(AnimHash.Guard, 0.03f);

        player.Combat.CurrentDefenseType = currentParryTime > 0f ? DefenseType.Parry : DefenseType.NormalGuard;
    }


    public override void Update()
    {
        guardTimer += Time.unscaledDeltaTime;

        if (player.Combat.CurrentDefenseType == DefenseType.Parry && guardTimer > currentParryTime)
        {
            player.Combat.CurrentDefenseType = DefenseType.NormalGuard;
        }

        if (HitReactionTimer > 0f)
        {
            HitReactionTimer -= Time.deltaTime;
            HandleGuardMovement(Vector3.zero);
            return;
        }

        base.HandleInput();

        if (stateMachine.CurrentState != this) return;

        if (guardTimer < GuardLockTime)
        {
            HandleGuardMovement(Vector3.zero);
            return;
        }

        if (!player.InputHandler.GuardInput)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            return;
        }

        HandleGuardMovement(player.GetDesiredMoveDirection());
    }

    /// <summary>
    /// 가드 중 피격시 넉백
    /// </summary>
    private void GuardKnockBack(in DamageResult result)
    {
        KnockbackSpec knockback = KnockBackPolicy.DefenderKnockBack(result);

        HitReactionTimer = knockback.Duration;
        player.Motor.StartKnockback(result.HitDirection, knockback);
    }

    /// <summary>
    /// 가드 중 이동 및 회전 통합 처리
    /// </summary>
    private void HandleGuardMovement(Vector3 moveDir)
    {
        player.Motor.SetMovement(moveDir * player.Motor.GuardSpeed);

        if (player.IsLockOn && player.TargetingSystem.CurrentTarget != null)
        {
            Vector3 directionToTarget = player.TargetingSystem.CurrentTarget.TargetTransform.position - player.transform.position;
            directionToTarget.y = 0;
            player.Motor.RotateToDirection(directionToTarget);
        }
        else if (moveDir != Vector3.zero)
        {
            player.Motor.RotateToDirection(moveDir);
        }

        UpdateLocomotionAnimation(moveDir);
    }

    /// <summary>
    /// 움직임 애니메이션 동기화
    /// </summary>
    private void UpdateLocomotionAnimation(Vector3 moveDir)
    {
        if (moveDir == Vector3.zero)
        {
            player.AnimatorController.UpdateLocomotion(0, 0);
        }
        else
        {
            if (player.IsLockOn)
            {
                Vector3 localMove = player.transform.InverseTransformDirection(moveDir);
                player.AnimatorController.UpdateLocomotion(localMove.x, localMove.z);
            }
            else
                player.AnimatorController.UpdateLocomotion(0, player.InputHandler.MoveAmount);
        }
    }

    protected override void OnAttackCommand()
    {
        if (RequestDeathblow()) return;

        stateMachine.RequestedAttackData = player.Combat.FirstAttackData;
        stateMachine.TransitionTo(stateMachine.PlayerAttackState);
    }

    protected override void OnDodgeCommand()
    {
        stateMachine.TransitionTo(stateMachine.PlayerDodgeState);
    }

    protected override void OnJumpCommand()
    {
        if (player.Motor.IsGrounded)
        {
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
        }
    }

    protected override void OnGuardCommand()
    {
        if (guardTimer >= GuardLockTime)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
        }
    }

    public override void Exit()
    {
        HitReactionTimer = 0f;
        player.Motor.StopKnockback();
        player.Combat.CurrentDefenseType = DefenseType.None;
    }

}
