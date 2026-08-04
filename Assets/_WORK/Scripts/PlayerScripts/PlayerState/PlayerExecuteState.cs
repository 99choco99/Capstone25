using UnityEngine;

/// <summary>
/// 플레이어가 인살 중인 상태
/// </summary>
public class PlayerExecuteState : PlayerState
{
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
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
    }

    public override void Exit()
    {
        player.Execution.OnExecuteEnd -= HandleExecutionCompleted;
        player.SetInvincible(false);
    }
}
