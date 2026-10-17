using UnityEngine;

[DefaultExecutionOrder(-300)]
public class Weapon : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField] private float hitCheckRadius = 0.15f; // 검사할 반경
    [SerializeField] private Transform[] hitPoints;    // 검사 중심점
    [SerializeField, Min(0)] private int hitGroup;
    [SerializeField] private TrailRenderer attackTrailPrefab;

    [SerializeField] private WeaponSlot weaponSlot;

    public WeaponSlot Slot => weaponSlot;
    public int HitGroup => hitGroup;
    public float HitCheckRadius => hitCheckRadius;
    public System.Collections.Generic.IReadOnlyList<Transform> HitPoints => hitPoints;
    public Vector3 HitMotion { get; private set; }
    public Vector3 HitEffectPoint { get; private set; }

    private IWeaponOwner owner;
    private Transform ownerTransform;
    private bool isAttackActive = false;
    private TrailRenderer attackTrail;

    private Vector3[] previousPoints;
    private bool hasValidHitPoints;

    private RaycastHit[] hitResults = new RaycastHit[32];
    private Collider[] overlapResults = new Collider[32];

    private void Awake()
    {
        owner = GetComponentInParent<IWeaponOwner>();
        ownerTransform = (owner as Component)?.transform;
        if (hitPoints == null || hitPoints.Length == 0)
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

        if (attackTrailPrefab != null)
        {
            attackTrail = Instantiate(attackTrailPrefab, transform, false);
            attackTrail.transform.position = (hitPoints[0].position + hitPoints[hitPoints.Length - 1].position) * 0.5f;
            attackTrail.emitting = false;
        }
    }

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

        if (attackTrail != null)
        {
            attackTrail.Clear();
            attackTrail.AddPosition(attackTrail.transform.position);
            attackTrail.emitting = true;
        }

        for (int i = 0; i < hitPoints.Length; i++)
        {
            previousPoints[i] = hitPoints[i].position;
        }
    }

    public void DisableWeaponCollider()
    {
        isAttackActive = false;
        if (attackTrail != null)
            attackTrail.emitting = false;
    }

    private void OnDisable()
    {
        DisableWeaponCollider();
        if (attackTrail != null)
            attackTrail.Clear();
    }

    public void PerformHitCheck()
    {
        if (!hasValidHitPoints)
            return;

        if (owner.IsDefensibleAttack)
        {
            for (int i = 0; i < hitPoints.Length; i++)
            {
                ProcessGuardContacts(previousPoints[i], hitPoints[i].position);
                if (!isAttackActive)
                    return;
            }
        }

        for (int i = 0; i < hitPoints.Length; i++)
        {
            Vector3 currentPoint = hitPoints[i].position;
            Vector3 previousPoint = previousPoints[i];

            Vector3 direction = currentPoint - previousPoint;
            float distance = direction.magnitude;

            if (distance > 0.001f)
            {
                ProcessOverlapsAt(previousPoint, direction);

                int hitCount = Physics.SphereCastNonAlloc(
                    previousPoint,
                    hitCheckRadius,
                    direction.normalized,
                    hitResults,
                    distance,
                    targetLayerMask);
                for (int h = 0; h < hitCount; h++)
                {
                    RaycastHit hit = hitResults[h];
                    ProcessHit(hit.collider, hit.point, direction);
                }
            }

            // 프레임 사이에 칼날이 대상 안으로 들어간 채 끝난 경우를 보완합니다.
            ProcessOverlapsAt(currentPoint, direction);
            previousPoints[i] = currentPoint;
            if (!isAttackActive)
                return;
        }
    }

    private void ProcessGuardContacts(Vector3 start, Vector3 end)
    {
        float reach = CombatSettings.Current.GuardContactReach;
        if (reach <= 0f)
            return;

        int count = Physics.OverlapCapsuleNonAlloc(
            start,
            end,
            hitCheckRadius + reach,
            overlapResults,
            targetLayerMask,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider body = overlapResults[i];
            if (!body.TryGetComponent<IDamageable>(out var target) || owner.OwnerFaction == target.TargetFaction)
                continue;
            if (owner.HasProcessedTarget(target))
                continue;
            if (!GuardContactPolicy.TryGetContact(
                body,
                body.transform.forward,
                start,
                end,
                hitCheckRadius,
                reach,
                out Vector3 point))
                continue;

            int obstacleMask = Physics.DefaultRaycastLayers & ~targetLayerMask.value & ~(1 << ownerTransform.gameObject.layer);
            Vector3 origin = ownerTransform.position;
            origin.y = point.y;
            if (Physics.Linecast(origin, point, obstacleMask, QueryTriggerInteraction.Ignore))
                continue;

            owner.OnWeaponHit(target, body, this, point, defenseOnly: true);
            if (!isAttackActive)
                return;
        }
    }

    /// <summary>
    /// 칼날의 한 지점이 이미 대상 콜라이더와 겹쳐 있는 경우를 검사합니다.
    /// 이동 경로를 검사하는 SphereCast의 시작·끝 겹침 누락을 보완하는 용도입니다.
    /// 한 공격에서 같은 대상에게 여러 번 피해가 들어가는 것은 공격 소유자의 hitTargets가 막습니다.
    /// </summary>
    private void ProcessOverlapsAt(Vector3 point, Vector3 motion)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(point, hitCheckRadius, overlapResults, targetLayerMask);

        for (int h = 0; h < hitCount; h++)
        {
            Collider hitCollider = overlapResults[h];
            Vector3 hitPoint = hitCollider != null ? hitCollider.ClosestPoint(point) : point;
            ProcessHit(hitCollider, hitPoint, motion);
        }
    }

    private void ProcessHit(Collider hitCollider, Vector3 hitPoint, Vector3 motion)
    {
        if (hitCollider != null && hitCollider.TryGetComponent<IDamageable>(out var target))
        {
            if (owner.OwnerFaction != target.TargetFaction)
            {
                // 이미 처리한 접촉은 연출 위치 계산과 피해 요청 구성을 생략합니다.
                if (owner.HasProcessedTarget(target))
                    return;

                // SphereCast가 대상 내부에서 시작하면 hit.point가 (0, 0, 0)일 수 있으므로
                // 해당 프레임의 무기 위치와 가장 가까운 표면점으로 한 번 보정합니다.
                if (hitPoint == Vector3.zero)
                    hitPoint = hitCollider.ClosestPoint(transform.position);

                HitMotion = motion;
                HitEffectPoint = GetHitEffectPoint(hitCollider, hitPoint, motion);
                owner.OnWeaponHit(target, hitCollider, this, hitPoint);
            }
        }
    }

    private Vector3 GetHitEffectPoint(Collider body, Vector3 hitPoint, Vector3 motion)
    {
        Vector3 direction = motion.sqrMagnitude > 0.000001f ? motion.normalized : ownerTransform.forward;
        float distance = body.bounds.size.magnitude;
        Ray ray = new(hitPoint - direction * distance, direction);
        return body.Raycast(ray, out RaycastHit hit, distance * 2f) ? hit.point : hitPoint;
    }

    public Vector3 GetClosestBladePoint(Vector3 point)
    {
        if (!hasValidHitPoints)
            return transform.position;

        Vector3 closestPoint = hitPoints[0].position;
        float closestDistance = (point - closestPoint).sqrMagnitude;

        for (int i = 1; i < hitPoints.Length; i++)
        {
            Vector3 candidate = hitPoints[i].position;
            float distance = (point - candidate).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closestPoint = candidate;
            closestDistance = distance;
        }

        return closestPoint;
    }

    public float GetForwardReach(Transform attacker, Vector3 direction)
    {
        if (!hasValidHitPoints)
            return 0f;

        float reach = 0f;
        for (int i = 0; i < hitPoints.Length; i++)
            reach = Mathf.Max(reach, Vector3.Dot(hitPoints[i].position - attacker.position, direction));
        return reach + hitCheckRadius;
    }

    private void OnDrawGizmosSelected()
    {
        if (hitPoints == null)
            return;

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

public static class GuardContactPolicy
{
    public static bool TryGetContact(
        Collider body,
        Vector3 forward,
        Vector3 start,
        Vector3 end,
        float weaponRadius,
        float reach,
        out Vector3 point)
    {
        Bounds bounds = body.bounds;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        Vector3 center = bounds.center + forward * reach;
        float halfHeight = Mathf.Max(0f, bounds.extents.y - radius);
        Vector3 bottom = center - Vector3.up * halfHeight;
        Vector3 top = center + Vector3.up * halfHeight;
        GetClosestPoints(start, end, bottom, top, out Vector3 bladePoint, out Vector3 guardPoint);

        Vector3 offset = bladePoint - guardPoint;
        float distance = offset.magnitude;
        point = guardPoint + (distance > 0.0001f ? offset / distance : forward) * radius;
        return distance <= radius + weaponRadius && Vector3.Dot(bladePoint - bounds.center, forward) >= 0f;
    }

    private static void GetClosestPoints(
        Vector3 start,
        Vector3 end,
        Vector3 bottom,
        Vector3 top,
        out Vector3 bladePoint,
        out Vector3 guardPoint)
    {
        Vector3 blade = end - start;
        Vector3 guard = top - bottom;
        Vector3 offset = start - bottom;
        float bladeLength = blade.sqrMagnitude;
        float guardLength = guard.sqrMagnitude;
        float bladeTime = 0f;
        float guardTime = 0f;

        if (bladeLength <= 0.0001f)
        {
            if (guardLength > 0.0001f)
                guardTime = Mathf.Clamp01(Vector3.Dot(guard, offset) / guardLength);
        }
        else
        {
            float bladeOffset = Vector3.Dot(blade, offset);
            if (guardLength <= 0.0001f)
                bladeTime = Mathf.Clamp01(-bladeOffset / bladeLength);
            else
            {
                float alignment = Vector3.Dot(blade, guard);
                float guardOffset = Vector3.Dot(guard, offset);
                float denominator = bladeLength * guardLength - alignment * alignment;
                if (denominator > 0f)
                    bladeTime = Mathf.Clamp01((alignment * guardOffset - bladeOffset * guardLength) / denominator);
                guardTime = (alignment * bladeTime + guardOffset) / guardLength;
                if (guardTime < 0f)
                {
                    guardTime = 0f;
                    bladeTime = Mathf.Clamp01(-bladeOffset / bladeLength);
                }
                else if (guardTime > 1f)
                {
                    guardTime = 1f;
                    bladeTime = Mathf.Clamp01((alignment - bladeOffset) / bladeLength);
                }
            }
        }

        bladePoint = start + blade * bladeTime;
        guardPoint = bottom + guard * guardTime;
    }
}
