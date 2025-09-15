using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMotor : MonoBehaviour
{

    public Rigidbody rb;
    private Player player;



    [SerializeField] private float rotationSpeed = 15f;


    [SerializeField] private float groundCheckDistance = 1f; 
    [SerializeField] private LayerMask groundLayer;
    public bool IsGrounded = true;


    private bool isKnockingBack = false;
    private float knockBackTimer;
    private float knockBackDuration;
    private Vector3 knockbackStartPosition;
    private Vector3 knockbackTargetPosition;


    public bool canMove = true;
    public bool canRotate = true;
    private Coroutine movementLockCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        player = GetComponent<Player>();
    }

    private void Update()
    {
        UpdateAnimSpeedParameter();
    }

    private void FixedUpdate()
    {
        HandleGroundCheck();
        HandleRotation();
        HandleKnockBack();
    }



    public void Move(Vector3 moveInput, float speed)
    {
        if (!canMove || isKnockingBack) return; 

        Vector3 cameraForward = player.MainCamera.transform.forward;
        cameraForward.y = 0f;
        Vector3 cameraRight = player.MainCamera.transform.right;
        cameraRight.y = 0;

        Vector3 moveDirection = (cameraForward.normalized * moveInput.z + cameraRight.normalized * moveInput.x).normalized;

        Vector3 targetVelocity = new Vector3(moveDirection.x * speed,  rb.linearVelocity.y , moveDirection.z * speed);
        rb.linearVelocity = targetVelocity;
    }
    private void UpdateAnimSpeedParameter()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        if (currentSpeed <= 0f || !canMove || isKnockingBack)
        {
            currentSpeed = 0;
            player.Anim.SetFloat("Speed", 0);
            return;
        }

        float normalizedSpeed = currentSpeed / player.Stats.SprintSpeed;

        player.Anim.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);
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
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public void Jump(float jumpForce)
    {
        if (!IsGrounded || !canMove || isKnockingBack) return;

        // y축 속도를 리셋하여 연속 점프 시 힘이 누적되는 것을 방지
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Impulse 모드는 순간적으로 큰 힘을 가해 오브젝트를 튀어 오르게 함
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

    }

    private void HandleRotation()
    {
        if (!canRotate || isKnockingBack) { return; }

        Vector3 moveInputDirection = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).normalized;

        if (canRotate && player.TargetingSystem.CurrentTarget == null)
        {
            Vector3 cameraForward = player.MainCamera.transform.forward;
            cameraForward.y = 0;
            Vector3 cameraRight = player.MainCamera.transform.right;
            cameraRight.y = 0;
            Vector3 lookDirection = (cameraForward.normalized * player.InputHandler.MoveInput.z + cameraRight.normalized * player.InputHandler.MoveInput.x).normalized;

            RotateTowardsDirection(lookDirection);
        }
        else if (player.TargetingSystem.CurrentTarget != null)
        {
            Vector3 targetDir = (player.TargetingSystem.CurrentTarget.transform.position - transform.position).normalized;
            RotateTowardsDirection(targetDir);
        }
        else if (moveInputDirection != Vector3.zero)
        {
            RotateTowardsDirection(moveInputDirection);
        }
    }

    public void RotateTowardsDirection(Vector3 direction)
    {
        direction.y = 0;
        if (direction == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    //지면 체크
    public void HandleGroundCheck()
    {
        if(rb.linearVelocity.y > 1f) { 
            IsGrounded = false; 
            return; 
        }
        IsGrounded = Physics.SphereCast(transform.position + Vector3.up, 0.1f, Vector3.down, out _, groundCheckDistance,groundLayer);
    }


    
    //넉백 시작
     public void StartKnockBack(Vector3 direction, float force, float duration)
    {
        isKnockingBack = true;
        knockBackTimer = 0f;
        knockBackDuration = duration;
        knockbackStartPosition = transform.position;
        knockbackTargetPosition = knockbackStartPosition + direction * force;
        rb.linearVelocity = Vector3.zero;
        
    }
    //넉백
    public void HandleKnockBack()
    {
        if (!isKnockingBack) return;

        knockBackTimer += Time.fixedDeltaTime;
        float t = knockBackTimer / knockBackDuration; // 0에서 1까지 증가하는 시간 비율
        t = 1f - (1f - t) * (1f - t);

        rb.MovePosition(Vector3.Lerp(knockbackStartPosition, knockbackTargetPosition, t));


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
            rb.MovePosition(rb.position + player.Anim.deltaPosition);
            transform.rotation *= player.Anim.deltaRotation;
        }
    }
}
