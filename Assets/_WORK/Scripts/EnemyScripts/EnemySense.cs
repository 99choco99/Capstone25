using System.Collections;
using UnityEngine;

public class EnemySense : MonoBehaviour
{
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



    private void Update() {
        DetectTarget();
        if (IsTargetDetected)
        {
            DistanceToTarget = Vector3.Distance(CurrentTarget.position, transform.position);
            loseTargetTimer -= Time.deltaTime;
            if (loseTargetTimer <= 0)
            {
                SetDetectState(null);
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
                    SetDetectState(potentialTarget.transform);
                    loseTargetTimer = loseTargetTime;
                    return;
                }
            }
        }

    }

    public void SetDetectState(Transform target)
    {
        CurrentTarget = target;

        if (target != null)
        {
            loseTargetTimer = loseTargetTime;
            DistanceToTarget = Vector3.Distance(target.position, transform.position);
        }
    }
}