using UnityEngine;

public class PlayerMotor : MonoBehaviour
{

    public CharacterController controller { get;private set;}

    [Header("움직임 설정")]
    public float MoveSpeed = 5f;
    public float SprintSpeed = 8f;
    public float GuardSpeed = 2f;
    public float rotationSpeed = 15f;

    [Header("점프 및 중력처리")]
    [Tooltip("캐릭터가 뛸 최고 높이 (미터 단위)")]
    [SerializeField] float jumpHeight = 2.0f; // 소울라이크 국룰: 1.5 ~ 2.0
    [Tooltip("점프 후 정점까지 도달하는 시간 (초)")]
    [SerializeField] float timeToJumpApex = 0.4f; // 소울라이크 국룰: 0.3 ~ 0.45
    [Tooltip("떨어질 때 묵직함을 주는 배수")]
    [SerializeField] float fallMultiplier = 2.5f; // 액션 게임 국룰: 2.0 ~ 3.0
    [Tooltip("경사로에서 허공에 안 뜨게 잡아주는 힘")]
    [SerializeField] float groundedGravity = -5.0f; // 국룰: -2.0 ~ -5.0

    [Header("지면 체크")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float groundCheckDistance = 0.15f;

    // 내부 계산용 변수
    public float Gravity { get; private set; }
    public float InitialJumpVelocity { get; private set; }
    private Vector3 verticalVelocity;
    private Vector3 knockbackVelocity;
    private Vector3 inputVelocity;


    private Vector3 groundNormal = Vector3.up;

    public float CurrentVerticalVelocity => verticalVelocity.y;



    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        Gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        InitialJumpVelocity = Mathf.Abs(Gravity) * timeToJumpApex;
    }

    private void Update()
    {
        HandleGroundCheck();
        ApplyMovement();
    }

    public void ApplyMovement()
    {
        Vector3 finalMovement = Vector3.zero;

        finalMovement += inputVelocity;
        finalMovement += knockbackVelocity;
        finalMovement += CalculateGravity();

        controller.Move(finalMovement * Time.deltaTime);

        inputVelocity = Vector3.zero;
    }

    //중력
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

    //지면 체크
    public void HandleGroundCheck()
    {
        float radius = controller.radius * 0.9f;
        Vector3 sphereStart = transform.position + Vector3.up * (radius + controller.skinWidth + 0.05f);
        float castDistance = (controller.skinWidth + 0.05f) + groundCheckDistance;
        if (Physics.SphereCast(sphereStart, radius, Vector3.down, out RaycastHit hit, castDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Angle(hit.normal, Vector3.up) <= controller.slopeLimit)
            {
                IsGrounded = true;
                groundNormal = hit.normal;
                return;
            }
        }
        IsGrounded = false;
        groundNormal = Vector3.up;
    }


    //기본적인 움직임
    public void SetMovement(Vector3 moveDirection)
    {
        inputVelocity = moveDirection;
    }
    
    //넉백값 계산
    public void SetKnockbackVelocity(Vector3 knockbackVel)
    {
        knockbackVelocity = knockbackVel;
    }


    //루트모션시 움직임
    public void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        controller.Move(deltaPosition);
        transform.rotation *= deltaRotation;
    }

    //점프
    public void Jump()
    {
        if (IsGrounded)
        {
            verticalVelocity.y = InitialJumpVelocity;
        }
    }

    //회전처리
    public void RotateToDirection(Vector3 direction)
    {
        direction.y = 0;
        if (direction.sqrMagnitude < 0.01f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

}
