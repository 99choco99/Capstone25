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
    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int AbilityPoint { get; private set; }

    //UI 변경 이벤트
    public event Action<int, int> OnExpChanged;
    public event Action<PlayerStats> OnStatsChanged;

    //로직 이벤트

    protected override IDefenser Defenser => playerCombat;
    protected override CombatSettings.PostureRecovery PostureRecovery => CombatSettings.Current.PlayerPostureRecovery;
    protected override PostureRecoveryMode CurrentPostureRecoveryMode => player.StateMachine?.CurrentState?.PostureRecoveryMode ?? PostureRecoveryMode.Disabled;

    protected override void Awake()
    {
        base.Awake();

        playerCombat = GetComponent<PlayerCombat>();
        player = GetComponent<Player>();
    }

    //게임 데이터 불러오기
    public void LoadPlayerData(PlayerData data)
    {
        PlayerGrowthSettings settings = PlayerGrowthSettings.Current;
        float maxHp = data.maxHp > 0f ? data.maxHp : settings.InitialHealth;
        float maxPosture = data.maxPosture > 0f ? data.maxPosture : settings.InitialPosture;
        float attackPower = data.attackPower > 0f ? data.attackPower : settings.InitialAttackPower;

        MaxHp = new Stat(maxHp);
        MaxPosture = new Stat(maxPosture);
        AttackPower = new Stat(attackPower);

        CurrentHp = Mathf.Clamp(data.currentHp, 0f, maxHp);

        ResetPosture();

        Level = data.level;
        Exp = data.exp;
        AbilityPoint = data.abilityPoint;

        Debug.Log("데이터 불러오기 성공");
    }

    //====================스탯 관련 ===========================

    public static int GetMaxExpForLevel(int level)
    {
        return level * PlayerGrowthSettings.Current.ExpPerLevel;
    }

    /// <summary>
    /// 경험치 증가
    /// </summary>
    public void AddExp(int addExp)
    {
        if (IsDead) return;
        Exp += addExp;
        int maxExp = GetMaxExpForLevel(Level);

        // 레벨업 조건 체크
        while (Exp >= maxExp)
        {
            Exp -= maxExp;
            Level++;
            maxExp = GetMaxExpForLevel(Level);
            AbilityPoint += 1;
        }

        OnExpChanged?.Invoke(Exp, Level);
        OnStatsChanged?.Invoke(this);
    }

    /// <summary>
    /// 스텟 증가
    /// 성장 종류와 실제 Stat 인스턴스를 연결
    /// </summary>
    public void UpAbility(PlayerStatType statType)
    {
        if (AbilityPoint <= 0) return;

        Stat targetStat = statType switch
        {
            PlayerStatType.Health => MaxHp,
            PlayerStatType.MaxPosture => MaxPosture,
            PlayerStatType.AttackPower => AttackPower,
            _ => null
        };

        if (targetStat != null)
        {
            AbilityPoint--;
            targetStat.AddBaseValue(PlayerGrowthSettings.Current.GetStatIncrease(statType));

            UpdateTotalStats();
            OnStatsChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// 전체 스탯 재계산
    /// </summary>
    private void UpdateTotalStats()
    {
        CurrentHp = Mathf.Clamp(CurrentHp, 0f, MaxHp.GetValue());
    }
}
