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
    public KnockBackLevel KnockBackLevel { get; }
    public AttackReactionType ReactionType { get; }
    public bool CanGuard { get; }
    public bool CanDeflect { get; }
    public DeflectResponse DeflectResponse { get; }
    public bool IsHeavyRebound => (KnockBackLevel == KnockBackLevel.Heavy || ReactionType == AttackReactionType.Finisher) && DeflectResponse == DeflectResponse.Rebound;

    private DamageRequest(GameObject attacker, Weapon sourceWeapon, Vector3 hitPoint, Vector3 hitDirection, in DamagePayload requestedPayload, AttackData attackData)
    {
        hitDirection.y = 0f;
        Attacker = attacker;
        SourceWeapon = sourceWeapon;
        HitPoint = hitPoint;
        HitDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector3.zero;
        RequestedPayload = requestedPayload;
        KnockBackLevel = attackData.ImpactLevel;
        ReactionType = attackData.ReactionType;
        CanGuard = attackData.Type == AttackType.Normal;
        CanDeflect = attackData.Type == AttackType.Normal || attackData.Type == AttackType.Thrust;
        DeflectResponse = attackData.DeflectResponse;
    }

    /// <summary>
    /// 피해 요청
    /// </summary>
    public static DamageRequest AttackDamage(GameObject attacker, Weapon sourceWeapon, AttackData attackData, float attackerAttackPower, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (attackData == null)
            throw new ArgumentNullException(nameof(attackData));

        float healthDamage = Mathf.Max(0f, attackerAttackPower) + attackData.damage;
        DamagePayload requestedPayload = new(healthDamage, attackData.postureDamage);

        return new DamageRequest(attacker, sourceWeapon, hitPoint, hitDirection, requestedPayload, attackData);
    }
}

/// <summary>
/// 피해의 결과를 계산
/// </summary>
public static class DamageCalculator
{
    public static DamagePayload Calculate(in DamageRequest request, DefenseType defense)
    {
        DamagePayload requested = request.RequestedPayload;
        float healthDamage = requested.HealthDamage;
        float postureDamage = requested.PostureDamage;

        switch (defense)
        {
            case DefenseType.Parry:
                healthDamage = 0f;
                postureDamage *= CombatSettings.Current.ParryPostureRatio;
                break;

            case DefenseType.NormalGuard:
                healthDamage = 0f;
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
    public float ContactDistance { get; }

    public Vector3 HitPoint => Request.HitPoint;
    public Vector3 HitDirection => Request.HitDirection;

    private DamageResult(in DamageRequest request, bool wasAccepted, DefenseType defense, float contactDistance)
    {
        Request = request;
        IsAccepted = wasAccepted;
        DefenseType = defense;
        ContactDistance = contactDistance;
    }

    public static DamageResult Accepted(in DamageRequest request, DefenseType defense, float contactDistance = float.PositiveInfinity)
    {
        return new DamageResult(request, wasAccepted: true, defense, contactDistance);
    }

    public static DamageResult Ignored(in DamageRequest request)
    {
        return new DamageResult(request, wasAccepted: false, DefenseType.None, float.PositiveInfinity);
    }
}
