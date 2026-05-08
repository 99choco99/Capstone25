using UnityEngine;

public class PlayerDodgeState : State
{
    public PlayerDodgeState(Player player,PlayerStateMachine stateMachine) : base(player, stateMachine) { }


    public override bool UseRootMotion => true;


    public override void Enter()
    {
        player.Motor.Dodge();
        player.Motor.canRotate = false;
        player.Stats.isInvincible = true;
    }


    public override void Update()
    {
        if (!player.AnimatorManager.isPerformingAction) 
        {
            if(player.InputHandler.MoveInput != Vector3.zero)
            {
                stateMachine.TransitionTo(stateMachine.PlayerMoveState);
            }
            else
            {
                stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            }
        }
    }

    public override void Exit() 
    {
        player.Motor.canMove = true;
        player.Motor.canRotate = true;
        player.Stats.isInvincible = false;
    }

}
