using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;

public class PlayerMotor : MonoBehaviour
{

    public CharacterController controller;
    private Player player;


    private float moveAmount;
    [SerializeField] private float rotationSpeed = 15f;

    //중력 및 점프 처리를 위한 변수
    private Vector3 verticalVelocity;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundCheckDistance = 1f; 
    [SerializeField] private LayerMask groundLayer;
    public bool IsGrounded = true;



    public bool canMove = true;
    public bool canRotate = true;
    private Coroutine movementLockCoroutine;

    private bool isKnockingBack = false;
    private float knockBackTimer;
    private float knockBackDuration;
    private Vector3 knockbackMovement;




    // [추가] 네트워크 전송 주기 관리를 위한 변수
    private float lastSendTime = 0f;
    private float sendInterval = 0.1f; // 0.1초 간격으로 전송 (초당 10번)

    // [추가] 마지막으로 전송한 위치/회전 값을 저장할 변수
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        player = GetComponent<Player>();
    }


    private void Update()
    {
        HandleGroundCheck();
        HandleGravity();

        HandleRotation();
        HandleKnockBack();
    }

    private void LateUpdate()
    {
        UpdateAnimMoveParameter();
    }

    // 3. 중력 처리 함수 추가
    private void HandleGravity()
    {
        if (IsGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; // 땅에 붙어있도록 살짝 아래로 힘을 줌
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }


    public void Move()
    {
        if (!canMove || isKnockingBack) return;

        Vector3 cameraForward = player.MainCamera.transform.forward;
        cameraForward.y = 0f;
        Vector3 cameraRight = player.MainCamera.transform.right;
        cameraRight.y = 0f;

        Vector3 moveDirection = (cameraForward.normalized * player.InputHandler.MoveInput.z + cameraRight.normalized * player.InputHandler.MoveInput.x).normalized;

        if(player.InputHandler.moveAmount > 0.5f)
        {
            controller.Move(player.Stats.MoveSpeed * Time.deltaTime * moveDirection);
        }else if(player.InputHandler.moveAmount <= 0.5f)
        {
            controller.Move(player.Stats.RunSpeed * Time.deltaTime * moveDirection);
        }




        // 1. 설정된 전송 주기(0.1초)가 지났는지 확인합니다.
        if (Time.time - lastSendTime > sendInterval)
        {
            // 2. 마지막으로 보냈던 위치나 회전 값과 현재 값에 변화가 있는지 확인합니다.
            if (Vector3.Distance(transform.position, lastSentPosition) > 0.01f ||
                Quaternion.Angle(transform.rotation, lastSentRotation) > 0.1f)
            {
                // 3. 두 조건이 모두 만족될 때만 서버로 데이터를 전송합니다.
                SocketManager.instance.EmitPlayerMovement(transform.position, transform.rotation);

                // 마지막 전송 시간과 상태를 현재 시간과 상태로 기록합니다.
                lastSendTime = Time.time;
                lastSentPosition = transform.position;
                lastSentRotation = transform.rotation;
            }
        }
    }
    private void UpdateAnimMoveParameter()
    {
        float horizontalInput = player.InputHandler.MoveInput.x;
        float verticalInput = player.InputHandler.MoveInput.z;

        player.Anim.SetFloat("Horizontal", horizontalInput, 0.1f, Time.deltaTime);
        player.Anim.SetFloat("Vertical", verticalInput, 0.1f, Time.deltaTime);
        // 만약 걷기/달리기를 구분하는 'Speed' 파라미터도 Animator에 있다면 아래 코드를 추가합니다.
        // player.Anim.SetFloat("Speed", moveAmount, 0.1f, Time.deltaTime);
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

    public void Jump(float jumpForce)
    {
        if (!IsGrounded || !canMove || isKnockingBack) return;

        // y축 속도에 점프 힘을 직접 더해줌
        verticalVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
    }

    //지면 체크
    public void HandleGroundCheck()
    {

            // 1. 구체를 쏠 시작 위치를 정합니다. 
            //    캐릭터의 현재 위치에서 살짝 위로 올립니다. (컨트롤러 높이의 일부)
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z);

            // 2. 캐릭터의 발밑으로 짧은 거리(groundCheckDistance)만큼 구체를 쏴서 
            //    'groundLayer'에 해당하는 물체가 있는지 확인합니다.
            IsGrounded = Physics.CheckSphere(spherePosition, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void HandleRotation()
    {
        if (!canRotate || isKnockingBack) { return; }

        if (PlayerCamera.Instance.isLockOn)
        {
            //sprint 는 예외
            if (player.InputHandler.SprintInput)
            {
                Vector3 targetDirection = Vector3.zero;
                targetDirection = player.MainCamera.transform.forward * player.InputHandler.MoveInput.z;
                targetDirection += player.MainCamera.transform.right * player.InputHandler.MoveInput.x;

                RotateTowardsDirection(targetDirection);
            }
            else
            {
                if(player.TargetingSystem.CurrentTarget == null) { return; }
                Vector3 targetDirection = Vector3.zero;
                targetDirection = player.TargetingSystem.CurrentTarget.transform.position - transform.position;

                RotateTowardsDirection(targetDirection);
            }
        }

        Vector3 cameraForward = player.MainCamera.transform.forward;
        Vector3 cameraRight = player.MainCamera.transform.right;

        Vector3 RotationDirection = (cameraForward.normalized * player.InputHandler.MoveInput.z + cameraRight.normalized * player.InputHandler.MoveInput.x);

        // 계산된 방향으로 부드럽게 회전
        RotateTowardsDirection(RotationDirection);
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
     public void StartKnockBack(Vector3 direction, float force, float duration)
    {
        isKnockingBack = true;
        knockBackTimer = 0f;
        knockBackDuration = duration;
        knockbackMovement = direction * force; // 초당 넉백될 이동량 계산

    }
    //넉백
    public void HandleKnockBack()
    {
        if (!isKnockingBack) return;

        knockBackTimer += Time.fixedDeltaTime;
        controller.Move(knockbackMovement * Time.deltaTime);

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

    public void AE_playerMoveEnable()
    {
        canRotate = true;
        canMove = true;
    }
    public void AE_playerMoveDisable()
    {
        canMove = false;
        canRotate = true;
    }

    private void OnAnimatorMove()
    {
        // 현재 상태가 존재하고, 현재 상태가 루트 모션을 사용하겠다고 선언한 경우에만 실행
        if (player.StateMachine.CurrentState != null && player.StateMachine.CurrentState.UseRootMotion)
        {
            // 애니메이터가 이번 프레임에 계산한 이동량(deltaPosition)과 회전량(deltaRotation)을
            // 우리가 직접 Rigidbody에 적용합니다.
            controller.Move(player.Anim.deltaPosition);
            transform.rotation *= player.Anim.deltaRotation;
        }
    }
}
