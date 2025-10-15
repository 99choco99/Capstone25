using System.Collections;
using UnityEngine;

public class EnemySense : MonoBehaviour
{
    private Enemy enemy;

    [Header("감지 설정 (Sensing Settings)")]
    [SerializeField] private Transform eyeTransform;
    [SerializeField] private float detectionRadius = 15f; // 플레이어를 감지할 수 있는 최대 반경
    [SerializeField, Range(0, 360)] private float detectionAngle = 90f; // AI의 시야각
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float senseInterval = 0.1f; // 감지 주기 (성능 최적화)

    [Header("위협 분석 (Threat Analysis)")]
    [SerializeField] private float threatDistance = 4f;
    [SerializeField, Range(0, 1)] private float threatAngleThreshold = 0.5f;

    [Header("타겟 상실 (Target Lost)")]
    [SerializeField] private float loseTargetTime = 5f;
    private float loseTargetTimer;


    public Transform Target { get; private set; }
    public bool IsTargetDetected { get; private set; }
    public float DistanceToTarget { get; private set; }
    public bool IsPlayerAttacking { get; private set; }
    public bool IsPlayerVulnerable { get; private set; }
    public bool IsPlayerAttackThreatening { get; private set; }

    private Animator playerAnimator;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        // 성능 최적화를 위해 Update 대신 Coroutine 사용
        StartCoroutine(SenseRoutine());
    }

    private IEnumerator SenseRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(senseInterval);
        while (true)
        {
            DetectTarget();
            if (IsTargetDetected)
            {
                AnalyzeTarget();
            }
            yield return wait;
        }
    }

    private void DetectTarget()
    {
        Collider[] hits = new Collider[1];
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, hits, playerLayer);

        if (hitCount > 0)
        {
            Transform potentialTarget = hits[0].transform;
            Vector3 directionToTarget = potentialTarget.position - eyeTransform.position;

            if (Vector3.Angle(transform.forward, directionToTarget) < detectionAngle / 2f)
            {
                if (!Physics.Linecast(eyeTransform.position, potentialTarget.position + Vector3.up * 1f, obstacleLayer))
                {
                    SetDetectState(true, potentialTarget);
                    loseTargetTimer = loseTargetTime;
                    return;
                }
            }
        }

        if (IsTargetDetected)
        {
            loseTargetTimer -= senseInterval;
            if (loseTargetTimer <= 0)
            {
                SetDetectState(false, null);
            }
        }
    }

    private void AnalyzeTarget()
    {
        if (Target == null)
        {
            // 타겟이 없다면 모든 위협 정보를 초기화
            IsPlayerAttacking = false;
            IsPlayerAttackThreatening = false;
            return;
        }

        DistanceToTarget = Vector3.Distance(Target.position, transform.position);

        if (playerAnimator == null) return;
        AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);

        IsPlayerAttacking = stateInfo.IsTag("Attack");
        IsPlayerAttackThreatening = false;

        if (IsPlayerAttacking)
        {
            if (DistanceToTarget <= threatDistance)
            {
                Vector3 directionToEnemy = (transform.position - Target.position).normalized;
                if (Vector3.Dot(Target.forward, directionToEnemy) > threatAngleThreshold)
                {
                    IsPlayerAttackThreatening = true;
                }
            }
        }
    }

    private void SetDetectState(bool detected, Transform target)
    {
        IsTargetDetected = detected;
        Target = target;

        if (detected)
        {
            SoundManager.Instance.StopLoopingSFX("BGM_Main");
            SoundManager.Instance.PlayLoopingSFX("BGM_Combat");
            if (playerAnimator == null && target != null)
            {
                playerAnimator = target.GetComponentInParent<Animator>();
            }
        }
        else
        {
            // 타겟을 잃으면 참조도 초기화
            playerAnimator = null;
            SoundManager.Instance.PlayLoopingSFX("BGM_Main");
            SoundManager.Instance.StopLoopingSFX("BGM_Combat");
        }
    }

    // 비헤이비어 트리의 조건 노드가 사용할 유틸리티 함수
    public bool IsTargetInAttackRange(float range)
    {
        return IsTargetDetected && DistanceToTarget <= range;
    }
}