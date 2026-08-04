using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField] private float hitCheckRadius = 0.15f; // 검사할 반경
    [SerializeField] private Transform[] hitPoints;    // 검사 중심점

    private IWeaponOwner owner;
    private bool isAttackActive = false;
    public bool IsAttackActive => isAttackActive;

    private Vector3[] previousPoints;
    private bool hasValidHitPoints;

    private RaycastHit[] hitResults = new RaycastHit[32];
    private Collider[] overlapResults = new Collider[32];

    private void Awake()
    {
        owner = GetComponentInParent<IWeaponOwner>();
        if(hitPoints == null || hitPoints.Length == 0)
        {
            Debug.LogError("Weapon: 무기에 hitPoint가 설정되지 않아 공격 판정을 비활성화합니다.", this);
            return;
        }

        for (int i = 0; i < hitPoints.Length; i++)
        {
            if (hitPoints[i] != null)
                continue;

            Debug.LogError($"Weapon: hitPoints[{i}]가 비어 있어 공격 판정을 비활성화합니다.", this);
            return;
        }

        previousPoints = new Vector3[hitPoints.Length];
        hasValidHitPoints = true;
    }
    /// <summary>
    /// 플레이어·적 State가 이번 프레임의 공격 취소와 히트윈도 갱신을 먼저 처리한 뒤
    /// 무기 충돌을 검사해 이미 닫힌 공격이 한 프레임 더 맞히는 현상을 막습니다.
    /// </summary>
    private void LateUpdate()
    {
        if (owner == null || !isAttackActive || !hasValidHitPoints)
        {
            return;
        }
        PerformHitCheck();
    }
    public void EnableWeaponCollider()
    {
        if (!hasValidHitPoints)
        {
            isAttackActive = false;
            return;
        }

        isAttackActive = true;

        for (int i = 0; i < hitPoints.Length; i++)
        {
            previousPoints[i] = hitPoints[i].position;
        }
    }

    public void DisableWeaponCollider()
    {
        isAttackActive = false;
    }

    /// <summary>
    /// 두 무기의 hitPoints를 칼날을 이루는 선분으로 보고 가장 가까운 두 점의 중점을 구합니다.
    /// 가드·패링 이펙트를 피격자의 몸이 아니라 실제로 맞부딪힌 두 칼 사이에 배치할 때 사용합니다.
    /// </summary>
    public bool TryGetClosestPointTo(Weapon other, out Vector3 contactPoint, out float sqrDistance)
    {
        contactPoint = default;
        sqrDistance = float.PositiveInfinity;

        if (other == null
            || hitPoints == null
            || other.hitPoints == null
            || hitPoints.Length == 0
            || other.hitPoints.Length == 0)
        {
            return false;
        }

        bool found = false;
        int thisSegmentCount = Mathf.Max(1, hitPoints.Length - 1);
        int otherSegmentCount = Mathf.Max(1, other.hitPoints.Length - 1);

        for (int i = 0; i < thisSegmentCount; i++)
        {
            if (!TryGetSegment(hitPoints, i, out Vector3 a0, out Vector3 a1))
                continue;

            for (int j = 0; j < otherSegmentCount; j++)
            {
                if (!TryGetSegment(other.hitPoints, j, out Vector3 b0, out Vector3 b1))
                    continue;

                ClosestPointsOnSegments(a0, a1, b0, b1, out Vector3 pointA, out Vector3 pointB);
                float candidateDistance = (pointA - pointB).sqrMagnitude;
                if (candidateDistance >= sqrDistance)
                    continue;

                sqrDistance = candidateDistance;
                contactPoint = Vector3.Lerp(pointA, pointB, 0.5f);
                found = true;
            }
        }

        return found;
    }

    private static bool TryGetSegment(Transform[] points, int index, out Vector3 start, out Vector3 end)
    {
        start = default;
        end = default;

        if (points.Length == 1)
        {
            if (points[0] == null) return false;
            start = end = points[0].position;
            return true;
        }

        if (index < 0
            || index + 1 >= points.Length
            || points[index] == null
            || points[index + 1] == null)
        {
            return false;
        }

        start = points[index].position;
        end = points[index + 1].position;
        return true;
    }

    /// <summary>
    /// 선분-선분 최근접점 공식을 사용합니다.
    /// 길이가 0인 선분도 처리하므로 hitPoint가 하나뿐인 임시 무기에서도 안전합니다.
    /// </summary>
    private static void ClosestPointsOnSegments(
        Vector3 p1,
        Vector3 q1,
        Vector3 p2,
        Vector3 q2,
        out Vector3 point1,
        out Vector3 point2)
    {
        const float epsilon = 0.000001f;

        Vector3 d1 = q1 - p1;
        Vector3 d2 = q2 - p2;
        Vector3 r = p1 - p2;
        float a = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);
        float s;
        float t;

        if (a <= epsilon && e <= epsilon)
        {
            point1 = p1;
            point2 = p2;
            return;
        }

        if (a <= epsilon)
        {
            s = 0f;
            t = Mathf.Clamp01(f / e);
        }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= epsilon)
            {
                t = 0f;
                s = Mathf.Clamp01(-c / a);
            }
            else
            {
                float b = Vector3.Dot(d1, d2);
                float denominator = a * e - b * b;
                s = denominator > epsilon
                    ? Mathf.Clamp01((b * f - c * e) / denominator)
                    : 0f;

                t = (b * s + f) / e;
                if (t < 0f)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else if (t > 1f)
                {
                    t = 1f;
                    s = Mathf.Clamp01((b - c) / a);
                }
            }
        }

        point1 = p1 + d1 * s;
        point2 = p2 + d2 * t;
    }

    public void PerformHitCheck()
    {
        if (!hasValidHitPoints)
            return;

        for (int i = 0; i < hitPoints.Length; i++)
        {
            Vector3 currentPoint = hitPoints[i].position;
            Vector3 previousPoint = previousPoints[i];

            Vector3 direction = currentPoint - previousPoint;
            float distance = direction.magnitude;

            if(distance > 0.001f)
            {
                // SphereCast는 시작점이 이미 대상 콜라이더 안에 있으면 놓칠 수 있습니다.
                // 짧은 타격 구간의 첫 프레임도 보존하기 위해 이동 시작점을 함께 검사합니다.
                ProcessOverlapsAt(previousPoint);

                int hitCount = 
                    Physics.SphereCastNonAlloc
                    (previousPoint, hitCheckRadius, direction.normalized, hitResults, distance, targetLayerMask);
                for (int h = 0; h < hitCount; h++)
                {
                    RaycastHit hit = hitResults[h];
                    ProcessHit(hit.collider, hit.point);
                }

                // 프레임 사이에 칼날이 대상 안으로 들어간 채 끝난 경우를 보완합니다.
                ProcessOverlapsAt(currentPoint);
            }
            else
            {
                ProcessOverlapsAt(currentPoint);
            }
            previousPoints[i] = currentPoint;
        }

    }

    /// <summary>
    /// 칼날의 한 지점이 이미 대상 콜라이더와 겹쳐 있는 경우를 검사합니다.
    /// 이동 경로를 검사하는 SphereCast의 시작·끝 겹침 누락을 보완하는 용도입니다.
    /// 한 공격에서 같은 대상에게 여러 번 피해가 들어가는 것은 공격 소유자의 hitTargets가 막습니다.
    /// </summary>
    private void ProcessOverlapsAt(Vector3 point)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            point,
            hitCheckRadius,
            overlapResults,
            targetLayerMask);

        for (int h = 0; h < hitCount; h++)
        {
            Collider hitCollider = overlapResults[h];
            Vector3 hitPoint = hitCollider != null
                ? hitCollider.ClosestPoint(point)
                : point;
            ProcessHit(hitCollider, hitPoint);
        }
    }


    private void ProcessHit(Collider hitCollider, Vector3 hitPoint)
    {
        if (hitCollider != null && hitCollider.TryGetComponent<IDamageable>(out var target))
        {
            if (owner.OwnerFaction != target.TargetFaction)
            {
                // SphereCast가 대상 내부에서 시작하면 hit.point가 (0, 0, 0)일 수 있으므로
                // 해당 프레임의 무기 위치와 가장 가까운 표면점으로 한 번 보정합니다.
                if (hitPoint == Vector3.zero)
                    hitPoint = hitCollider.ClosestPoint(transform.position);

                owner.OnWeaponHit(target, hitCollider, this, hitPoint);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (hitPoints == null) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < hitPoints.Length; i++)
        {
            if (hitPoints[i] != null)
            {
                Gizmos.DrawWireSphere(hitPoints[i].position, hitCheckRadius);

                if (Application.isPlaying && isAttackActive && previousPoints != null && previousPoints.Length > i)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(previousPoints[i], hitPoints[i].position);
                    Gizmos.color = Color.red;
                }
            }
        }
    }
}
