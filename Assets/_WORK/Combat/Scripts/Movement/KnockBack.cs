using UnityEngine;

/// <summary>
///넉백 이동에 필요한 최종 거리와 시간
/// </summary>
public readonly struct KnockbackSpec
{
    public float Distance { get; }
    public float Duration { get; }
    public AnimationCurve MovementCurve { get; }

    public KnockbackSpec(float distance, float duration, AnimationCurve movementCurve = null)
    {
        Distance = Mathf.Max(0f, distance);
        Duration = Mathf.Max(0f, duration);
        MovementCurve = movementCurve;
    }
}

/// <summary>
/// 넉백 관련 수치정의 및 계산
/// </summary>
public static class KnockBackPolicy
{
    /// <summary>
    /// 수비자의 최종 넉백량을 반환
    /// </summary>
    public static KnockbackSpec DefenderKnockBack(in DamageResult result)
    {
        if (!result.IsAccepted)
            return default;

        KnockbackSpec spec = GetDefenderSpec(result);
        return LimitContactSeparation(result, spec);
    }

    /// <summary>
    /// 공격자가 받을 최종 넉백량을 반환
    /// </summary>
    public static KnockbackSpec AttackerKnockBack(in DamageResult result)
    {
        if (!result.IsAccepted || !result.IsParryRebound)
            return default;

        return LimitContactSeparation(
            result,
            CombatSettings.Current.Knockback.Parried.GetSpec(result.Request.KnockBackLevel));
    }

    private static KnockbackSpec LimitContactSeparation(in DamageResult result, KnockbackSpec spec)
    {
        CombatSettings.KnockbackSettings settings = CombatSettings.Current.Knockback;
        AttackImpactProfile profile = result.Request.ImpactProfile;
        float profileLimit = profile != null ? profile.ContactSeparationDistanceLimit : -1f;
        if (result.DefenseType == DefenseType.Parry
            && profile != null
            && profile.ParryContactSeparationDistanceLimit >= 0f)
            profileLimit = profile.ParryContactSeparationDistanceLimit;
        float distanceLimit = profileLimit >= 0f
            ? profileLimit
            : result.Request.KnockBackLevel == KnockBackLevel.Heavy
            ? settings.HeavyContactSeparationDistanceLimit
            : settings.ContactSeparationDistanceLimit;
        if (distanceLimit <= 0f || !IsValidContactDistance(result.ContactDistance))
            return spec;
        KnockbackSpec defenderSpec = GetDefenderSpec(result);
        float separationDistance = defenderSpec.Duration > 0f ? defenderSpec.Distance : 0f;
        if (result.IsParryRebound)
        {
            KnockbackSpec attackerSpec = settings.Parried.GetSpec(result.Request.KnockBackLevel);
            if (attackerSpec.Duration > 0f)
                separationDistance += attackerSpec.Distance;
        }
        if (separationDistance <= distanceLimit)
            return spec;

        return new KnockbackSpec(spec.Distance * distanceLimit / separationDistance, spec.Duration, spec.MovementCurve);
    }

    private static KnockbackSpec GetDefenderSpec(in DamageResult result)
    {
        if (result.Request.KnockBackLevel == KnockBackLevel.None)
            return default;

        AttackImpactProfile profile = result.Request.ImpactProfile;
        return profile != null && profile.OverrideKnockback && profile.Knockback != null ? profile.Knockback.GetSpec(result.DefenseType)
            : CombatSettings.Current.Knockback.GetDefenderSpec(result.DefenseType, result.Request.KnockBackLevel);
    }

    public static bool CanApplyAttackPressure(in DamageResult result)
    {
        return result.IsAccepted && result.Request.KnockBackLevel != KnockBackLevel.None
            && (result.DefenseType != DefenseType.Parry || result.Request.DeflectResponse == DeflectResponse.Continue);
    }

    public static float GetAttackContactGap(in DamageResult result, float bodyWidth, float defenderRadius)
    {
        float contactGap = IsValidContactDistance(result.ContactDistance)
            ? result.ContactDistance : bodyWidth;
        Weapon weapon = result.Request.SourceWeapon;
        Transform attacker = result.Request.Attacker != null ? result.Request.Attacker.transform : null;
        float weaponGap = weapon != null
            && attacker != null
            ? weapon.GetForwardReach(attacker, result.HitDirection) + defenderRadius
            : 0f;
        return Mathf.Max(bodyWidth, Mathf.Max(contactGap, weaponGap));
    }

    public static float GetAttackPressureGap(in DamageResult result, float bodyWidth, float defenderRadius)
    {
        KnockbackSpec spec = DefenderKnockBack(result);
        float contactGap = IsValidContactDistance(result.ContactDistance)
            ? result.ContactDistance : bodyWidth;
        return Mathf.Max(GetAttackContactGap(result, bodyWidth, defenderRadius), contactGap + spec.Distance);
    }

    private static bool IsValidContactDistance(float contactDistance)
    {
        return contactDistance > 0f && !float.IsInfinity(contactDistance) && !float.IsNaN(contactDistance);
    }

    public static bool TryGetAttackPressureGeometry(
        Vector3 sourcePosition,
        Vector3 targetPosition,
        Vector3 contactDirection,
        out Vector3 direction,
        out float distance,
        out float lateralDistance)
    {
        direction = targetPosition - sourcePosition;
        direction.y = 0f;
        distance = direction.magnitude;
        lateralDistance = Vector3.ProjectOnPlane(direction, contactDirection).magnitude;
        return !(distance < 0.0001f || Vector3.Dot(direction, contactDirection) <= 0f);
    }

    public static Vector3 ClampAdvance(
        Vector3 deltaPosition,
        Vector3 direction,
        float currentDistance,
        float minimumDistance)
    {
        if (minimumDistance <= 0f)
            return deltaPosition;

        float forwardDistance = Vector3.Dot(deltaPosition, direction);
        float allowedForwardDistance = Mathf.Max(0f, currentDistance - minimumDistance);
        float excessForwardDistance = Mathf.Max(0f, forwardDistance - allowedForwardDistance);

        return deltaPosition - direction * excessForwardDistance;
    }
}

/// <summary>
/// 지정된 이동거리를 지정된 시간안에 이동하기 위한 이동량 계산
/// </summary>
public sealed class KnockbackMotion
{
    private Vector3 direction;
    private float totalDistance;
    private float duration;
    private float elapsed;
    private float previousProgress;
    private AnimationCurve movementCurve;
    private Transform followingSource;
    private float followGap;

    public bool IsActive { get; private set; }

    public bool IsFollowingAttack(Transform source) => source != null
        && IsActive
        && ReferenceEquals(
        followingSource,
        source);

    /// <summary>
    /// 방향으로 spec만큼 이동 준비
    /// </summary>
    public void Ready(Vector3 worldDirection, KnockbackSpec spec)
    {
        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude <= 0.0001f || spec.Duration <= 0f)
        {
            Stop();
            return;
        }

        direction = worldDirection.normalized;
        totalDistance = spec.Distance;
        duration = spec.Duration;
        elapsed = 0f;
        previousProgress = 0f;
        movementCurve = spec.MovementCurve ?? CombatSettings.Current.Knockback.MovementCurve;
        followingSource = null;
        followGap = 0f;
        IsActive = true;
    }

    public void FollowAttack(Transform source, float gap, Vector3 position)
    {
        if (!IsActive || source == null || float.IsInfinity(gap) || float.IsNaN(gap))
            return;

        followingSource = source;
        followGap = Mathf.Max(0f, gap);
        if (previousProgress == 0f)
            totalDistance = Mathf.Max(totalDistance, followGap - Vector3.Dot(position - source.position, direction));
    }

    public Vector3 GetAttackPressureAdvance(Transform source, Vector3 requestedDelta, Vector3 position)
    {
        if (!IsFollowingAttack(source))
            return Vector3.zero;

        requestedDelta.y = 0f;
        float forwardDistance = Mathf.Max(0f, Vector3.Dot(requestedDelta, direction));
        float currentGap = Vector3.Dot(position - source.position, direction);
        float currentGoalGap = followGap - totalDistance * (1f - previousProgress);
        return direction * Mathf.Clamp(currentGoalGap + forwardDistance - currentGap, 0f, forwardDistance);
    }

    public Vector3 ConstrainRootMotion(Vector3 deltaPosition)
    {
        if (!IsActive || totalDistance <= 0f || elapsed >= duration && followingSource == null)
            return deltaPosition;

        float opposingDistance = Vector3.Dot(deltaPosition, direction);
        return opposingDistance < 0f ? deltaPosition - direction * opposingDistance : deltaPosition;
    }

    public void StopFollowingAttack(Transform source)
    {
        if (!ReferenceEquals(followingSource, source))
            return;

        StopFollowingAttack();
    }

    public void StopFollowingAttack()
    {
        followingSource = null;
        followGap = 0f;
        IsActive = elapsed < duration;
    }

    /// <summary>
    /// 1 프레임에 적용할 이동량을 반환
    /// </summary>
    public Vector3 Start(float deltaTime)
    {
        if (!IsActive || deltaTime <= 0f)
            return Vector3.zero;

        elapsed = Mathf.Min(elapsed + deltaTime, duration);
        float normalizedTime = Mathf.Clamp01(elapsed / duration);
        float currentProgress = elapsed >= duration
            ? 1f
            : Mathf.Clamp(
            movementCurve.Evaluate(normalizedTime),
            previousProgress,
            1f);
        float frameProgress = currentProgress - previousProgress;
        previousProgress = currentProgress;

        float frameDistance = totalDistance * frameProgress;

        IsActive = elapsed < duration || followingSource != null;

        return direction * frameDistance;
    }

    /// <summary>
    /// 넉백 그만.
    /// </summary>
    public void Stop()
    {
        direction = Vector3.zero;
        totalDistance = 0f;
        duration = 0f;
        elapsed = 0f;
        previousProgress = 0f;
        followingSource = null;
        followGap = 0f;
        IsActive = false;
    }
}
