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
    [Min(1f)] public float InitialHealth = 100f;
    [Min(1f)] public float InitialPosture = 100f;
    [Min(0f)] public float InitialAttackPower = 1f;

    [Header("성장")]
    [Min(1)] public int ExpPerLevel = 10;
    [Min(0f)] public float HealthPerPoint = 25f;
    [Min(0f)] public float PosturePerPoint = 25f;
    [Min(0f)] public float AttackPowerPerPoint = 2f;

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
}
