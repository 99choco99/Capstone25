using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMotor : MonoBehaviour
{
    private NavMeshAgent navAgent;

    private int strafeDirection = 1; // 1은 오른쪽, -1은 왼쪽
    private float strafeTimer = 0f;


    [Header("회전 설정")]
    [SerializeField] private float combatRotationSpeed = 15f;
    [SerializeField] private float chaseRotationSpeed = 8f;


    [Header("이동 속도 설정")]
    [SerializeField] private float patrolSpeed = 2f; // 비전투 순찰 속도
    [SerializeField] private float chaseSpeed = 5f;  // 전투 추적 속도
    [SerializeField] private float strafeSpeed = 0.5f;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    // ================== 이동 명령 ==================

    public void MoveTo(Vector3 destination)
    {
        if (!navAgent.isOnNavMesh) { return; }

        navAgent.speed = patrolSpeed;
        navAgent.isStopped = false;
        navAgent.updateRotation = true;
        navAgent.SetDestination(destination);
    }

    public void RotationToDirect(Vector3 dirToTarget)
    {
        dirToTarget.y = 0f;
        if (dirToTarget != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, combatRotationSpeed * Time.deltaTime);
        }
    }

    public void Chase(Vector3 destination)
    {
        if (!navAgent.isOnNavMesh) return;

        navAgent.speed = chaseSpeed;
        navAgent.isStopped = false;
        navAgent.updateRotation = true;
        navAgent.angularSpeed = chaseRotationSpeed * 50f;
        navAgent.SetDestination(destination);
    }

    public void CombatStrafe(Vector3 targetPos, float desiredDistance)
    {
        if (!navAgent.isOnNavMesh) return;

        navAgent.isStopped = false;
        navAgent.updateRotation = false;

        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0)
        {
            strafeDirection = Random.value > 0.5f ? 1 : -1;
            strafeTimer = Random.Range(1.5f, 3.5f);
        }

        Vector3 dirToTarget = (targetPos - transform.position).normalized;
        Vector3 rightOffset = Vector3.Cross(Vector3.up, dirToTarget) * strafeDirection;
        Vector3 destination = targetPos - (dirToTarget * (desiredDistance * 0.8f)) + (rightOffset * 2.5f);

        if(NavMesh.SamplePosition(destination,out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            navAgent.speed = strafeSpeed;
            navAgent.SetDestination(destination);
        }
        else
        {
            strafeDirection *= -1;
            strafeTimer = 0.5f;
        }
        dirToTarget.y = 0;
        if (dirToTarget != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, combatRotationSpeed * Time.deltaTime);
        }
    }

    public void Stop()
    {
        if (!navAgent.isOnNavMesh) return;
        navAgent.isStopped = true;
        navAgent.ResetPath();
        navAgent.velocity = Vector3.zero;
    }
    // ================== 애니메이션 동기화 =================

    public Vector2 GetNormalizedVelocity()
    {
        float safeSpeed = navAgent.speed > 0f ? navAgent.speed : 1f;
        Vector3 worldVelocity = navAgent.velocity;
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        float forward = localVelocity.z / safeSpeed;
        float right = localVelocity.x / safeSpeed;

        return new Vector2(right, forward);
    }



    // ================== 넉백 처리 ==================

    public void ApplyForce(Vector3 velocity)
    {
        if (!navAgent.isOnNavMesh) return;
        navAgent.Move(velocity * Time.deltaTime);
    }
}
