using System;
using System.Collections.Generic;
using UnityEngine;
using UniversalGraph;
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
    [SerializeField] private PlayerCombat playerCombat;
    private Player player;

    public override Faction TargetFaction => Faction.PlayerTeam;
    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int AbilityPoint { get; private set; }

    //UI 변경 이벤트
    public event Action<int, int> OnExpChanged;
    public event Action<PlayerStats> OnStatsChanged;

    //로직 이벤트
    public event Action<int> OnLevelUp;

    private readonly Dictionary<PlayerStatType, Stat> statMap = new();
    private readonly Dictionary<PlayerStatType, float> statGrowthRates = new()
    {
        { PlayerStatType.Health, 25f },
        { PlayerStatType.MaxPosture, 25f },
        { PlayerStatType.AttackPower, 2f }
    };

    protected override IDefenser Defenser => playerCombat;
    protected override PostureRecoveryMode CurrentPostureRecoveryMode =>
        player.StateMachine?.CurrentState?.PostureRecoveryMode ?? PostureRecoveryMode.Disabled;

    protected override void Awake()
    {
        base.Awake();

        playerCombat = GetComponent<PlayerCombat>();
        player = GetComponent<Player>();
        RegisterStats();
    }


    //게임 데이터 불러오기
    public void LoadPlayerData(PlayerData data)
    {
        float maxHp = data.maxHp > 0f ? data.maxHp : 100f;
        float attackPower = data.attackPower > 0f ? data.attackPower : AttackPower.GetBaseValue();

        MaxHp = new Stat(maxHp);
        MaxPosture = new Stat(data.maxPosture);
        AttackPower = new Stat(attackPower);

        CurrentHp = Mathf.Clamp(data.currentHp, 0f, maxHp);

        ResetPosture();

        Level = data.level;
        Exp = data.exp;
        AbilityPoint = data.abilityPoint;


        RegisterStats();


        UpdateTotalStats();
        Debug.Log("데이터 불러오기 성공");
    }

    //====================스탯 관련 ===========================

    /// <summary>
    /// 성장 종류와 실제 Stat 인스턴스를 연결
    /// </summary>
    private void RegisterStats()
    {
        statMap[PlayerStatType.Health] = MaxHp;
        statMap[PlayerStatType.MaxPosture] = MaxPosture;
        statMap[PlayerStatType.AttackPower] = AttackPower;
    }

    /// <summary>
    /// 경험치 증가
    /// </summary>
    public void AddExp(int addExp)
    {
        if (IsDead) return;
        Exp += addExp;
        int maxExp = DataManager.Instance.GetMaxExpForLevel(Level);

        // 레벨업 조건 체크
        while (Exp >= maxExp)
        {
            Exp -= maxExp;
            Level++;
            maxExp = DataManager.Instance.GetMaxExpForLevel(Level);
            AbilityPoint += 1;
            OnLevelUp?.Invoke(Level);
        }

        OnExpChanged?.Invoke(Exp, Level);
    }


    /// <summary>
    /// 스텟 증가
    /// </summary>
    public void UpAbility(PlayerStatType statType)
    {
        if (AbilityPoint <= 0) return;

        if (statMap.TryGetValue(statType, out Stat targetStat))
        {
            AbilityPoint--;
            targetStat.AddBaseValue(statGrowthRates[statType]);

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
