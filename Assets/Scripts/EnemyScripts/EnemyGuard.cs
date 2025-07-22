using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyGuard : MonoBehaviour
{
    [SerializeField] private LayerMask playerAttackLayerMask;
    [SerializeField] private float guardChance = 0.9f;
    [SerializeField] private float knockBackPower;
    [SerializeField] private float knockBackTime;
    [SerializeField] private float guardReleaseDelay;

    private Enemy self;
    private bool isGuarding = false;
    private Coroutine guardDelayCoroutine;

    // --- 넉백 관련 변수 추가 ---
    private bool needsKnockback = false; // 넉백을 적용해야 할지 여부
    private Vector3 currentKnockbackDirection; // 적용할 넉백 방향

    private void Start()
    {
        self = GetComponentInParent<Enemy>();
    }

    // --- FixedUpdate 추가 ---
    private void FixedUpdate()
    {
        // needsKnockback 플래그가 true일 때만 넉백 적용
        if (needsKnockback)
        {
            ApplyKnockbackPhysics(); // 물리 연산을 FixedUpdate에서 수행
            needsKnockback = false; // 넉백 적용 후 플래그 초기화

            // NavMeshAgent 제어는 여기서도 가능하지만, KnockbackRoutine 내부에서 하는 것이 더 깔끔할 수 있습니다.
            // 여기서는 물리 적용만 전담하도록 합니다.
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 공격이 아닐 경우 리턴
        if (!((1 << other.gameObject.layer) == playerAttackLayerMask))
        {
            return;
        }

        // 이미 가드 중일 경우: 넉백 플래그만 설정하고 타이머 리셋
        if (isGuarding)
        {
            // 넉백 방향 설정 (필요에 따라 인자 받도록 변경 가능)
            currentKnockbackDirection = -self.transform.forward; // 현재 적의 뒤 방향으로 넉백

            needsKnockback = true; // FixedUpdate에서 넉백 적용되도록 플래그 설정

            // 가드 중 재공격 시 가드 유지 타이머 리셋
            if (guardDelayCoroutine != null)
            {
                StopCoroutine(guardDelayCoroutine);
            }
            guardDelayCoroutine = StartCoroutine(GuardDelay());
            return;
        }

        // 가드 중이 아닐 때만 확률 체크하여 가드 시도
        float randomValue = Random.value;
        if (randomValue <= guardChance)
        {
            StartGuard();
        }
    }

    private void StartGuard()
    {
        if (self == null || self.anim == null || self.rb == null || self.NavAgent == null)
        {
            Debug.LogError("EnemyGuard: Missing references for Guard action (self, anim, rb, or NavAgent).");
            return;
        }

        isGuarding = true;
        self.anim.SetTrigger("Guard"); // 가드 애니메이션 트리거 설정

        // 넉백 플래그 설정
        currentKnockbackDirection = -self.transform.forward; // 넉백 방향 설정
        needsKnockback = true; // FixedUpdate에서 넉백 적용되도록 플래그 설정

        // NavMeshAgent 움직임 멈춤 (넉백 적용 전, 또는 넉백 루틴 시작 시 멈춤)
        if (self.NavAgent.isOnNavMesh)
        {
            self.NavAgent.isStopped = true;
        }

        if (guardDelayCoroutine != null)
        {
            StopCoroutine(guardDelayCoroutine);
        }
        guardDelayCoroutine = StartCoroutine(GuardDelay());
        Debug.Log("Enemy initiated Guard.");
    }

    // 넉백 '물리 연산'만 전담하는 메서드 (FixedUpdate에서 호출)
    private void ApplyKnockbackPhysics()
    {
        if (self == null || self.rb == null) return;

        // 기존 넉백 코루틴이 있다면 중지하여 새 넉백이 적용되도록 합니다.
        if (self.knockbackCoroutine != null)
        {
            self.StopCoroutine(self.knockbackCoroutine);
        }

        self.rb.AddForce(currentKnockbackDirection * knockBackPower, ForceMode.Impulse);

        // 넉백 시간 동안 NavMeshAgent를 비활성화하는 코루틴 시작
        self.knockbackCoroutine = self.StartCoroutine(self.KnockbackRoutine(knockBackTime));
    }

    // GuardDuration을 대체하는 guardReleaseDelay 시간 동안 가드를 유지
    IEnumerator GuardDelay()
    {
        yield return new WaitForSeconds(guardReleaseDelay);

        if (isGuarding)
        {
            GuardRelease();
        }
    }

    void GuardRelease()
    {
        isGuarding = false;
        if (self != null && self.anim != null)
        {
            self.anim.SetBool("Guard", false);
        }

        if (self != null && self.NavAgent != null && self.NavAgent.isOnNavMesh)
        {
            self.NavAgent.isStopped = false;
        }
        Debug.Log("Enemy Guard Released.");
    }
}