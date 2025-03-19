using UnityEngine;

public class PlayerAttackState : IState
{
    private readonly PlayerController player;
    private float lastAttackTime;  //°ø°Ý ÄðÅ¸ÀÓ
    int attackCount { get => player.anim.GetInteger("AttackCount"); set => player.anim.SetInteger("AttackCount", value); }
    public PlayerAttackState(PlayerController player) { this.player = player; }
    public void Enter() {
        player.anim.SetTrigger("Attack");
        attackCount = player.anim.GetInteger("AttackCount");
        lastAttackTime = Time.time;
    }
    public void Update() { 
        if(Time.time <= lastAttackTime + player.AttackTime) { return; }
        player.playerStateMachine.TransitionTo(player.playerStateMachine.playerMoveState);
    }
    public void Exit() {
        player.anim.ResetTrigger("Attack");
    }
}
