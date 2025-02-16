using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMoveState : IState
{
    PlayerController player;
    private Vector3 moveDirection;
    private float speed = 5;

    public PlayerMoveState(PlayerController player) { this.player = player; }

    public void Enter()
    {
        Debug.Log("이동 상태");
        player.anim.SetBool("isMove", true);
    }
    public void Update()
    {
        moveDirection = player.transform.forward * player.input.move.z + player.transform.right * player.input.move.x;

        player.anim.SetFloat("xDir", player.input.move.x);
        player.anim.SetFloat("zDir", player.input.move.z);

        Vector3 newPosition = player.rb.position + speed * Time.deltaTime * moveDirection.normalized;
        player.rb.MovePosition(newPosition);
    }

    public void Exit() { player.anim.SetBool("isMove", false); }


}
