using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyKnockBack : MonoBehaviour
{
    [SerializeField] private LayerMask playerAttackLayerMask;

    [SerializeField] private float knockBackPower;
    [SerializeField] private float knockBackTime;

    private Enemy self;
    private bool needsKnockback = false; // 넉백을 적용해야 할지 여부

    private void Start()
    {
        self = GetComponent<Enemy>();
    }

    private void FixedUpdate()
    {
        if (needsKnockback)
        {
            ApplyKnockbackPhysics(); // 물리 연산을 FixedUpdate에서 수행
        }
    }

    public void KnockBack()
    {
        if (self == null || self.anim == null || self.rb == null || self.NavAgent == null)
        {
            return;
        }

        // 넉백 플래그 설정
        needsKnockback = true; // FixedUpdate에서 넉백 적용되도록 플래그 설정

        // NavMeshAgent 움직임 멈춤 (넉백 적용 전, 또는 넉백 루틴 시작 시 멈춤)
        if (self.NavAgent.isOnNavMesh)
        {
            self.NavAgent.isStopped = true;
        }
    }

    private void ApplyKnockbackPhysics()
    {
        if (self == null || self.rb == null) return;
        self.rb.AddForce(self.hitDirection * knockBackPower, ForceMode.Impulse);

        // 넉백 시간 동안 NavMeshAgent를 비활성화하는 코루틴 시작
        self.knockbackCoroutine = StartCoroutine(KnockbackRoutine(knockBackTime));
    }

    public IEnumerator KnockbackRoutine(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        needsKnockback = false; // 넉백 적용 후 플래그 초기화
    }
}