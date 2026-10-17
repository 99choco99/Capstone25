using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerGrowthSettings", menuName = "Scriptable Objects/PlayerGrowthSettings")]
public sealed class PlayerGrowthSettings : ScriptableObject
{
    private static PlayerGrowthSettings current;

    public static PlayerGrowthSettings Current
    {
        get
        {
            if (current == null)
            {
                current = Resources.Load<PlayerGrowthSettings>("PlayerGrowthSettings");
                if (current == null)
                    throw new InvalidOperationException("Resources/PlayerGrowthSettings 에셋이 필요합니다.");
            }
            return current;
        }
    }

    [Header("초기 스탯")]
    [Min(1)] public int InitialLevel = 1;
    [Min(1f)] public float InitialHealth = 560f;
    [Min(1f)] public float InitialPosture = 210f;
    [Min(0f)] public float InitialAttackPower = 3f;

    [Header("성장")]
    [Min(1)] public int ExpPerLevel = 10;
    [Min(0f)] public float HealthPerPoint = 40f;
    [Min(0f)] public float PosturePerPoint = 15f;
    [Min(0f)] public float AttackPowerPerPoint = 0.25f;
    [Min(1f)] public float MaximumHealth = 1120f;
    [Min(1f)] public float MaximumPosture = 420f;
    [Min(1)] public int MaximumAttackPower = 99;

    public float GetStatIncrease(PlayerStatType statType)
    {
        return statType switch
        {
            PlayerStatType.Health => HealthPerPoint,
            PlayerStatType.MaxPosture => PosturePerPoint,
            PlayerStatType.AttackPower => AttackPowerPerPoint,
            _ => throw new ArgumentOutOfRangeException(nameof(statType))
        };
    }

    public float GetMaxStatValue(PlayerStatType statType)
    {
        return statType switch
        {
            PlayerStatType.Health => MaximumHealth,
            PlayerStatType.MaxPosture => MaximumPosture,
            PlayerStatType.AttackPower => MaximumAttackPower,
            _ => throw new ArgumentOutOfRangeException(nameof(statType))
        };
    }

    /// <summary>직접 공격과 패링 체간에 공통으로 적용하는 원본 공격력 배율. 분할 투자는 원본 정수 단계 사이를 보간합니다.</summary>
    public float GetAttackPowerMultiplier(float attackPower)
    {
        float power = Mathf.Clamp(attackPower, 1f, Mathf.Min(MaximumAttackPower, 99f));
        if (power <= 14f)
            return 1f + (power - 1f) * 0.25f;
        if (power <= 27f)
            return 4.25f + (power - 14f) * 0.1f;
        if (power <= 51f)
            return 5.55f + (power - 27f) * 0.05f;
        if (power <= 52f)
            return 6.75f + (power - 51f) * 0.025f;
        // 원본 SpEffect 160052/160053의 6.775 → 6.77도 그대로 반영합니다.
        if (power <= 53f)
            return 6.775f - (power - 52f) * 0.005f;
        return 6.77f + (power - 53f) * 0.01f;
    }

    public float GetDeflectPostureMultiplier(float attackPower) => GetAttackPowerMultiplier(attackPower);
}
