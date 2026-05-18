using UnityEngine;

public class PlayerExecuteState : State
{
    public PlayerExecuteState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private ITargetable target;
    public override bool UseRootMotion => true;

    public override void Enter()
    {
        player.Stats.IsInvincible = true;
        player.InputHandler.enabled = false;

        target = player.TargetingSystem.CurrentTarget;

        if(target == null)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            return;
        }
        player.Motor.StopMovement();


        if (target is Enemy enemy)
        {
            player.Execution.AttemptDeathblow(enemy);
        }
    }

    public override void Exit()
    {
        player.Stats.IsInvincible = false;
        player.InputHandler.enabled = true;
    }
}
