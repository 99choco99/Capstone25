using UnityEngine;

public class EnemySense : MonoBehaviour
{
    private float currentSightRange;
    [SerializeField] private float normalSightRange = 10f;
    [SerializeField] private float detectSightRange = 20f;
    [SerializeField][Range(0, 360)] private float sightAngle = 90f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask obstacleLayer;

    public Transform Target { get; private set; }
    public bool IsTargetDetected { get; private set; }


    private void Update()
    {
        DetectPlayer();
    }

    //플레이어 발견 로직
    public void DetectPlayer()
    {
        Collider[] hits = new Collider[1];
        if (Physics.OverlapSphereNonAlloc(transform.position, currentSightRange, hits, targetLayer) > 0)
        {
            Transform playerTransform = hits[0].transform;
            Vector3 directionToTarget = (playerTransform.position - transform.position).normalized;

            //장애물에 숨어있을 때
            if (Physics.Raycast(transform.position, directionToTarget, currentSightRange, obstacleLayer))
            {
                SetDetectState(false, null);
                return;
            }
            if (Vector3.Dot(directionToTarget, transform.forward) > Mathf.Cos(sightAngle * 0.5f * Mathf.Deg2Rad) || IsTargetDetected)
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
    }
}
