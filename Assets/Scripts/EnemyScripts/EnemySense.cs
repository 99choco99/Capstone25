using UnityEngine;

public class EnemySense : MonoBehaviour
{
    private float currentSightRange;
    [SerializeField] private float normalSightRange = 10f;
    [SerializeField] private float detectSightRange = 20f;
    [SerializeField][Range(0, 360)] private float sightAngle = 90f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask obstacleLayer;


    private Animator playerAnimator;
    private PlayerStats playerStats;


    public Transform Target { get; private set; }
    public bool IsTargetDetected { get; private set; }
    public float DistanceToTarget { get; private set; } // 타겟과의 거리
    public bool IsPlayerAttacking { get; private set; } // 플레이어가 공격 중인가?
    public bool IsPlayerVulnerable { get; private set; } // 플레이어가 무방비 상태인가?


    private void Start()
    {
        currentSightRange = normalSightRange;
    }

    private void Update()
    {
        DetectPlayer();
        if(IsTargetDetected)
        {
            AnalyzeTarget();
        }

    }

    //플레이어 발견 로직
    public void DetectPlayer()
    {
        Collider[] hits = new Collider[1];
        if (Physics.OverlapSphereNonAlloc(transform.position, currentSightRange, hits, targetLayer) > 0)
        {
            Transform playerTransform = hits[0].transform;
            Vector3 directionToTarget = (playerTransform.position - transform.position).normalized;
            directionToTarget.y = 0;

            if (IsTargetDetected) { return; }
            //장애물에 숨어있을 때
            if (Physics.Raycast(transform.position, directionToTarget, currentSightRange, obstacleLayer))
            {
                SetDetectState(false, null);
                return;
            }

            if (Vector3.Dot(directionToTarget, transform.forward) > Mathf.Cos(sightAngle * 0.5f * Mathf.Deg2Rad))
            {
                SetDetectState(true, playerTransform);
            }
            else
            {
                SetDetectState(false, null);
            }
        }
        else
        {
            SetDetectState(false, null);
        }

    }

    public void SetDetectState(bool detected, Transform target)
    {
        IsTargetDetected = detected;
        Target = target;
        currentSightRange = detected ? detectSightRange : normalSightRange;


        if(detected && target != null)
        {
            if (playerAnimator == null || playerStats == null)
            {
                playerAnimator = Target.GetComponentInParent<Animator>();
                playerStats = Target.GetComponentInParent<PlayerStats>();
            }
        }
        else
        {
            playerAnimator = null;
            playerStats = null;
        }
    }

    private void AnalyzeTarget()
    {
        if (Target == null || playerAnimator == null)
        {
            IsPlayerAttacking = false;
            IsPlayerVulnerable = false;
            return;
        }

        DistanceToTarget = Vector3.Distance(Target.position, transform.position);

        AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);

        IsPlayerAttacking = stateInfo.IsTag("Attack");
        IsPlayerVulnerable = stateInfo.IsTag("Vulnerable");
    }


    public bool IsTargetInAttackRange(float range)
    {
        if (!IsTargetDetected) return false;
        return DistanceToTarget <= range;
    }

    public bool IsPlayerPostureHigh()
    {
        if (playerStats == null) return false;
        return playerStats.currentPosture >= playerStats.maxPosture * 0.7f;
    }

}
