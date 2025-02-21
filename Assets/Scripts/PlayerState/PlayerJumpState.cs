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
        player.jump = false;
        if (!Physics.Raycast(player.rb.position, Vector3.down, 0.2f))
        {
            player.isGround = false;
        }
        else
        {
            if (player.rb.linearVelocity.y <= 3) { 
                player.isGround = true;
                player.playerStateMachine.TransitionTo(player.playerStateMachine.preState);
            }
        }
    }
    public void Exit()
    {
        player.anim.ResetTrigger("Jump");
    }

}
