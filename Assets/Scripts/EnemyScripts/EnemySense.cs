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
    [SerializeField] private float attackThreatRange = 1f;
    private int lastPlayerAttackStateHash = 0;

    
    [Header("타겟 상실 (Target Lost)")]
    [SerializeField] private float loseTargetTime = 4f;
    private float loseTargetTimer;


    public Transform CurrentTarget { get; private set; }
    public bool IsTargetDetected { get; private set; }
    public float DistanceToTarget { get; private set; }
    public bool IsPlayerAttacking { get; private set; }
    public bool IsPlayerVulnerable { get; private set; }

    private Animator playerAnimator;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnDestroy()
    {
        if (GameManager.instance != null && enemy != null)
        {
            GameManager.instance.UnregisterEnemyInCombat(enemy);
        }
    }
    private void Update() { 
        DetectTarget();
        if (IsTargetDetected)
        {
            AnalyzeTarget();
            loseTargetTimer -= Time.deltaTime;
            if (loseTargetTimer <= 0)
            {
                SetDetectState(false, null);
            }
        }
    }


    private void DetectTarget()
    {
        Collider[] hits = new Collider[1];
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, hits, playerLayer);

        if (hitCount > 0)
        {
            Collider potentialTarget = hits[0];
            Vector3 directionToTarget = potentialTarget.transform.position - eyeTransform.position;

            if (Vector3.Angle(transform.forward, directionToTarget) < detectionAngle /2f)
            {
                if (!Physics.Linecast(eyeTransform.position, potentialTarget.bounds.center, obstacleLayer))
                {
                    SetDetectState(true, potentialTarget.transform);
                    loseTargetTimer = loseTargetTime;
                    return;
                }
            }
        }

    }

    private void AnalyzeTarget()

    {

        if (CurrentTarget == null)

        {

            // 타겟이 없다면 모든 위협 정보를 초기화

            IsPlayerAttacking = false;

            lastPlayerAttackStateHash = 0;

            return;

        }



        DistanceToTarget = Vector3.Distance(CurrentTarget.position, transform.position);

        if (Vector3.Dot(CurrentTarget.forward, transform.forward) > -0.8f) { return; }







        if (playerAnimator == null)

        {

            IsPlayerAttacking = false;

            lastPlayerAttackStateHash = 0;

            return;

        }

        AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
        int currentStateHash = stateInfo.fullPathHash;
        bool isPlayerInAttackAnim = stateInfo.IsTag("Attack");
        bool isAttackInThreatRange = DistanceToTarget <= attackThreatRange;





        bool isPlayerAttackingNow = isPlayerInAttackAnim && isAttackInThreatRange;



        if (enemy.AnimationManager.IsPerformAction)
        { 
            lastPlayerAttackStateHash = currentStateHash;
            return;

        }


        if (isPlayerAttackingNow && currentStateHash != lastPlayerAttackStateHash)
        {
            enemy.Combat.DecideDefenseAction();
        }

        lastPlayerAttackStateHash = currentStateHash;

    }

    public void DetectWithAttack(Player player)
    {
        SetDetectState(true, player.transform);
        loseTargetTimer = loseTargetTime;
    }

    public void SetDetectState(bool detected, Transform target)
    {
        if (CurrentTarget == target)
        {
            return;
        }
        Transform previousTarget = CurrentTarget;
        IsTargetDetected = detected;
        CurrentTarget = target;

        if (detected)
        {
            if(target == DataManager.Instance.Player.transform)
            {
                GameManager.instance.RegisterEnemyInCombat(enemy);
            }

            if (playerAnimator == null && target != null)
            {
                playerAnimator = target.GetComponentInParent<Animator>();
            }
        }
        else
        {
            // 타겟을 잃으면 참조도 초기화
            playerAnimator = null;
            if (previousTarget == DataManager.Instance.Player)
            {
                GameManager.instance.UnregisterEnemyInCombat(enemy);
            }
        }
    }

    // 비헤이비어 트리의 조건 노드가 사용할 유틸리티 함수
    public bool IsTargetInAttackRange(float range)
    {
        return IsTargetDetected && DistanceToTarget <= range;
    }
}