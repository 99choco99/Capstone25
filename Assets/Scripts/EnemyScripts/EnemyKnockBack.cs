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
    private bool needsKnockback = false; // 넉백을 적용해야 할지 여부

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

            // 부드러운 감속을 위해 곡선 사용 (예: Ease-Out)
            // t = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease-Out Sine
            t = 1f - (1f - t) * (1f - t); // Ease-Out Quad (조금 더 빠르게 감속)

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


    public void KnockBack()
    {
        if (self == null || self.anim == null || self.rb == null || self.NavAgent == null)
        {
            return;
        }
        self.rb.linearVelocity = Vector3.zero;
        isKnockingBack = true;

        // NavMeshAgent 움직임 멈춤
        if (self.NavAgent.isOnNavMesh)
        {
            self.NavAgent.isStopped = true;
        }
        // 넉백 시간 동안 NavMeshAgent를 비활성화하는 코루틴 시작
        //StartCoroutine(KnockbackRoutine(knockBackTime));
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (isKnockingBack) return; // 이미 넉백 중이면 중복 적용 방지

        knockbackDirection = direction.normalized; // 방향을 정규화하여 일관된 힘 적용
        knockbackForce = force;
        knockbackDuration = duration;

        startKnockbackPosition = transform.position; // 넉백 시작 위치 저장
        knockbackTimer = 0f;
        isKnockingBack = true;

        Debug.Log($"Starting Kinematic Knockback. Dir: {direction}, Force: {force}, Duration: {duration}");
    }

    //public IEnumerator KnockbackRoutine(float duration)
    //{
    //    float timer = 0f;
    //    Vector3 startPosition = transform.position;
    //    Vector3 targetPosition = startPosition + self.hitDirection.normalized * knockBackPower;
    //    while (timer < duration)
    //    {
    //        timer += Time.deltaTime;
    //        float t = timer / duration; // 0에서 1까지 증가하는 시간 비율

    //        // 부드러운 감속을 위해 곡선 사용
    //        t = 1f - (1f - t) * (1f - t); // Ease-Out Quad

    //        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);
    //        yield return null;
    //    }
    //    needsKnockback = false; // 넉백 적용 후 플래그 초기화
    //    self.NavAgent.isStopped = false;
    //}
}