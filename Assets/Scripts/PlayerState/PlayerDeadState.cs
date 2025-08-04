using UnityEngine;

public class PlayerDeadState : StateMachineBehaviour
{
    PlayerController player;

    public PlayerDeadState(PlayerController player) { this.player = player; }

    public void Enter()
    {
        player.anim.SetTrigger("Die");
        player.col.tag = "Invincible";
        player.col.gameObject.layer = 9;
    }

    public void Exit()
    {

    }

    public void Update()
    {

    }
}
