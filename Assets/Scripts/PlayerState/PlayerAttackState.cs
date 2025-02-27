using UnityEngine;

public class PlayerAttackState : IState
{
    PlayerController player;
    private float lastAttackTime;  //���� ��Ÿ��
    public PlayerAttackState(PlayerController player) { this.player = player; }
    public void Enter() {
        player.anim.SetBool("Attack", true);
        lastAttackTime = Time.time;
    }
    public void Update() { 
        if(Time.time <= lastAttackTime + player.AttackTime) { return; }
        player.playerStateMachine.TransitionTo(player.playerStateMachine.playerIdleState);
    }
    public void Exit() {
        player.anim.SetBool("Attack", false);
    }
}
