using System.Collections.Generic;
using UnityEngine;

/// <summary>한 공격 판정창에서 대상별 중복 접촉을 막고 유효한 피해 요청을 전달합니다.</summary>
public sealed class AttackHitResolver
{
    private readonly HashSet<IDamageable> hitTargets = new();

    public bool HasProcessedTarget(IDamageable target) => hitTargets.Contains(target);

    public bool TryResolve(
        IDamageable target,
        Collider targetCollider,
        in DamageRequest request,
        bool defenseOnly,
        out DamageResult result)
    {
        result = default;

        if (defenseOnly && hitTargets.Contains(target))
            return false;

        if (defenseOnly
            && ((!request.CanGuard && !request.CanDeflect)
                || !targetCollider.TryGetComponent<IDefenser>(out var defenser)
                || defenser.DecideDefense(request) == DefenseType.None))
            return false;
        if (!hitTargets.Add(target))
            return false;

        result = target.ReceiveDamage(request);
        // 방어용 선행 접촉이 거부되면 이후 실제 무기 접촉은 다시 판정할 수 있습니다.
        if (defenseOnly && !result.IsAccepted)
            hitTargets.Remove(target);

        return result.IsAccepted;
    }

    public void Clear()
    {
        hitTargets.Clear();
    }
}
