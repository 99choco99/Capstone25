using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDamagedState : IState
{
    PlayerController player;

    public PlayerDamagedState(PlayerController player) { this.player = player; }
    PlayerInput playerInput;
    float LastInvincibleTime;
    public void Enter()
    {
        playerInput = player.GetComponent<PlayerInput>();
        playerInput.enabled = false;
        player.col.tag = "Invincible";
        LastInvincibleTime = Time.time;
    }
    public void Update()
    {
        if(LastInvincibleTime + player.InvincibleTime < Time.time)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerIdleState);
        }
    }
    public void Exit()
    {
        playerInput.enabled = true;
        player.col.tag = "Player";
    }

}
