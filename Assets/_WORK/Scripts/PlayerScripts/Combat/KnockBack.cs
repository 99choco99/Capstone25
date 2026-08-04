using UnityEngine;

/// <summary>
///넉백 이동에 필요한 최종 거리와 시간
/// </summary>
public readonly struct KnockbackSpec
{
    public float Distance { get; }
    public float Duration { get; }

    public KnockbackSpec(float distance, float duration)
    {
        Distance = Mathf.Max(0f, distance);
        Duration = Mathf.Max(0f, duration);
    }
}

/// <summary>
/// 넉백 관련 수치정의 및 계산
/// </summary>
public static class KnockBackPolicy
{
    private const float DirectHitDuration = 0.26f;
    private const float GuardDuration = 0.20f;
    private const float ParryDuration = 0.24f;

    /// <summary>
    /// 수비자의 최종 넉백량을 반환
    /// </summary>
    public static KnockbackSpec DefenderKnockBack(in DamageResult result)
    {
        if (!result.IsAccepted)
            return default;

        KnockBackLevel level = result.Request.KnockBackLevel;
        return result.DefenseType switch
        {
            DefenseType.None => new KnockbackSpec(GetDirectHitDistance(level), DirectHitDuration),
            DefenseType.NormalGuard => new KnockbackSpec(GetGuardDistance(level), GuardDuration),
            DefenseType.Parry => default,
            _ => default
        };
    }

    /// <summary>
    /// 공격자가 받을 최종 넉백량을 반환
    /// </summary>
    public static KnockbackSpec AttackerKnockBack(in DamageResult result)
    {
        if (!result.IsAccepted)
            return default;

        return result.DefenseType == DefenseType.Parry ? new KnockbackSpec(GetParryDistance(result.Request.KnockBackLevel),ParryDuration) : default;
    }

    private static float GetDirectHitDistance(KnockBackLevel level)
    {
        return level switch
        {
            KnockBackLevel.Light => 0.40f,
            KnockBackLevel.Medium => 0.65f,
            KnockBackLevel.Heavy => 0.95f,
            _ => 0f
        };
    }

    private static float GetGuardDistance(KnockBackLevel level)
    {
        return level switch
        {
            KnockBackLevel.Light => 0.15f,
            KnockBackLevel.Medium => 0.27f,
            KnockBackLevel.Heavy => 0.42f,
            _ => 0f
        };
    }

    private static float GetParryDistance(KnockBackLevel level)
    {
        return level switch
        {
            KnockBackLevel.Light => 0.18f,
            KnockBackLevel.Medium => 0.30f,
            KnockBackLevel.Heavy => 0.46f,
            _ => 0f
        };
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

    public bool IsActive { get; private set; }


    /// <summary>
    /// 방향으로 spec만큼 이동 준비
    /// </summary>
    public void Ready(Vector3 worldDirection, KnockbackSpec spec)
    {
        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude <= 0.0001f || spec.Distance <= 0f || spec.Duration <= 0f)
        {
            Stop();
            return;
        }

        direction = worldDirection.normalized;
        totalDistance = spec.Distance;
        duration = spec.Duration;
        elapsed = 0f;
        previousProgress = 0f;
        IsActive = true;
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
        float remaining = 1f - normalizedTime;
        float currentProgress = 1f - remaining * remaining * remaining; //1 - (1-t)^3 (Cubic Ease-Out)
        float frameProgress = Mathf.Max(0f, currentProgress - previousProgress);
        previousProgress = currentProgress;

        Vector3 frameDisplacement = direction * (totalDistance * frameProgress);
        if (elapsed >= duration)
            IsActive = false;

        return frameDisplacement;
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
        IsActive = false;
    }
}
