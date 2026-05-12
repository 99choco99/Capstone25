using System;
using System.Collections;
using UnityEngine;


public class PlayerMotor : MonoBehaviour
{

    public CharacterController controller;
    private Player player;
    private Transform camTransform;


    [SerializeField] private float rotationSpeed = 15f;

    [Header("움직임 설정")]
    public float MoveSpeed;
    public float SprintSpeed;
    public float GuardSpeed;
    public float JumpPower;

    [Header("점프 및 중력처리")]
    //중력 및 점프 처리를 위한 변수
    public Vector3 verticalVelocity;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundCheckDistance = 0.2f; 
    [SerializeField] private LayerMask groundLayer;


    [Header("넉백")]
    private bool isKnockingBack = false;
    private float knockBackTimer;
    private float knockBackDuration;
    private Vector3 knockbackMovement;

    [Header("백스탭")]
    public Vector3 rollDirection;

    public bool IsGrounded { get; private set; } = true;
    public bool CanMove = true;
    public bool CanRotate = true;

    private Vector3 inputVelocity;



    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        player = GetComponent<Player>();
    }

    private void Start()
    {
        camTransform = Camera.main.transform;
        if (!player.IsLocalPlayer) { return; }
        player.Stats.OnDamaged += StartKnockBack;
        player.Stats.OnPostureBroken += Groggy;
    }

    private void OnDestroy()
    {
        if (!player.IsLocalPlayer) { return; }
        player.Stats.OnDamaged -= StartKnockBack;
        player.Stats.OnPostureBroken -= Groggy;
    }


    private void Update()
    {
        HandleGroundCheck();
        ApplyMovement();
    }

    //지면 체크
    public void HandleGroundCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z);
        IsGrounded = Physics.CheckSphere(spherePosition, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }


    public void ApplyMovement()
    {
        Vector3 finalMovement = Vector3.zero;

        finalMovement += CalculateInputMovement();
        finalMovement += CalculateKnockBack();
        finalMovement += CalculateGravity();

        controller.Move(finalMovement * Time.deltaTime);
    }

    private Vector3 CalculateInputMovement()
    {
        return inputVelocity;
    }
    private Vector3 CalculateGravity()
    {
        verticalVelocity.y = gravity * Time.deltaTime;

        if (IsGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        return verticalVelocity;
    }
    private Vector3 CalculateKnockBack()
    {
        if (!isKnockingBack) { return Vector3.zero; }

        knockBackTimer += Time.deltaTime;
        float deceleration = 1f - (knockBackTimer / Time.deltaTime);
        deceleration = Mathf.Clamp01(deceleration);

        if (knockBackTimer >= knockBackDuration) isKnockingBack = false;

        return knockbackMovement * deceleration;
    }


    //회전처리
    public void HandleRotation()
    {
        if (!CanRotate || isKnockingBack) { return; }

        if (player.IsLockOn)
        {
            if (player.TargetingSystem.CurrentTarget == null) { return; }
            Vector3 targetDirection = player.TargetingSystem.CurrentTarget.TargetTransform.position - transform.position;
            RotateTowardsDirection(targetDirection);
        }
        else
        {
            Vector3 RotationDirection = (camTransform.forward.normalized * player.InputHandler.MoveInput.z + camTransform.right.normalized * player.InputHandler.MoveInput.x);
            RotateTowardsDirection(RotationDirection);
        }
    }

    public void RotateTowardsDirection(Vector3 direction)
    {
        direction.y = 0;
        direction.Normalize();
        if (direction == Vector3.zero) { direction = transform.forward; }
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    //===================특수 움직임  ================//

    //기본적인 이동
    public void SetTargetVelocity(float targetSpeed)
    {
        if (!player.IsLocalPlayer) { return; }
        if (!CanMove || isKnockingBack) return;

        Vector3 cameraForward = camTransform.forward;
        cameraForward.y = 0f;
        Vector3 cameraRight = camTransform.right;
        cameraRight.y = 0f;

        Vector3 moveDirection = (cameraForward * player.InputHandler.MoveInput.z + cameraRight * player.InputHandler.MoveInput.x).normalized;

        inputVelocity = moveDirection * targetSpeed;
    }

    //백스텝 및 구르기
    public void Dodge()
    {
        //구르기 shift 
        if (player.InputHandler.MoveInput != Vector3.zero)
        {
            Vector3 cameraForward = camTransform.forward;
            cameraForward.y = 0f;
            Vector3 cameraRight = camTransform.right;
            cameraRight.y = 0f;

            Vector3 rollDirection = (cameraForward.normalized * player.InputHandler.MoveInput.z + cameraRight.normalized * player.InputHandler.MoveInput.x).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(rollDirection);
            transform.rotation = targetRotation;
            player.AnimatorManager.PlayAction(AnimHash.Roll, true);
        }
        else
        {
            player.AnimatorManager.PlayAction(AnimHash.BackStep, true);
        }

    }

    public void StopMovement()
    {
        controller.Move(Vector3.zero);
    }

    public void Groggy()
    {
        player.AnimatorManager.PlayAction(AnimHash.GuardBreak, true, true);
    }


    //점프
    public void Jump(float jumpForce)
    {
        if (!IsGrounded || !CanMove || isKnockingBack) return;

        // y축 속도에 점프 힘을 직접 더해줌
        verticalVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
    }



    //넉백 시작
     public void StartKnockBack(DamageInfo damageInfo)
     {
        isKnockingBack = true;
        knockBackTimer = 0f;
        knockBackDuration = damageInfo.knockbackDuration;
        knockbackMovement = damageInfo.hitDirection * damageInfo.knockbackForce; // 초당 넉백될 이동량 계산

     }

}
