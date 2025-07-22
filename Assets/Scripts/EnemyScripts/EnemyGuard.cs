using System.Collections;
using Unity.VisualScripting; // 이 using은 필요 없을 수 있습니다.
using UnityEngine;
using UnityEngine.AI; // NavMeshAgent를 사용하기 위해 추가

public class EnemyGuard : MonoBehaviour
{
    [SerializeField] private LayerMask playerAttackLayerMask;
    [SerializeField] private float guardChance = 0.9f;
    [SerializeField] private float knockBackPower;
    [SerializeField] private float knockBackTime; // 넉백이 적용될 시간
    [SerializeField] private float guardReleaseDelay;// 추가 공격이 없을 경우 가드를 유지하는 시간

    private Enemy self; // Enemy 스크립트의 인스턴스
    private bool isGuarding = false; // 현재 가드 중인지 여부
    private Coroutine guardDelayCoroutine; // GuardDelay 코루틴 참조용

    private void Start()
    {
        self = GetComponentInParent<Enemy>(); // 부모에서 Enemy 스크립트 찾기
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이미 가드 중인데 플레이어 공격 레이어 마스크에 해당하면, 넉백만 다시 적용
        if (isGuarding && ((1 << other.gameObject.layer) == playerAttackLayerMask))
        {
            ApplyKnockback();
            // 가드 중 재공격 시 가드 유지 타이머 리셋
            if (guardDelayCoroutine != null)
            {
                StopCoroutine(guardDelayCoroutine);
            }
            guardDelayCoroutine = StartCoroutine(GuardDelay());
            return; // 이미 가드 중이면 더 이상 확률 체크나 SetTrigger("Guard")를 할 필요 없음
        }

        // 가드 중이 아닐 때만 확률 체크하여 가드 시도
        if ((1 << other.gameObject.layer) == playerAttackLayerMask)
        {
            float randomValue = Random.value; // 0.0 ~ 1.0 사이의 랜덤 값
            if (randomValue <= guardChance) // 90% (0.9)보다 작거나 같으면 가드
            {
                StartGuard();
            }
            else
            {
                // 가드에 실패한 경우 (피격 로직 등으로 연결될 수 있음)
                Debug.Log("Enemy failed to guard the attack!");
                // 여기서 피격 애니메이션, 데미지 처리 등의 로직을 추가할 수 있습니다.
            }
        }
    }

    // Guard를 시작하는 메서드
    private void StartGuard()
    {
        if (self == null || self.anim == null || self.rb == null || self.NavAgent == null)
        {
            Debug.LogError("EnemyGuard: Missing references for Guard action (self, anim, rb, or NavAgent).");
            return;
        }

        isGuarding = true;
        self.anim.SetTrigger("Guard"); // 가드 애니메이션 트리거 설정
        ApplyKnockback(); // 넉백 적용

        // NavMeshAgent 움직임 멈춤
        if (self.NavAgent.isOnNavMesh) // NavMesh 위에 있을 때만 제어
        {
            self.NavAgent.isStopped = true; // NavMeshAgent 멈춤
            // self.NavAgent.ResetPath(); // 경로를 완전히 지워도 됨 (선택 사항)
            // self.NavAgent.enabled = false; // NavMeshAgent 자체를 비활성화하는 것도 방법 (넉백 동안)
        }

        // 기존 코루틴이 있다면 중지하고 새로운 가드 지연 코루틴 시작
        if (guardDelayCoroutine != null)
        {
            StopCoroutine(guardDelayCoroutine);
        }
        guardDelayCoroutine = StartCoroutine(GuardDelay());
        Debug.Log("Enemy initiated Guard.");
    }

    // 넉백을 적용하는 내부 메서드
    private void ApplyKnockback()
    {
        if (self == null || self.rb == null) return;

        // 기존 넉백 코루틴이 있다면 중지하여 새 넉백이 적용되도록 합니다.
        if (self.knockbackCoroutine != null) // Enemy 스크립트에 knockbackCoroutine이 있다고 가정
        {
            self.StopCoroutine(self.knockbackCoroutine);
        }

        // 넉백 방향: 적의 현재 앞 방향의 반대 (뒤)
        Vector3 knockbackDirection = -self.transform.forward;

        // Rigidbody를 통해 넉백 적용 (ForceMode.Impulse는 순간적인 힘)
        self.rb.AddForce(knockbackDirection * knockBackPower, ForceMode.Impulse);

        // 넉백 시간 동안 NavMeshAgent를 비활성화하는 코루틴 시작
        self.knockbackCoroutine = self.StartCoroutine(self.KnockbackRoutine(knockBackTime));
    }

    // GuardDuration을 대체하는 guardReleaseDelay 시간 동안 가드를 유지
    // OnTriggerExit 로직을 제거하고 이 코루틴 내부에서 가드 해제를 관리합니다.
    IEnumerator GuardDelay()
    {
        // Debug.Log($"Guard will be released in {guardReleaseDelay:F2} seconds if no new attack.");
        yield return new WaitForSeconds(guardReleaseDelay); // guardReleaseDelay 만큼 대기

        // 대기 시간 후 isGuarding이 여전히 true이면 가드 해제
        // 이 조건은 새로운 공격이 들어와 코루틴이 재시작되면 false가 되지 않도록 함.
        if (isGuarding) // 재확인 (혹시 모를 Race Condition 방지)
        {
            GuardRelease();
        }
    }

    // 가드를 해제하는 메서드
    void GuardRelease()
    {
        isGuarding = false;
        if (self != null && self.anim != null)
        {
            self.anim.SetBool("Guard", false); // "Guard" 불리언 파라미터를 사용하여 가드 애니메이션 해제
        }

        // NavMeshAgent 다시 활성화 (이동 재개)
        if (self != null && self.NavAgent != null && self.NavAgent.isOnNavMesh)
        {
            self.NavAgent.isStopped = false; // 멈췄던 NavMeshAgent 다시 움직이도록
            // self.NavAgent.enabled = true; // 만약 넉백 중 비활성화했다면 다시 활성화
            // AI Behavior Tree의 다음 Action이 자동으로 SetDestination을 호출하여 이동을 재개할 것임.
        }
        Debug.Log("Enemy Guard Released.");
    }

}