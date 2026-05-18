using UnityEngine;

public class PlayerDodgeState : State
{
    public PlayerDodgeState(Player player,PlayerStateMachine stateMachine) : base(player, stateMachine) { }


    public override bool UseRootMotion => true;


    public override void Enter()
    {
        player.Motor.Dodge();
        player.Stats.IsInvincible = true;
    }


    public override void Update()
    {
        if (player.InputHandler.MoveInput != Vector3.zero)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
        else
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    public override void Exit() 
    {
        player.Stats.IsInvincible = false;
    }

}
