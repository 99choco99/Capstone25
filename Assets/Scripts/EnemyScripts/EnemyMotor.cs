using UnityEngine;
using UnityEngine.AI;

public class EnemyMotor : MonoBehaviour
{
    private NavMeshAgent navAgent;
    private Animator anim;
    private Rigidbody rb;
    private Enemy enemy;

    private bool isKnockingBack = false;
    private float knockbackForce;       // 넉백될 힘
    private float knockbackTimer = 0f;
    private float knockbackDuration;
    private Vector3 startKnockbackPosition;
    private Vector3 knockbackDirection;


    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemy = GetComponent<Enemy>();

        enemy.Stats.OnDamaged += KnockBackStart;
    }

    private void OnDestroy()
    {
        enemy.Stats.OnDamaged -= KnockBackStart;
    }

    private void FixedUpdate()
    {
        HandleKnockBack();
    }

    public void KnockBackStart(DamageInfo damageInfo)
    {
        if (enemy == null || anim == null || rb == null || navAgent == null)
        {
            return;
        }

        knockbackDirection = damageInfo.hitDirection;
        knockbackForce = damageInfo.knockbackForce;
        knockbackDuration = damageInfo.knockbackDuration;

        startKnockbackPosition = transform.position; // 넉백 시작 위치 저장
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
            float t = knockbackTimer / knockbackDuration; // 0에서 1까지 증가하는 시간 비율
            t = 1f - (1f - t) * (1f - t);

            // 시작 위치에서 목표 위치까지 Lerp
            Vector3 targetPos = startKnockbackPosition + knockbackDirection * knockbackForce;
            Vector3 currentPos = Vector3.Lerp(startKnockbackPosition, targetPos, t);

            // Rigidbody.MovePosition 사용 (물리 업데이트에 적합)
            rb.MovePosition(currentPos);

            if (knockbackTimer >= knockbackDuration)
            {
                isKnockingBack = false;
                // 필요하다면 넉백 종료 후 상태 복구 로직 추가
                Debug.Log("Knockback Finished!");
            }
        }
    }
}
