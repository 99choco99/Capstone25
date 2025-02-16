using UnityEngine;

public class PlayerAttackState : IState
{
    PlayerController player;
    public PlayerAttackState(PlayerController player) { this.player = player; }
    public void Enter() { 
        player.anim.SetBool("Attack", true);
        player.StartCoroutine("HandleAttack");
    }
    public void Update() { }
    public void Exit() {

    }
}
