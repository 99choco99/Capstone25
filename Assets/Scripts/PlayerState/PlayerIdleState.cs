using UnityEngine;

public class PlayerIdleState : IState
{
    PlayerController player;
    public PlayerIdleState(PlayerController player) { this.player = player; }
    public void Enter() {}
    public void Update() {
        if (player.move.magnitude != 0 || player.jump && player.isGround) { 
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerMoveState); 
        }
        else if (player.attack)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerAttackState);
        }else if(player.sprint)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerSlideState);
        }else if (player.guard)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerGuardState);
        }
    }
    public void Exit()
    {

    }
}
