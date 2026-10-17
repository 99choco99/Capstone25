using System;
using UnityEngine;

/// <summary>
/// 포인트로 성장시킬 수 있는 플레이어 스탯
/// </summary>
public enum PlayerStatType
{
    Health,
    MaxPosture,
    AttackPower
}

public class PlayerStats : LivingEntity
{
    private PlayerCombat playerCombat;
    private Player player;

    public override Faction TargetFaction => Faction.PlayerTeam;
    public Stat AttackPower { get; private set; }
    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int AbilityPoint { get; private set; }

    //UI 변경 이벤트
    public event Action<int, int> OnExpChanged;
    public event Action<PlayerStats> OnStatsChanged;

    //로직 이벤트

    protected override IDefenser Defenser => playerCombat;
    protected override float DeflectPostureMultiplier => PlayerGrowthSettings.Current.GetDeflectPostureMultiplier(
        AttackPower?.GetValue() ?? PlayerGrowthSettings.Current.InitialAttackPower);
    protected override CombatSettings.PostureRecovery PostureRecovery => CombatSettings.Current.PlayerPostureRecovery;
    protected override PostureRecoveryMode CurrentPostureRecoveryMode => player.StateMachine?.CurrentState?.PostureRecoveryMode
        ?? PostureRecoveryMode.Disabled;

    protected override void Awake()
    {
        base.Awake();
        AttackPower = new Stat(1f);

        playerCombat = GetComponent<PlayerCombat>();
        player = GetComponent<Player>();
    }

    //게임 데이터 불러오기
    public void LoadPlayerData(PlayerData data)
    {
        MaxHp = new Stat(data.maxHp);
        MaxPosture = new Stat(data.maxPosture);
        AttackPower = new Stat(data.attackPower);

        CurrentHp = Mathf.Clamp(data.currentHp, 0f, data.maxHp);

        ResetPosture();

        Level = data.level;
        Exp = data.exp;
        AbilityPoint = data.abilityPoint;

        Debug.Log("데이터 불러오기 성공");
    }

    //====================스탯 관련 ===========================

    public static long GetMaxExpForLevel(int level)
    {
        if (level < 1)
            throw new ArgumentOutOfRangeException(nameof(level));

        int expPerLevel = PlayerGrowthSettings.Current.ExpPerLevel;
        if (expPerLevel < 1)
            throw new InvalidOperationException("레벨당 필요 경험치는 1 이상이어야 합니다.");

        return (long)level * expPerLevel;
    }

    /// <summary>
    /// 경험치 증가
    /// </summary>
    public void AddExp(int addExp)
    {
        if (IsDead || addExp < 0)
            return;
        long remainingExp = (long)Exp + addExp;
        int nextLevel = Level;
        int nextAbilityPoint = AbilityPoint;
        long maxExp = GetMaxExpForLevel(nextLevel);

        // 레벨업 조건 체크
        while (remainingExp >= maxExp)
        {
            remainingExp -= maxExp;
            nextLevel = checked(nextLevel + 1);
            maxExp = GetMaxExpForLevel(nextLevel);
            nextAbilityPoint = checked(nextAbilityPoint + 1);
        }

        Exp = checked((int)remainingExp);
        Level = nextLevel;
        AbilityPoint = nextAbilityPoint;

        OnExpChanged?.Invoke(Exp, Level);
        OnStatsChanged?.Invoke(this);
    }

    /// <summary>
    /// 스텟 증가
    /// 성장 종류와 실제 Stat 인스턴스를 연결
    /// </summary>
    public void UpAbility(PlayerStatType statType)
    {
        Stat targetStat = GetUpgradeableStat(statType);
        if (targetStat == null)
            return;

        PlayerGrowthSettings settings = PlayerGrowthSettings.Current;
        float increase = Mathf.Min(
            settings.GetStatIncrease(statType),
            settings.GetMaxStatValue(statType) - targetStat.GetValue());
        AbilityPoint--;
        targetStat.AddBaseValue(increase);

        CurrentHp = Mathf.Clamp(CurrentHp, 0f, MaxHp.GetValue());
        OnStatsChanged?.Invoke(this);
    }

    public bool CanUpgradeStat(PlayerStatType statType)
    {
        return GetUpgradeableStat(statType) != null;
    }

    private Stat GetUpgradeableStat(PlayerStatType statType)
    {
        if (AbilityPoint <= 0)
            return null;

        Stat targetStat = statType switch
        {
            PlayerStatType.Health => MaxHp,
            PlayerStatType.MaxPosture => MaxPosture,
            PlayerStatType.AttackPower => AttackPower,
            _ => null
        };
        PlayerGrowthSettings settings = PlayerGrowthSettings.Current;
        return targetStat != null && settings.GetStatIncrease(statType) > 0f
            && targetStat.GetValue() < settings.GetMaxStatValue(statType) ? targetStat : null;
    }
}
