using UnityEngine;


// 기절, 점프, 공격 등으로 인한 사용자 통제 불능 상태
public class PlayerUnControllableState : IState
{
    PlayerController player;

    public PlayerUnControllableState(PlayerController player) { 
        this.player = player;
    }

    public void Enter() {
        Debug.Log("기절");
    }
    public void Update() {

    }
    public void Exit() {
        player.anim.SetBool("isJump", false);
        player.anim.SetBool("Attack", false);
    } 

}
