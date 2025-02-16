using UnityEngine;


// ����, ����, ���� ������ ���� ����� ���� �Ҵ� ����
public class PlayerUnControllableState : IState
{
    PlayerController player;

    public PlayerUnControllableState(PlayerController player) { 
        this.player = player;
    }

    public void Enter() {
        Debug.Log("����");
    }
    public void Update() {

    }
    public void Exit() {
        player.anim.SetBool("isJump", false);
        player.anim.SetBool("Attack", false);
    } 

}
