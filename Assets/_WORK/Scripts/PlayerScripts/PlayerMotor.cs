using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{

    public CharacterController Controller { get;private set;}

    [Header("움직임 설정")]
    public float MoveSpeed = 5f;
    public float SprintSpeed = 8f;
    public float GuardSpeed = 2f;
    public float rotationSpeed = 15f;

    [Header("점프 및 중력처리")]
    [Tooltip("캐릭터가 뛸 최고 높이")]
    [SerializeField] float jumpHeight = 2.0f;
    [Tooltip("점프 후 정점까지 도달하는 시간")]
    [SerializeField] float timeToJump = 0.4f;
    [Tooltip("떨어질 때 묵직함을 주는 배수")]
    [SerializeField] float fallMultiplier = 2.5f;
    [Tooltip("경사로에서 허공에 안 뜨게 잡아주는 힘")]
    [SerializeField] float groundedGravity = -5.0f;

    [Header("지면 체크")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float groundCheckDistance = 0.15f;

    // 내부 계산용 변수
    public float Gravity { get; private set; }
    public float InitialJumpVelocity { get; private set; }
    private Vector3 verticalVelocity;
    private Vector3 inputVelocity;

    private readonly KnockbackMotion knockbackMotion = new();
    public float CurrentVerticalVelocity => verticalVelocity.y;



    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        Controller = GetComponent<CharacterController>();

        Gravity = -(2 * jumpHeight) / (timeToJump * timeToJump);
        InitialJumpVelocity = Mathf.Abs(Gravity) * timeToJump;
    }

    private void Update()
    {
        HandleGroundCheck();
        ApplyMovement();
    }

    /// <summary>
    /// 움직임 최종 적용
    /// </summary>
    public void ApplyMovement()
    {
        float deltaTime = Time.deltaTime;
        Vector3 velocity = inputVelocity + CalculateGravity();
        Vector3 frameDisplacement = velocity * deltaTime;

        frameDisplacement += knockbackMotion.Start(deltaTime);
        Controller.Move(frameDisplacement);

        inputVelocity = Vector3.zero;
    }

    /// <summary>
    /// 중력
    /// </summary>
    private Vector3 CalculateGravity()
    {
        // 땅에 닿으면 경사로 고정용 적용
        if (IsGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = groundedGravity;
        }
        else
        {
            if (verticalVelocity.y < 0)
            {
                verticalVelocity.y += Gravity * fallMultiplier * Time.deltaTime;
            }
            else
            {
                verticalVelocity.y += Gravity * Time.deltaTime;
            }
        }

         return verticalVelocity;
    }

    /// <summary>
    /// 지면 체크
    /// </summary>
    public void HandleGroundCheck()
    {
        float radius = Controller.radius * 0.9f;
        Vector3 sphereStart = transform.position + Vector3.up * (radius + Controller.skinWidth + 0.05f);
        float castDistance = (Controller.skinWidth + 0.05f) + groundCheckDistance;
        if (Physics.SphereCast(sphereStart, radius, Vector3.down, out RaycastHit hit, castDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Angle(hit.normal, Vector3.up) <= Controller.slopeLimit)
            {
                IsGrounded = true;
                return;
            }
        }
        IsGrounded = false;
    }


    //기본적인 움직임
    public void SetMovement(Vector3 moveDirection)
    {
        inputVelocity = moveDirection;
    }
    
    /// <summary>
    /// 정해진 방향과 거리로 넉백 시작
    /// </summary>
    public void StartKnockback(Vector3 direction, KnockbackSpec spec)
    {
        knockbackMotion.Ready(direction, spec);
    }

    /// <summary>
    /// 넉백 종료
    /// </summary>
    public void StopKnockback()
    {
        knockbackMotion.Stop();
    }


    /// <summary>
    /// 루트모션시 움직임
    /// </summary>
    public void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        Controller.Move(deltaPosition);
        transform.rotation *= deltaRotation;
    }

    /// <summary>
    /// 정해진 위치로 transform 설정
    /// </summary>
    public void SetTransform(Vector3 position, Quaternion rotation)
    {
        StopKnockback();

        Controller.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        Controller.enabled = true;
    }

    /// <summary>
    /// 점프
    /// </summary>
    public void Jump()
    {
        if (IsGrounded)
        {
            verticalVelocity.y = InitialJumpVelocity;
        }
    }

    /// <summary>
    /// 특정 방향으로 회전처리
    /// </summary>
    public void RotateToDirection(Vector3 direction)
    {
        direction.y = 0;
        if (direction.sqrMagnitude < 0.01f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
