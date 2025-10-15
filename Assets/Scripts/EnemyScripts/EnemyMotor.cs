using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;

public class EnemyMotor : MonoBehaviour
{
    private NavMeshAgent navAgent;
    private Animator anim;
    private Enemy enemy;
    private CharacterController characterController;

    private bool isKnockingBack = false;
    private float knockbackForce;       // 넉백될 힘
    private float knockbackTimer = 0f;
    private float knockbackDuration;
    private Vector3 knockbackDirection;
    private float rotationSpeed = 10f;


    [SerializeField] private float strafeReplanningDistance = 1.5f; // 이 거리 이내로 가까워지면 다음 경로 재설정
    bool isStrafing;
    float strafeEndTime;
    Transform strafeTarget;
    float strafeDistance;

    public Transform deathblowVictimAnchor;


    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        navAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        enemy.Stats.OnDamaged += KnockBackStart;
    }
    private void OnDestroy()
    {
        enemy.Stats.OnDamaged -= KnockBackStart;
    }

    private void Update()
    {
        if (!isKnockingBack && navAgent.isOnNavMesh && navAgent.hasPath)
        {
            if (!isStrafing)
            {
                navAgent.nextPosition = transform.position;
            }
            characterController.Move(navAgent.velocity * Time.deltaTime);
        }
        UpdateAnimatorParameters();
        HandleKnockBack();
    }


    public void KnockBackStart(DamageInfo damageInfo)
    {
        if (enemy == null || anim == null || navAgent == null)
        {
            return;
        }

        knockbackDirection = damageInfo.hitDirection;
        knockbackForce = damageInfo.knockbackForce;
        knockbackDuration = damageInfo.knockbackDuration;

        knockbackTimer = 0f;

        isKnockingBack = true;

        // NavMeshAgent 움직임 멈춤
        if (navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }

    }

    private void HandleKnockBack()
    {
        if (isKnockingBack)
        {
            knockbackTimer += Time.fixedDeltaTime;
            float deceleration = 1f - (knockbackTimer / knockbackDuration);
            deceleration = Mathf.Clamp01(deceleration);
            characterController.Move(deceleration * knockbackForce * Time.deltaTime * knockbackDirection);

            if (knockbackTimer >= knockbackDuration)
            {
                isKnockingBack = false;
            }
        }
    }

    public void LookAtTarget(Vector3 targetPosition)
    {
        if (isKnockingBack) return;

        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void MoveTo(Vector3 destination)
    {
        if (isKnockingBack || !navAgent.isOnNavMesh) { return; }
        navAgent.SetDestination(destination);
        navAgent.isStopped = false;
    }

    public void Stop()
    {
        if (!navAgent.isOnNavMesh) { return; }
        navAgent.isStopped = true;

        navAgent.ResetPath();
    }

    public void Retreat(float distance)
    {
        if(enemy.Senses.Target == null) { return; }
        Vector3 directionAwayFromTarget = (transform.position - enemy.Senses.Target.position).normalized;

        Vector3 retreatDestination = transform.position + directionAwayFromTarget * distance;

        if(NavMesh.SamplePosition(retreatDestination,out var hit, distance, NavMesh.AllAreas))
        {
            MoveTo(hit.position);
        }
        else
        {
            MoveTo(retreatDestination);
        }
    }

    public void StartStrafe(float duration, Transform Target, float distance)
    {
        isStrafing = true;
        strafeEndTime = Time.time + duration;
        strafeTarget = Target;
        strafeDistance = distance;
        navAgent.stoppingDistance = default;
        navAgent.updateRotation = false;

        CalculateStrafeDestination();
    }

    public void StopStrafe()
    {
        isStrafing = false;
        navAgent.updateRotation = true;
        Stop();
    }

    public void HandleStrafe()
    {
        if (Time.time > strafeEndTime) {
            StopStrafe();
            return;
        }

        if (strafeTarget != null)
        {
            LookAtTarget(strafeTarget.position);
        }

        if (!navAgent.pathPending && navAgent.remainingDistance <= strafeReplanningDistance)
        {
            CalculateStrafeDestination();
        }

    }

    public void CalculateStrafeDestination()
    {
        if (strafeTarget == null) { return; }

        float direction = Random.value > 0.5f ? 1 : -1;

        Vector3 strafeVector = (transform.position - strafeTarget.position).normalized * strafeDistance;
        strafeVector.y = 0;
        Quaternion rotation = Quaternion.Euler(0, 45 * direction, 0);
        Vector3 newVector = rotation * strafeVector;

        Vector3 strafeDestination = strafeTarget.position + newVector;

        if (NavMesh.SamplePosition(strafeDestination, out var hit, 3f, NavMesh.AllAreas))
        {
            MoveTo(hit.position);
        }
    }


    private void UpdateAnimatorParameters()
    {
        if (isKnockingBack || !navAgent.isOnNavMesh)
        {
            // 넉백 중이거나 NavMesh에 없다면 모든 움직임 파라미터를 0으로 고정
            anim.SetFloat("Speed", 0f);
            anim.SetFloat("moveDirX", 0f);
            anim.SetFloat("moveDirZ", 0f);
            return;
        }

        // 1. NavMeshAgent로부터 월드 좌표계 기준의 현재 속도를 가져옵니다.
        Vector3 worldVelocity = navAgent.velocity;

        // 2. 월드 속도를 캐릭터 기준의 로컬 속도로 변환합니다. (가장 중요한 부분)
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        // 3. 각 축의 속도를 최대 속도로 나누어 -1 ~ 1 사이 값으로 정규화합니다.
        float moveDirX = localVelocity.x / navAgent.speed;
        float moveDirZ = localVelocity.z / navAgent.speed;

        // 4. 전체 속력(magnitude)도 0 ~ 1 사이 값으로 정규화합니다.
        float speed = worldVelocity.magnitude / navAgent.speed;

        // 5. 계산된 값들을 애니메이터에 부드럽게 전달합니다.
        anim.SetFloat("moveDirX", moveDirX, 0.1f, Time.deltaTime);
        anim.SetFloat("moveDirZ", moveDirZ, 0.1f, Time.deltaTime);
        anim.SetFloat("Speed", speed, 0.1f, Time.deltaTime); // Speed는 여전히 전체 속도 제어에 유용
    }


    public void PlayAttackAnimation(int attackIndex)
    {
        if (isKnockingBack) return;
        anim.SetInteger("AttackIndex", attackIndex);
        anim.SetTrigger("Attack");
    }
    public void PlayHeavyAttackAnimation(int attackIndex)
    {
        if (isKnockingBack) return;
        anim.SetInteger("AttackIndex", attackIndex);
        anim.SetTrigger("HeavyAttack");
    }
    public void PlayDeathAnimation()
    {
        anim.SetTrigger("Die");
        Stop();
    }

}
