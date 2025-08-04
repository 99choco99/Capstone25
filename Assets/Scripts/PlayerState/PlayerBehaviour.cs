using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerBehaviour : MonoBehaviour
{
    PlayerController player;
    public GameObject weapon;
    Collider weaponCollider;
    const float SENSEGROUND = 0.4f;

    private bool canRotation;
    public bool canMove;
    private Vector3 moveDirection;
    public bool isInRange;

    public float guardDuration;


    public bool isKnockingBack;
    [SerializeField] private float knockBackDuration;
    float knockBackForce;
    float knockBackTimer;
    Vector3 knockBackDirection;
    Vector3 startPosition;
    Vector3 targetPosition;
    Vector3 currentPosition;

    void Start()
    {
        weaponCollider = weapon.GetComponentInChildren<Collider>();
        player = GetComponent<PlayerController>();
    }


    //타겟 조준, 카메라 회전 등 구현 해야됨

    void FixedUpdate()
    {
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
                else if (player.isGround)
                {
                    player.anim.SetBool("Jump", false);
                }
                if (canMove)
                {
                    Move();
                }
                break;
            case PlayerState.Attack:
                //공격 동시에 방향 조절
                if (player.playerDetectEnemy.currentTarget != null)
                {
                    Transform currentTargetTransform = player.playerDetectEnemy.currentTarget.transform;
                    transform.rotation = Quaternion.LookRotation((currentTargetTransform.position - transform.position).normalized);
                }
                Vector3 forwardDir = Quaternion.Euler(0, player.CameraMovement.rotY, 0) * Vector3.forward;
                moveDirection = forwardDir * player.move.z + Quaternion.Euler(0, 90, 0) * forwardDir * player.move.x;
                if (canRotation && player.playerDetectEnemy.currentTarget == null && moveDirection.sqrMagnitude > 0.01f)
                {
                    player.transform.rotation = Quaternion.LookRotation(moveDirection.normalized);
                }
                break;
            case PlayerState.Guard:
                if (canMove)
                {
                    Move();
                }
                guardDuration += Time.deltaTime;
                if (player.playerSetting.Ishit)
                {
                    KnockBack();
                    player.transform.rotation = Quaternion.LookRotation(-knockBackDirection);
                }
                break;
            case PlayerState.Damaged:
                if (isKnockingBack)
                {
                    KnockBack();
                    player.transform.rotation = Quaternion.LookRotation(-knockBackDirection);
                }
                break;
        }
    }

    public void KnockBackInit(float knockBackForce, float knockBackDuration)
    {
        knockBackDirection = player.playerSetting.hitDirection.normalized;
        this.knockBackDuration = knockBackDuration;
        this.knockBackForce = knockBackForce;
        startPosition = player.transform.position;
        knockBackTimer = 0f;
        isKnockingBack = true;
        player.rb.linearVelocity = default;
    }

    public void KnockBack()
    {
        knockBackTimer += Time.fixedDeltaTime;
        float t = knockBackTimer / knockBackDuration; // 0에서 1까지 증가하는 시간 비율
        t = 1f - (1f - t) * (1f - t);

        targetPosition = startPosition + knockBackDirection * knockBackForce;
        currentPosition = Vector3.Lerp(startPosition, targetPosition, t);
        player.rb.MovePosition(currentPosition);


        if(knockBackTimer >= knockBackDuration)
        {
            player.playerSetting.Ishit = false;
            isKnockingBack = false;
            if (player.guard)
            {
                player.currentState = PlayerState.Guard; //임시조치
            }
            player.currentState = PlayerState.Move;
        }
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
        Vector3 newPosition = player.rb.position + player.currentSpeed * Time.deltaTime * moveDirection.normalized;
        player.rb.MovePosition(newPosition);
    }



    public void Jump()  
    {
        player.anim.SetBool("Jump", true);
        player.rb.AddForce(Vector3.up * player.jumpPower, ForceMode.Impulse);
        player.isGround = false;
        player.jump = true;
    }


    public void AE_playerAttackStart()
    {
        weaponCollider.enabled = true;

    }
    public void AE_playerAttackEnd()
    {
        weaponCollider.enabled = false;
    }

    public void AE_playerAttackRotationEnable()
    {
        canRotation = true;
    }
    public void AE_playerAttackRotationDisable()
    {
        canRotation = false;
    }
    public void AE_playerMoveEnable()
    {
        canMove = true;
    }
    public void AE_playerMoveDisable()
    {
        canMove = false;
    }   
}
