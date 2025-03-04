using UnityEngine;

public class PlayerGuardState : IState
{
    PlayerController player;

    public PlayerGuardState(PlayerController player) { this.player = player; }
    public void Enter() {
        Debug.Log("가드상태");
        player.anim.SetTrigger("Guard");
        player.anim.SetLayerWeight(1, 1);
    }
    public void Update() {
        if (!player.guard)
        {
            player.anim.SetTrigger("GuardExit");
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerIdleState);
        }
    }
    public void Exit() {
        player.anim.SetLayerWeight(1, 0);
        player.anim.ResetTrigger("Guard");
    }
}
