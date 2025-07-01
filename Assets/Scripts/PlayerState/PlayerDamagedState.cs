using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDamagedState : StateMachineBehaviour
{
    private readonly PlayerController player;

    public PlayerDamagedState(PlayerController player) { this.player = player; }
    float LastInvincibleTime;
    public void Enter()
    {
        player.playerInput.enabled = false;
        player.col.tag = "Invincible";
        LastInvincibleTime = Time.time;
        player.anim.SetTrigger("Hit");
    }
    public void Update()
    {
        if(LastInvincibleTime + player.InvincibleTime < Time.time)
        {
            
        }
    }
    public void Exit()
    {
        player.playerInput.enabled = true;
        player.col.tag = "PlayerData";
        player.player.Ishit = false;
        player.anim.ResetTrigger("Hit");
    }

}
