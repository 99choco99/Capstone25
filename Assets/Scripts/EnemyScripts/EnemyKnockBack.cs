using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class EnemyKnockBack : MonoBehaviour
{
    private Vector3 knockbackDirection; // 넉백될 방향
    private float knockbackForce;       // 넉백될 거리 또는 속도
    private float knockbackDuration;    // 넉백이 지속될 시간

    private bool isKnockingBack = false;
    private Vector3 startKnockbackPosition;
    private float knockbackTimer = 0f;

    private Enemy self;

    private void Start()
    {
        self = GetComponent<Enemy>();
    }

    void FixedUpdate() // 넉백 이동은 FixedUpdate에서 처리하는 것이 좋음
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
            self.rb.MovePosition(currentPos);

            if (knockbackTimer >= knockbackDuration)
            {
                isKnockingBack = false;
                // 필요하다면 넉백 종료 후 상태 복구 로직 추가
                Debug.Log("Knockback Finished!");
            }
        }
    }


    public void KnockBack(Vector3 direction, float force, float duration)
    {
        if (self == null || self.anim == null || self.rb == null || self.NavAgent == null)
        {
            return;
        }

        knockbackDirection = direction.normalized;
        knockbackForce = force;
        knockbackDuration = duration;

        startKnockbackPosition = transform.position; // 넉백 시작 위치 저장
        knockbackTimer = 0f;

        isKnockingBack = true;

        // NavMeshAgent 움직임 멈춤
        if (self.NavAgent.isOnNavMesh)
        {
            self.NavAgent.isStopped = true;
        }

    }
}