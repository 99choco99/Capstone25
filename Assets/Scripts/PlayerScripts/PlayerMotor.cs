using System.Collections;
using UnityEngine;


public class PlayerMotor : MonoBehaviour
{

    public CharacterController controller;
    private Player player;


    [SerializeField] private float rotationSpeed = 15f;

    [Header("움직임")]
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
    public bool IsGrounded = true;

    [Header("넉백")]
    private bool isKnockingBack = false;
    private float knockBackTimer;
    private float knockBackDuration;
    private Vector3 knockbackMovement;

    [Header("백스탭")]
    public Vector3 rollDirection;


    public bool canMove = true;
    public bool canRotate = true;
    public Coroutine movementLockCoroutine;


    // 네트워크 전송 주기 관리를 위한 변수
    private float lastSendTime = 0f;
    private float sendInterval = 0.1f; // 0.1초 간격으로 전송 (초당 10번)

    // 마지막으로 전송한 위치/회전 값을 저장할 변수
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        player = GetComponent<Player>();
    }

    private void Start()
    {
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
        if (player.Combat.IsPlayingDirector) { 
            canMove = false;
            canRotate = false;
            return; 
        }
        HandleGroundCheck();
        HandleGravity();

        HandleKnockBack();
        HandleRotation();
    }



    //기본적인 이동
    public void Move()
    {
        if (!player.IsLocalPlayer) { return; }
        if (!canMove || isKnockingBack) return;

        Vector3 cameraForward = player.MainCamera.transform.forward;
        cameraForward.y = 0f;
        Vector3 cameraRight = player.MainCamera.transform.right;
        cameraRight.y = 0f;

        Vector3 moveDirection = (cameraForward * player.InputHandler.MoveInput.z + cameraRight * player.InputHandler.MoveInput.x).normalized;

        if (player.StateMachine.CurrentState == player.StateMachine.PlayerSprintState)
        {
            controller.Move(SprintSpeed * Time.deltaTime * moveDirection);
        }
        else if(player.StateMachine.CurrentState == player.StateMachine.PlayerGuardState)
        {
            controller.Move(GuardSpeed * Time.deltaTime * moveDirection);
        }
        else
        {
            controller.Move(MoveSpeed * Time.deltaTime * moveDirection);
        }

        if (Time.time - lastSendTime > sendInterval)
        {

            if (Vector3.Distance(transform.position, lastSentPosition) > 0.01f ||
                Quaternion.Angle(transform.rotation, lastSentRotation) > 0.1f)
            {

                NetworkManager.instance.socket.EmitPlayerMovement(transform.position, transform.rotation);

                lastSendTime = Time.time;
                lastSentPosition = transform.position;
                lastSentRotation = transform.rotation;
            }
        }
    }

    //백스텝 및 구르기
    public void Dodge()
    {
        //구르기 shift 
        if (player.InputHandler.MoveInput != Vector3.zero)
        {
            Vector3 cameraForward = player.MainCamera.transform.forward;
            cameraForward.y = 0f;
            Vector3 cameraRight = player.MainCamera.transform.right;
            cameraRight.y = 0f;

            Vector3 rollDirection = (cameraForward.normalized * player.InputHandler.MoveInput.z + cameraRight.normalized * player.InputHandler.MoveInput.x).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(rollDirection);
            transform.rotation = targetRotation;
            player.animatorManager.PlayTargetActionAnimation("Roll", true);
        }
        else
        {
            player.animatorManager.PlayTargetActionAnimation("BackStep", true);
        }

    }




    public void LockMovementFor(float duration)
    {
        if(movementLockCoroutine != null)
        {
            StopCoroutine(movementLockCoroutine);
        }
        movementLockCoroutine = StartCoroutine(MovementLockCoroutine(duration));
    }

    private IEnumerator MovementLockCoroutine(float duration)
    {
        canMove = false;
        canRotate = false;
        yield return new WaitForSeconds(duration);
        canMove = true;
        canRotate = true;
        movementLockCoroutine = null;
    }


    public void StopMovement()
    {
        controller.Move(Vector3.zero);
    }

    public void Groggy()
    {
        player.animatorManager.PlayTargetActionAnimation("GuardBreak", true, true);
    }


    //점프
    public void Jump(float jumpForce)
    {
        if (!IsGrounded || !canMove || isKnockingBack) return;

        // y축 속도에 점프 힘을 직접 더해줌
        verticalVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
    }

    //지면 체크
    public void HandleGroundCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z);
        IsGrounded = Physics.CheckSphere(spherePosition, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);

    }


    // 중력 처리 함수
    private void HandleGravity()
    {
        verticalVelocity.y += 2f * gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (player.StateMachine.CurrentState == player.StateMachine.PlayerRollingState)
        {
            return;
        }
        if (!canRotate || isKnockingBack) { return; }

        if (player.isLockOn)
        {
            if (player.TargetingSystem.CurrentTarget == null) { return; }
            Vector3 targetDirection = player.TargetingSystem.CurrentTarget.transform.position - transform.position;
            RotateTowardsDirection(targetDirection);
        }
        else
        {
            Vector3 cameraForward = player.MainCamera.transform.forward;
            Vector3 cameraRight = player.MainCamera.transform.right;

            Vector3 RotationDirection = (cameraForward.normalized * player.InputHandler.MoveInput.z + cameraRight.normalized * player.InputHandler.MoveInput.x);

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


    //넉백 시작
     public void StartKnockBack(DamageInfo damageInfo)
     {
        isKnockingBack = true;
        knockBackTimer = 0f;
        knockBackDuration = damageInfo.knockbackDuration;
        knockbackMovement = damageInfo.hitDirection * damageInfo.knockbackForce; // 초당 넉백될 이동량 계산

     }

    //넉백
    public void HandleKnockBack()
    {
        if (!isKnockingBack) return;

        knockBackTimer += Time.fixedDeltaTime;
        float deceleration = 1f - (knockBackTimer / knockBackDuration);
        deceleration = Mathf.Clamp01(deceleration);
        controller.Move(deceleration * Time.deltaTime * knockbackMovement);

        if (knockBackTimer >= knockBackDuration)
        {
            isKnockingBack = false;
        }
    }

    //특정 물체 바라보기
    public void RotateToward(Transform target)
    {
        StartCoroutine(RotateCoroutine(target));
    }

    IEnumerator RotateCoroutine(Transform target)
    {
        float timer = 0f;
        float duration = 0.5f;
        Vector3 dir = target.position - transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer/ duration;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, progress);
            yield return null;
        }
        transform.rotation = targetRot;
    }

    public void AE_playerMoveDisable()
    {
        canMove = false;
        canRotate = true;
    }

}
