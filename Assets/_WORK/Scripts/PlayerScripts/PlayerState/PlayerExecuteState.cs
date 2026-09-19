using UnityEngine;

/// <summary>
/// 플레이어가 인살 중인 상태
/// </summary>
public class PlayerExecuteState : PlayerState
{
    private Enemy executedTarget;
    private bool restoreLockOn;

    public PlayerExecuteState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override bool UseRootMotion => true;

    public override void Enter()
    {
        DeathblowPlan? requested = stateMachine.RequestedDeathblowPlan;
        stateMachine.RequestedDeathblowPlan = null;

        if (!requested.HasValue)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            return;
        }

        executedTarget = requested.Value.Target;
        restoreLockOn = player.TargetingSystem.CurrentTarget is Enemy target && target == executedTarget;
        player.SetInvincible(true);
        player.Combat.ForceResetAttackState();
        player.Motor.SetMovement(Vector3.zero);

        player.Motor.StopKnockback();
        player.Execution.OnExecuteEnd -= HandleExecutionCompleted;
        player.Execution.OnExecuteEnd += HandleExecutionCompleted;


        if (!player.Execution.StartDeathblow(requested.Value))
        {   
            //인살 실패 시
            stateMachine.RequestedAttackData = player.Combat.FirstAttackData;
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
            return;
        }

        player.TargetingSystem.DeselectTarget();
    }

    /// <summary>
    /// 인살 끝난 후 복귀
    /// </summary>
    private void HandleExecutionCompleted()
    {
        if (stateMachine.CurrentState == this)
        {
            // 인살 전에 락온했던 적이 살아남은 경우에만 전투 시점을 복원합니다.
            if (restoreLockOn && executedTarget != null && !executedTarget.IsDead)
                player.TargetingSystem.SelectTarget(executedTarget);
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    public override void Exit()
    {
        player.Execution.OnExecuteEnd -= HandleExecutionCompleted;
        executedTarget = null;
        restoreLockOn = false;
        player.SetInvincible(false);
    }
}
