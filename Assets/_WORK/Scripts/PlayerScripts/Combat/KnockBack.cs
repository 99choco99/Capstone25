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

        CombatSettings.KnockbackSettings settings = CombatSettings.Current.Knockback;
        return settings.GetDefenderSpec(result.Request.ReactionType, result.DefenseType, result.Request.KnockBackLevel);
    }

    /// <summary>
    /// 공격자가 받을 최종 넉백량을 반환
    /// </summary>
    public static KnockbackSpec AttackerKnockBack(in DamageResult result)
    {
        if (!result.IsAccepted || result.DefenseType != DefenseType.Parry
            || result.Request.DeflectResponse != DeflectResponse.Rebound)
            return default;

        return CombatSettings.Current.Knockback.Parried.GetSpec(result.Request.KnockBackLevel);
    }

    public static bool CanApplyAttackPressure(in DamageResult result)
    {
        return result.IsAccepted && result.Request.KnockBackLevel != KnockBackLevel.None
            && (result.DefenseType != DefenseType.Parry || result.Request.DeflectResponse == DeflectResponse.Continue);
    }

    public static float GetAttackPressureGap(in DamageResult result, float bodyWidth, float defenderRadius)
    {
        DamageRequest request = result.Request;
        KnockbackSpec spec = DefenderKnockBack(result);
        float contactGap = !float.IsInfinity(result.ContactDistance) && !float.IsNaN(result.ContactDistance)
            ? result.ContactDistance : bodyWidth;
        float reactionGap = contactGap + spec.Distance;
        if (spec.Distance <= 0f || request.SourceWeapon == null || request.Attacker == null)
            return Mathf.Max(bodyWidth, reactionGap);

        float weaponReach = request.SourceWeapon.GetForwardReach(request.Attacker.transform, result.HitDirection);
        float minimumGap = bodyWidth;
        float maximumGap = Mathf.Max(minimumGap, Mathf.Max(contactGap, weaponReach + defenderRadius + CombatSettings.Current.GuardContactReach));
        return Mathf.Clamp(reactionGap, minimumGap, maximumGap);
    }

    public static Vector3 ClampAdvance(Vector3 deltaPosition, Vector3 direction, float currentDistance, float minimumDistance)
    {
        if (minimumDistance <= 0f) return deltaPosition;

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
    private float pendingAdvance;

    public bool IsActive { get; private set; }

    public bool IsFollowingAttack(Transform source) => source != null && IsActive && ReferenceEquals(followingSource, source);

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
        pendingAdvance = 0f;
        IsActive = true;
    }

    public void FollowAttack(Transform source, float gap)
    {
        if (!IsActive || source == null || float.IsInfinity(gap) || float.IsNaN(gap))
            return;

        if (!ReferenceEquals(followingSource, source)) pendingAdvance = 0f;
        followingSource = source;
        followGap = Mathf.Max(0f, gap);
    }

    public void AddAttackAdvance(Transform source, Vector3 actualDelta)
    {
        if (!ReferenceEquals(followingSource, source)) return;

        actualDelta.y = 0f;
        pendingAdvance += Mathf.Max(0f, Vector3.Dot(actualDelta, direction));
    }

    public Vector3 ConstrainRootMotion(Vector3 deltaPosition)
    {
        if (!IsActive || totalDistance <= 0f || elapsed >= duration) return deltaPosition;

        float opposingDistance = Vector3.Dot(deltaPosition, direction);
        return opposingDistance < 0f ? deltaPosition - direction * opposingDistance : deltaPosition;
    }

    public void StopFollowingAttack(Transform source)
    {
        if (!ReferenceEquals(followingSource, source)) return;

        followingSource = null;
        followGap = 0f;
        pendingAdvance = 0f;
        IsActive = elapsed < duration;
    }

    /// <summary>
    /// 1 프레임에 적용할 이동량을 반환
    /// </summary>
    public Vector3 Start(float deltaTime, Vector3 position)
    {
        if (!IsActive || deltaTime <= 0f)
            return Vector3.zero;

        float currentGap = 0f;
        if (followingSource != null)
        {
            Vector3 sourceDirection = position - followingSource.position;
            sourceDirection.y = 0f;
            currentGap = Vector3.Dot(sourceDirection, direction);
        }

        elapsed = Mathf.Min(elapsed + deltaTime, duration);
        float normalizedTime = Mathf.Clamp01(elapsed / duration);
        float currentProgress = elapsed >= duration ? 1f : Mathf.Clamp(movementCurve.Evaluate(normalizedTime), previousProgress, 1f);
        float frameProgress = currentProgress - previousProgress;
        previousProgress = currentProgress;

        float frameDistance = totalDistance * frameProgress;
        if (followingSource != null)
        {
            float remainingDistance = Mathf.Max(0f, followGap - currentGap - frameDistance);
            frameDistance += Mathf.Min(remainingDistance, pendingAdvance);
        }
        pendingAdvance = 0f;

        if (elapsed >= duration)
        {
            followingSource = null;
            followGap = 0f;
        }
        IsActive = elapsed < duration;

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
        pendingAdvance = 0f;
        IsActive = false;
    }
}
