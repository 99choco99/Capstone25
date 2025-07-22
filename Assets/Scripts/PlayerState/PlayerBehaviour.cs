using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerBehaviour : MonoBehaviour
{
    PlayerController player;
    public GameObject weapon;
    Collider weaponCollider;
    const float SENSEGROUND = 0.4f;

    private Vector3 moveDirection;
    public bool isInRange;
    float tmpSpeed;

    public float guardDuration;

    [SerializeField] private float knockBackDuration;
    Vector3 knockBackDirection;
    bool isKnockBack;
    AnimationCurve knockBackCurve;
    Vector3 startPosition;
    Vector3 targetPosition;

    void Start()
    {
        weaponCollider = weapon.GetComponentInChildren<Collider>();
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
                    transform.rotation = Quaternion.LookRotation((currentTargetTransform.position - transform.position).normalized);
                }
                break;
            case PlayerState.Guard:
                Move();
                guardDuration += Time.deltaTime;
                break;
            case PlayerState.Damaged:
                KnockBack();
                player.transform.rotation = Quaternion.LookRotation(-knockBackDirection);
                break;
        }
    }

    public void KnockBackInit(float knockBackForce)
    {
        Debug.Log(knockBackForce);
        knockBackDirection = player.playerSetting.hitDirection.normalized;
        startPosition = player.transform.position;
        targetPosition = knockBackDirection * knockBackForce;
        player.rb.linearVelocity = default;
        isKnockBack = true;
        StartCoroutine(KnockBackCoroutine());
    }

    public void KnockBack()
    {
        if (isKnockBack)
        {
            player.rb.AddForce(targetPosition, ForceMode.Impulse);
            //if (player.anim.GetBool("AirBornState"))
            //{
            //    player.rb.AddForce((transform.up - transform.forward).normalized, ForceMode.Impulse);
            //}
        }
    }
    IEnumerator KnockBackCoroutine()
    {
        float timer = 0f;
        while(timer < knockBackDuration){
            timer += Time.deltaTime;
            yield return null;
        }
        isKnockBack = false;
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



    public void AE_playerAttackStart()
    {
        weaponCollider.enabled = true;
    }
    public void AE_playerAttackEnd()
    {
        weaponCollider.enabled = false;
    }
}
