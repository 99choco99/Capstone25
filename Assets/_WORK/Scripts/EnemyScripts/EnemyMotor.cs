using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy의 움직임을 정의
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMotor : MonoBehaviour
{
    [Header("회전")]
    [SerializeField, Min(0f)] private float combatRotationSpeed = 15f;
    [SerializeField, Min(0f)] private float chaseRotationSpeed = 8f;

    [Header("이동 속도")]
    [SerializeField, Min(0f)] private float patrolSpeed = 2f;
    [SerializeField, Min(0f)] private float chaseSpeed = 3f;
    [SerializeField, Min(0f)] private float strafeSpeed = 0.5f;

    [Header("교전 위치 보정")]
    [Tooltip("타깃 주위를 도는 각도")]
    [SerializeField, Range(5f, 60f)] private float strafeArcAngle = 24f;
    [Tooltip("방향 유지 최소 시간")]
    [SerializeField, Range(0f, 10f)] private float minStrafeTime = 1.5f;
    [Tooltip("방향 유지 최대 시간")]
    [SerializeField, Range(0f, 10f)] private float maxStrafeTime = 3.5f;
    [Tooltip("strafe 목적지 계산 간격, 작을수록 촘촘한 원형")]
    [SerializeField, Min(0.05f)] private float strafeRefreshTime = 0.25f;

    private NavMeshAgent navAgent;


    //strafe
    private int strafeDirection = 1;
    private float strafeDirectionTimer;
    private float strafeDestinationTimer;
    private bool hasStrafeDestination;

    //넉백
    private readonly KnockbackMotion knockbackMotion = new();


    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!knockbackMotion.IsActive)
            return;

        if (!CanUseAgent())
        {
            knockbackMotion.Stop();
            return;
        }

        Vector3 frameDisplacement = knockbackMotion.Start(Time.deltaTime);
        if (frameDisplacement.sqrMagnitude > 0f)
            navAgent.Move(frameDisplacement);
    }

    /// <summary>지정한 위치로 이동</summary>
    public void MoveTo(Vector3 destination)
    {
        if (!CanUseAgent()) return;

        navAgent.speed = patrolSpeed;
        navAgent.stoppingDistance = 0f;
        navAgent.isStopped = false;
        navAgent.updateRotation = true;
        navAgent.SetDestination(destination);
    }

    /// <summary>지정한 방향으로 회전</summary>
    public void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, combatRotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 추격
    /// </summary>
    public void Chase(Vector3 destination, float stoppingDistance)
    {
        if (!CanUseAgent()) return;

        navAgent.speed = chaseSpeed;
        navAgent.stoppingDistance = Mathf.Max(0f, stoppingDistance);
        navAgent.isStopped = false;
        navAgent.updateRotation = true;
        navAgent.angularSpeed = chaseRotationSpeed * 50f;
        navAgent.SetDestination(destination);
    }

    ///<summary>대상을 바라보면서 교전 거리를 유지하도록 좌우 이동</summary>
    public void Strafe(Vector3 targetPosition, float distance)
    {
        if (!CanUseAgent()) return;

        navAgent.isStopped = false;
        navAgent.updateRotation = false;
        navAgent.stoppingDistance = 0.1f;

        strafeDirectionTimer -= Time.deltaTime;
        strafeDestinationTimer -= Time.deltaTime;

        if (strafeDirectionTimer <= 0f)
        {
            strafeDirection = Random.value > 0.5f ? 1 : -1;
            strafeDirectionTimer = Random.Range(minStrafeTime, maxStrafeTime);
            strafeDestinationTimer = 0f;
        }

        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0f;
        if (directionToTarget.sqrMagnitude < 0.0001f)
            directionToTarget = transform.forward;
        directionToTarget.Normalize();

        //목적지에 도달했는지
        bool reachedDestination = !navAgent.pathPending && navAgent.hasPath && navAgent.remainingDistance <= 0.2f;

        if (!hasStrafeDestination || strafeDestinationTimer <= 0f || reachedDestination)
        {
            Vector3 nextDirection = Quaternion.AngleAxis(strafeDirection * strafeArcAngle, Vector3.up) * -directionToTarget.normalized;
            Vector3 destination = targetPosition + nextDirection * distance;

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                navAgent.speed = strafeSpeed;
                navAgent.SetDestination(hit.position);
                hasStrafeDestination = true;
                strafeDestinationTimer = strafeRefreshTime;
            }
            else
            {
                strafeDirection *= -1;
                strafeDirectionTimer = 0.5f;
                strafeDestinationTimer = 0f;
            }
        }

        RotateTowards(directionToTarget);
    }

    /// <summary>
    /// 정지
    /// </summary>
    public void Stop()
    {
        if (!CanUseAgent()) return;

        navAgent.isStopped = true;
        if (navAgent.hasPath) navAgent.ResetPath();
        navAgent.velocity = Vector3.zero;
        hasStrafeDestination = false;
        strafeDestinationTimer = 0f;
    }

    /// <summary>
    /// 현재 이동 속도를 애니메이션용 로컬 축 값으로 변환
    /// </summary>
    public Vector2 GetNormalizedVelocity()
    {
        if (!CanUseAgent()) return Vector2.zero;

        float safeSpeed = navAgent.speed > 0f ? navAgent.speed : 1f;
        Vector3 localVelocity = transform.InverseTransformDirection(navAgent.velocity);
        return new Vector2(localVelocity.x / safeSpeed, localVelocity.z / safeSpeed);
    }

    /// <summary>
    /// 방향과 최종 넉백 사양을 받아 NavMeshAgent 이동을 시작
    /// </summary>
    public void StartKnockback(Vector3 direction, KnockbackSpec spec)
    {
        knockbackMotion.Ready(direction, spec);
    }

    /// <summary>
    /// 넉백 멈춤
    /// </summary>
    public void StopKnockback()
    {
        knockbackMotion.Stop();
    }

    /// <summary>Navagent 종료</summary>
    public void DisableAgent()
    {
        StopKnockback();

        navAgent.enabled = false;
    }

    /// <summary>
    /// NavAgent 사용 가능한지
    /// </summary>
    private bool CanUseAgent()
    {
        return navAgent.enabled && navAgent.isOnNavMesh;
    }
}
