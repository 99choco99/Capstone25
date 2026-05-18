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
    [SerializeField] float groundCheckDistance;
    [SerializeField] LayerMask groundLayer;
    [Tooltip("캐릭터가 뛸 최고 높이 (미터 단위)")]
    [SerializeField] float jumpHeight = 2.0f; // 소울라이크 국룰: 1.5 ~ 2.0
    [Tooltip("점프 후 정점까지 도달하는 시간 (초)")]
    [SerializeField] float timeToJumpApex = 0.4f; // 소울라이크 국룰: 0.3 ~ 0.45
    [Tooltip("떨어질 때 묵직함을 주는 배수")]
    [SerializeField] float fallMultiplier = 2.5f; // 액션 게임 국룰: 2.0 ~ 3.0
    [Tooltip("경사로에서 허공에 안 뜨게 잡아주는 힘")]
    [SerializeField] float groundedGravity = -5.0f; // 국룰: -2.0 ~ -5.0

    // 내부 계산용 변수
    private float gravity;
    private float initialJumpVelocity;
    private Vector3 verticalVelocity;


    [Header("넉백")]
    private bool isKnockingBack = false;
    private float knockBackTimer;
    private float knockBackDuration;
    private Vector3 knockbackMovement;

    [Header("백스탭")]
    public Vector3 rollDirection;

    public bool IsGrounded { get; private set; } = true;

    private Vector3 inputVelocity;



    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        player = GetComponent<Player>();
    }

    private void Start()
    {
        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        initialJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;

        camTransform = UnityEngine.Camera.main.transform;
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

        inputVelocity = Vector3.zero;
    }

    private Vector3 CalculateInputMovement()
    {
        return inputVelocity;
    }
    private Vector3 CalculateGravity()
    {
        if (verticalVelocity.y < 0)
        {
            verticalVelocity.y += gravity * fallMultiplier * Time.deltaTime;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        // 땅에 닿으면 경사로 고정용 적용
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = groundedGravity;
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
        if (isKnockingBack) { return; }

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
        if (isKnockingBack) return;

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
            player.AnimatorController.PlayAction(AnimHash.Roll);
        }
        else
        {
            player.AnimatorController.PlayAction(AnimHash.BackStep);
        }

    }

    public void StopMovement()
    {
        controller.Move(Vector3.zero);
    }

    public void Groggy()
    {
        player.AnimatorController.PlayAction(AnimHash.GuardBreak);
    }


    //점프
    public void Jump()
    {
        verticalVelocity.y = initialJumpVelocity;
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
