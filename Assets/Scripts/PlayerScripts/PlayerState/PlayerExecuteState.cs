using UnityEngine;

public class PlayerExecuteState : State
{
    public PlayerExecuteState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private ITargetable target;
    public override bool UseRootMotion => true;

    public override void Enter()
    {
        player.Stats.isInvincible = true;
        player.Motor.CanMove = false;
        player.Motor.CanRotate = false;
        player.InputHandler.enabled = false;

        target = player.TargetingSystem.CurrentTarget;

        if(target == null)
        {
            stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            return;
        }
        player.Motor.StopMovement();
        //player.Execution.AttemptDeathblow(target.gameObject.GetComponent<Enemy>());
    }

    public override void Exit()
    {
        player.Stats.isInvincible = false;
        player.Motor.CanMove = true;
        player.Motor.CanRotate = true;
        player.InputHandler.enabled = true;
    }
}
