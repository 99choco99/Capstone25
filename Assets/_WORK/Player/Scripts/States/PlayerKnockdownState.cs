using UnityEngine;

/// <summary>체간 붕괴와 별개인 강공격 그로기. 같은 모션을 쓰며 기존 행동 복귀 대기를 유지합니다.</summary>
public sealed class PlayerKnockdownState : PlayerState
{
    private float stateTimer;
    private float recoveryTimer;
    private bool isRecovering;
    private bool ownsInvincibility;
    // 기존 기상 클립(0.65초, 빠른 복귀는 1.25배)의 행동 대기를 보존합니다.
    private float RecoveryDuration => CombatSettings.Current.PlayerKnockdownRecoveryDuration;
    private float EarlyRecoveryDuration => RecoveryDuration / CombatSettings.Current.PlayerKnockdownEarlyRecoveryMultiplier;
    private float recoveryDuration;

    public PlayerKnockdownState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override bool UseRootMotion => false;
    public override PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.HitReaction;

    public override void Enter()
    {
        stateTimer = recoveryTimer = 0f;
        isRecovering = false;
        ownsInvincibility = false;
        player.InputBuffer.Clear();
        player.Motor.SetMovement(Vector3.zero);
        player.Combat.ForceResetAttackState();
        player.Combat.CurrentDefenseType = DefenseType.None;
        player.AnimatorController.ForceStopLocomotion();
        player.AnimatorController.PlayReaction(AnimHash.Knockdown);
    }

    public void RestartHit()
    {
        if (isRecovering)
        {
            ReleaseInvincibility();
            Enter();
            return;
        }

        // 그로기 중 후속 강공격은 자세를 재시작하지 않고 위험 구간만 연장합니다.
        stateTimer = 0f;
    }

    public override void HandleDamage(DamageResult result)
    {
        if (!isRecovering && result.DefenseType == DefenseType.None
            && result.Request.HitReaction != HitReactionType.Knockdown)
            return;
        base.HandleDamage(result);
    }

    public override void Update()
    {
        player.Motor.SetMovement(Vector3.zero);
        if (!isRecovering)
        {
            stateTimer += Time.deltaTime;
            if (!player.AnimatorController.TryGetAttackProgress(AnimHash.Knockdown, out _, out float reactionProgress)
                || reactionProgress < 1f)
                return;
            float dodgeDelay = Mathf.Max(0f, CombatSettings.Current.PlayerKnockdownDodgeDelay);
            if (stateTimer >= dodgeDelay && player.InputBuffer.PeekValidCommand() == ActionCommand.Dodge)
            {
                player.InputBuffer.ConsumeCurrentCommand();
                BeginRecovery(true);
            }
            else if (stateTimer >= Mathf.Max(dodgeDelay, CombatSettings.Current.PlayerKnockdownRecoveryDelay))
                BeginRecovery(false);
            return;
        }

        recoveryTimer += Time.deltaTime;
        if (ownsInvincibility && recoveryTimer >= CombatSettings.Current.PlayerDodgeInvincibleDuration)
            ReleaseInvincibility();

        if (recoveryTimer >= recoveryDuration)
            stateMachine.TransitionTo(player.InputHandler.GuardInput
                ? stateMachine.PlayerGuardState
                : stateMachine.PlayerGroundedState);
    }

    private void BeginRecovery(bool dodge)
    {
        isRecovering = true;
        recoveryTimer = 0f;
        recoveryDuration = dodge ? EarlyRecoveryDuration : RecoveryDuration;
        player.Motor.StopKnockback();
        // 그로기 자세를 유지합니다. 누운 자세의 기상 모션은 재생하지 않습니다.
        if (dodge && CombatSettings.Current.PlayerDodgeInvincibleDuration > 0f)
        {
            player.SetInvincible(true);
            ownsInvincibility = true;
        }
    }

    private void ReleaseInvincibility()
    {
        if (!ownsInvincibility)
            return;
        player.SetInvincible(false);
        ownsInvincibility = false;
    }

    public override void Exit()
    {
        ReleaseInvincibility();
        stateTimer = recoveryTimer = 0f;
        isRecovering = false;
    }
}
