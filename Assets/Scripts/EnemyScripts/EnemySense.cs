using System.Collections;
using UnityEngine;

public class EnemySense : MonoBehaviour
{
    private Enemy enemy;

    [Header("감지 설정")]
    [SerializeField] private Transform eyeTransform;
    [SerializeField] private float detectionRadius = 15f; // 플레이어를 감지할 수 있는 최대 반경
    [SerializeField, Range(0, 360)] private float detectionAngle = 90f; // AI의 시야각
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    
    [Header("타겟 상실 (Target Lost)")]
    [SerializeField] private float loseTargetTime = 4f;
    private float loseTargetTimer;


    public Transform CurrentTarget { get; private set; }
    public bool IsTargetDetected { get; private set; }
    public float DistanceToTarget { get; private set; }

    private Collider[] overlapResults = new Collider[5];

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Update() {
        if (enemy.Stats.IsDead) return;
        DetectTarget();
        if (IsTargetDetected)
        {
            DistanceToTarget = Vector3.Distance(CurrentTarget.position, transform.position);
            loseTargetTimer -= Time.deltaTime;
            if (loseTargetTimer <= 0)
            {
                SetDetectState(false, null);
            }
        }
    }


    private void DetectTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, overlapResults, playerLayer);

        if (hitCount > 0)
        {
            Collider potentialTarget = overlapResults[0];
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

    public void DetectWithAttack(Player player)
    {
        SetDetectState(true, player.transform);
        loseTargetTimer = loseTargetTime;
    }

    public void SetDetectState(bool detected, Transform target)
    {
        if (CurrentTarget == target && IsTargetDetected == detected)
        {
            return;
        }
        IsTargetDetected = detected;
        CurrentTarget = target;
    }

    // 비헤이비어 트리의 조건 노드가 사용할 유틸리티 함수
    public bool IsTargetInAttackRange(float range)
    {
        return IsTargetDetected && DistanceToTarget <= range;
    }
}