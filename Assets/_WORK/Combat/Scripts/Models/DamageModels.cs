using System;
using UnityEngine;

/// <summary>
/// 방어 유형
/// </summary>
public enum DefenseType
{
    None,
    NormalGuard,
    Parry
}

/// <summary>
/// 피해 파이프라인에서 계산하는 순수 피해 묶음
/// </summary>
public readonly struct DamagePayload
{
    public float HealthDamage { get; }
    public float PostureDamage { get; }

    public DamagePayload(float healthDamage, float postureDamage)
    {
        HealthDamage = Mathf.Max(0f, healthDamage);
        PostureDamage = Mathf.Max(0f, postureDamage);
    }
}

/// <summary>
/// 공격자가 수신자에게 보내는 피해 요청
/// </summary>
public readonly struct DamageRequest
{
    public GameObject Attacker { get; }
    public Weapon SourceWeapon { get; }
    public Vector3 HitPoint { get; }
    public Vector3 HitDirection { get; }

    public DamagePayload RequestedPayload { get; }
    public float? GuardPostureDamage { get; }
    public float? ParryPostureDamage { get; }
    public float? DeflectedPostureDamage { get; }
    public KnockBackLevel KnockBackLevel { get; }
    /// <summary>Heavy의 직접 피격은 다운, 나머지는 일반 피격으로 처리합니다.</summary>
    public HitReactionType HitReaction => KnockBackLevel == KnockBackLevel.Heavy
        ? HitReactionType.Knockdown
        : HitReactionType.Normal;
    public AttackImpactProfile ImpactProfile { get; }
    public bool CanGuard { get; }
    public bool CanDeflect { get; }
    public DeflectResponse DeflectResponse { get; }
    public bool IsHeavyRebound => (KnockBackLevel == KnockBackLevel.Heavy
        || ImpactProfile != null
        && ImpactProfile.ForceHeavyRebound)
        && DeflectResponse == DeflectResponse.Rebound;
    public bool UseHeavyDefenseFeedback => IsHeavyRebound
        || ImpactProfile != null
        && ImpactProfile.UseHeavyDefenseFeedback;

    private DamageRequest(
        GameObject attacker,
        Weapon sourceWeapon,
        Vector3 hitPoint,
        Vector3 hitDirection,
        in DamagePayload requestedPayload,
        AttackData attackData,
        float? guardPostureDamage,
        float? parryPostureDamage,
        float? deflectedPostureDamage)
    {
        hitDirection.y = 0f;
        Attacker = attacker;
        SourceWeapon = sourceWeapon;
        HitPoint = hitPoint;
        HitDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector3.zero;
        RequestedPayload = requestedPayload;
        GuardPostureDamage = guardPostureDamage;
        ParryPostureDamage = parryPostureDamage;
        DeflectedPostureDamage = deflectedPostureDamage;
        KnockBackLevel = attackData.ImpactLevel;
        ImpactProfile = attackData.ImpactProfile;
        CanGuard = attackData.CanGuard;
        CanDeflect = true;
        DeflectResponse = attackData.DeflectResponse;
    }

    /// <summary>
    /// 피해 요청
    /// </summary>
    public static DamageRequest AttackDamage(
        GameObject attacker,
        Weapon sourceWeapon,
        AttackData attackData,
        Vector3 hitPoint,
        Vector3 hitDirection,
        float attackerAttackPower = 1f,
        float healthDamageMultiplier = 1f,
        float postureDamageMultiplier = 1f)
    {
        if (attackData == null)
            throw new ArgumentNullException(nameof(attackData));

        float healthDamage = attackData.damage;
        float postureDamage = attackData.postureDamage;
        float attackerDamageMultiplier = 1f;
        if (attackData is PlayerAttackData)
        {
            PlayerGrowthSettings settings = PlayerGrowthSettings.Current;
            attackerDamageMultiplier = settings.GetAttackPowerMultiplier(attackerAttackPower);
        }
        else if (attackData is EnemyAttackData
            && attacker != null
            && attacker.TryGetComponent(out EnemyStats enemyStats))
        {
            attackerDamageMultiplier = enemyStats.DamageMultiplier;
        }
        healthDamage *= attackerDamageMultiplier;
        postureDamage *= attackerDamageMultiplier;
        DamagePayload requestedPayload = new(
            healthDamage * Mathf.Max(0f, healthDamageMultiplier),
            postureDamage * Mathf.Max(0f, postureDamageMultiplier));

        float? guardPostureDamage = null, parryPostureDamage = null, deflectedPostureDamage = null;
        if (attackData.OverrideDefensePostureDamage)
        {
            float hitScale = Mathf.Max(0f, postureDamageMultiplier);
            guardPostureDamage = Mathf.Max(0f, attackData.GuardPostureDamage) * attackerDamageMultiplier * hitScale;
            parryPostureDamage = Mathf.Max(0f, attackData.ParryPostureDamage) * attackerDamageMultiplier * hitScale;
            deflectedPostureDamage = Mathf.Max(0f, attackData.DeflectedPostureDamage) * hitScale;
        }

        return new DamageRequest(attacker, sourceWeapon, hitPoint, hitDirection, requestedPayload, attackData,
            guardPostureDamage, parryPostureDamage, deflectedPostureDamage);
    }
}

/// <summary>
/// 피해의 결과를 계산
/// </summary>
public static class DamageCalculator
{
    public static DamagePayload Calculate(in DamageRequest request, DefenseType defense, bool isPlayer = false)
    {
        DamagePayload requested = request.RequestedPayload;
        float healthDamage = requested.HealthDamage;
        float postureDamage = requested.PostureDamage;

        switch (defense)
        {
            case DefenseType.Parry:
                healthDamage = 0f;
                postureDamage = request.ParryPostureDamage ?? postureDamage * CombatSettings.Current.ParryPostureRatio;
                break;

            case DefenseType.NormalGuard:
                healthDamage = 0f;
                postureDamage = request.GuardPostureDamage
                    ?? postureDamage * (isPlayer
                    ? CombatSettings.Current.PlayerGuardPostureRatio
                    : CombatSettings.Current.GuardPostureRatio);
                break;
        }

        return new DamagePayload(healthDamage, postureDamage);
    }
}

/// <summary>
/// 공격에 대한 최종 결과 보고
/// </summary>
public readonly struct DamageResult
{
    public DamageRequest Request { get; }

    /// <summary>
    /// 공격을 유효하게 처리할 것인지.
    /// </summary>
    public bool IsAccepted { get; }

    /// <summary>수비자가 확정한 방어 결과</summary>
    public DefenseType DefenseType { get; }
    /// <summary>확정된 패링 결과와 공격 설정이 공격자 반동 조건을 만족하는지.</summary>
    public bool IsParryRebound => DefenseType == DefenseType.Parry && Request.DeflectResponse == DeflectResponse.Rebound;
    public float ContactDistance { get; }
    /// <summary>이번 피해를 적용하기 전부터 체간이 붕괴되어 있었는지.</summary>
    public bool WasPostureBroken { get; }

    public Vector3 HitPoint => Request.HitPoint;
    public Vector3 HitDirection => Request.HitDirection;

    private DamageResult(
        in DamageRequest request,
        bool wasAccepted,
        DefenseType defense,
        float contactDistance,
        bool wasPostureBroken)
    {
        Request = request;
        IsAccepted = wasAccepted;
        DefenseType = defense;
        ContactDistance = contactDistance;
        WasPostureBroken = wasPostureBroken;
    }

    public static DamageResult Accepted(
        in DamageRequest request,
        DefenseType defense,
        float contactDistance = float.PositiveInfinity,
        bool wasPostureBroken = false)
    {
        return new DamageResult(request, wasAccepted: true, defense, contactDistance, wasPostureBroken);
    }

    public static DamageResult Ignored(in DamageRequest request)
    {
        return new DamageResult(
            request,
            wasAccepted: false,
            DefenseType.None,
            float.PositiveInfinity,
            wasPostureBroken: false);
    }
}
