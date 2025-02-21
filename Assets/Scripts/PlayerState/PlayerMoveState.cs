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
        if (player.move == Vector3.zero) { player.playerStateMachine.TransitionTo(player.playerStateMachine.playerIdleState); }
        else if (player.attack)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerAttackState);
        }
        else if (player.jump && player.isGround)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerJumpState);
        }else if(player.sprint)
        {
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerSlideState);
        }
        Move();
    }

    public void Exit() { player.anim.SetBool("isMove", false); }

    public void Move()
    {
        moveDirection = player.transform.forward * player.move.z + player.transform.right * player.move.x;
        player.anim.SetFloat("xDir", player.move.x);
        player.anim.SetFloat("zDir", player.move.z);
        Vector3 newPosition = player.rb.position + player.moveSpeed * Time.deltaTime * moveDirection.normalized;
        player.rb.MovePosition(newPosition);
    }
}
