using UnityEngine;

public class PlayerJumpState : IState
{
    PlayerController player;
    public PlayerJumpState(PlayerController player) { this.player = player; }
    public void Enter()
    {
        player.anim.SetTrigger("Jump");
        player.rb.AddForce(Vector3.up *player.jumpPower ,ForceMode.Impulse);
        player.isGround = false;
    }
    public void Update()
    {

    }
    public void Exit()
    {
        player.anim.ResetTrigger("Jump");
    }

}
