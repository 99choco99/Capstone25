using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMoveState : IState
{
    PlayerController player;
    private Vector3 moveDirection;

    public PlayerMoveState(PlayerController player) { this.player = player; }

    public void Enter()
    {
        player.anim.SetBool("isMove", true);
    }
    public void Update()
    {
        // 플레이어 점프
        if (player.jump && player.isGround)
        {
            Jump();
        }

        // 상태 전이 조건들
        if (player.move == Vector3.zero) { player.playerStateMachine.TransitionTo(player.playerStateMachine.playerIdleState); }
        else if (player.attack)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerAttackState);
        }
        else if(player.sprint)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerSlideState);
        }else if (player.guard)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerGuardState);
        }
        Move();
    }

    public void Exit() { player.anim.SetBool("isMove", false);}

    
    // 플레이어 움직임 구현
    public void Move()
    {
        moveDirection = player.transform.forward * player.move.z + player.transform.right * player.move.x;
        player.anim.SetFloat("xDir", player.move.x);
        player.anim.SetFloat("zDir", player.move.z);
        Vector3 newPosition = player.rb.position + player.moveSpeed * Time.deltaTime * moveDirection.normalized;
        player.rb.MovePosition(newPosition);
    }

    //플레이어 점프 구현
    public void Jump()
    {
        player.anim.SetTrigger("Jump");
        player.rb.AddForce(Vector3.up * player.jumpPower, ForceMode.Impulse);
        player.isGround = false;
        player.jump = false;
    }
}
