using UnityEngine;

public class PlayerGuardState : IState
{
    PlayerController player;

    public PlayerGuardState(PlayerController player) { this.player = player; }
    public void Enter() {
        player.anim.SetBool("Guard",true);
        player.col.tag = "GuardState";
    }
    public void Update() {
        if (player.anim.GetCurrentAnimatorStateInfo(0).IsName("Guard") && player.player.Ishit)
        {
            if (player.anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.8f)
            {
                player.anim.SetTrigger("Parry");
                player.playerStateMachine.TransitionTo(player.playerStateMachine.playerMoveState);
            }
            else
            {
                player.anim.SetTrigger("GuardHit");
                player.player.Ishit = false;
            }
        }
        else if (player.player.Ishit)
        {
            player.anim.SetTrigger("GuardHit");
            player.player.Ishit = false;
        }
        else if (!player.guard)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerMoveState);
            return;
        }
    }
    public void Exit() {
        player.anim.SetBool("Guard",false);
        player.anim.ResetTrigger("GuardHit");
        player.player.Ishit = false;
        player.col.tag = "PlayerData";
    }
}
