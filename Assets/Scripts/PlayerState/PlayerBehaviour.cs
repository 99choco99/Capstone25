using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerBehaviour : MonoBehaviour
{
    PlayerController player;
    const float SENSEGROUND = 0.4f;
    private Vector3 moveDirection;
    public Vector3 hitPoint;
    public Vector3 hitDirection;
    public bool isInRange;
    float tmpSpeed;

    void Start()
    {
        player = GetComponent<PlayerController>();
        tmpSpeed = player.moveSpeed;
    }


    //타겟 조준, 카메라 회전 등 구현 해야됨

    void FixedUpdate()
    {
        //플레이어 점프 착지
        if (Physics.Raycast(player.rb.position, Vector3.down, SENSEGROUND) && player.rb.linearVelocity.y <= 0)
        {
            player.isGround = true;
        }
        switch (player.currentState)
        {
            case PlayerState.Move:
                if (player.jump && player.isGround)
                {
                    Jump();
                }
                if (player.isGround)
                {
                    player.anim.SetBool("Jump", false);
                }
                if (player.sprint)
                {
                    player.moveSpeed = player.sprintSpeed;
                }
                else
                {
                    player.moveSpeed = tmpSpeed;
                }
                    Move();
                break;
            case PlayerState.Attack:
                if (player.playerDetectEnemy.currentTarget != null)
                {
                    Transform currentTargetTransform = player.playerDetectEnemy.currentTarget.transform;
                    if (Vector3.Distance(currentTargetTransform.position, transform.position) <= player.AttackRange)
                    {
                        isInRange = true;
                        Vector3 newPosition = (transform.position - currentTargetTransform.position).normalized;
                        player.rb.MovePosition(transform.position + newPosition);
                    }
                    else
                    {
                        transform.rotation = Quaternion.LookRotation((currentTargetTransform.position - transform.position).normalized);
                        isInRange = false;
                    }
                }
                break;
            case PlayerState.Guard:
                Move();
                break;
            case PlayerState.Damaged:

                break;
        }
    }



    public void Guard()
    {

    }

    // 플레이어 움직임 구현  
    public void Move()
    {
        Vector3 forwardDir = Quaternion.Euler(0, player.CameraMovement.rotY, 0) * Vector3.forward;
        moveDirection = forwardDir * player.move.z + Quaternion.Euler(0, 90, 0) * forwardDir * player.move.x;
        if (moveDirection.magnitude > 0.01f)
        {
            player.anim.SetBool("isMove", true);
            if (player.playerDetectEnemy.currentTarget != null)
            {
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, 
                    Quaternion.LookRotation(player.playerDetectEnemy.TargetDirection),
                    Time.fixedDeltaTime * player.moveSpeed);
            }
            else
            {
                player.transform.rotation =
                    Quaternion.Slerp(player.transform.rotation,
                    Quaternion.LookRotation(moveDirection.normalized),
                    Time.fixedDeltaTime * player.moveSpeed);
            }
        }
        else
        {
            player.anim.SetBool("isMove", false);
        }
        player.anim.SetFloat("xDir", player.move.x);
        player.anim.SetFloat("zDir", player.move.z);
        Vector3 newPosition = player.rb.position + player.moveSpeed * Time.deltaTime * moveDirection.normalized;
        player.rb.MovePosition(newPosition);
    }


    //플레이어 점프 구현
    public void Jump()
    {
        player.anim.SetBool("Jump", true);
        player.rb.AddForce(Vector3.up * player.jumpPower, ForceMode.Impulse);
        player.isGround = false;
        player.jump = true;
    }

}
