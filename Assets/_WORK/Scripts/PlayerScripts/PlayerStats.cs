using System;
using System.Collections.Generic;
using UnityEngine;
public enum PlayerStatType { AttackPower, Health, Defense ,MaxPosture }

public class PlayerStats : LivingEntity
{
    [SerializeField] private PlayerCombat playerCombat;
    public override Faction TargetFaction => Faction.PlayerTeam;
    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int AbilityPoint { get; private set; }

    //UI 변경 이벤트
    public event Action<int, int> OnExpChanged;
    public event Action<PlayerStats> OnStatsChanged;

    //로직 이벤트
    public event Action<DamageEvent> OnDamaged;
    public event Action<int> OnLevelUp;                  //레벨업시

    //상태
    public bool IsInvincible { get; set; }

    private readonly Dictionary<PlayerStatType, Stat> statMap = new();

    private readonly Dictionary<PlayerStatType, float> statGrowthRates = new()
    {
        { PlayerStatType.AttackPower, 1f },
        { PlayerStatType.Defense, 3f },
        { PlayerStatType.Health, 10f },
        { PlayerStatType.MaxPosture, 10f }
    };

    //게임 데이터 불러오기
    public void LoadPlayerData(PlayerData data)
    {
        gameObject.name = data.nickname;

        AttackPower = new Stat(data.damage);
        Defense = new Stat(data.defense);
        MaxHp = new Stat(data.maxHp);
        MaxPosture = new Stat(100f);

        CurrentHp = data.currentHp;

        Level = data.level;
        Exp = data.exp;
        AbilityPoint = data.abilityPoint;

        statMap[PlayerStatType.AttackPower] = AttackPower;
        statMap[PlayerStatType.Defense] = Defense;
        statMap[PlayerStatType.Health] = MaxHp;
        statMap[PlayerStatType.MaxPosture] = MaxPosture;


        UpdateTotalStats();
        Debug.Log("데이터 불러오기 성공");
    }


    //================= 게임 로직 ===================

    //데미지를 입었을 때
    public override void TakeDamage(ref DamageEvent damageEvent)
    {
        if (IsDead || IsInvincible) return;

        playerCombat.EvaluateDefense(ref damageEvent);

        if (damageEvent.isCancelled) return;

        //패링 성공시
        if (damageEvent.wasParried) {
            damageEvent.currentDamage = 0f;
            damageEvent.currentKnockbackForce = 0f;
            if (damageEvent.attacker != null && damageEvent.attacker.TryGetComponent<LivingEntity>(out var attacker))
            {
                attacker.TakePostureDamage(damageEvent.currentPostureDamage);
            }

        }
        else if (damageEvent.wasGuarded)
        {
            //일반 가드
            damageEvent.currentDamage = 0f;
            TakePostureDamage(damageEvent.currentPostureDamage);
        }
        else
        {
            //방어 실패
            TakePostureDamage(damageEvent.currentPostureDamage);
            if (damageEvent.currentDamage > 0)
            {
                damageEvent.currentDamage = Math.Max(1f, damageEvent.currentDamage - Defense.GetValue());
                base.TakeDamage(ref damageEvent);
            }
        }
        OnDamaged?.Invoke(damageEvent);
    }



    //====================스탯 관련 ===========================

    //경험치 증가
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
            AbilityPoint += 1;
            OnLevelUp?.Invoke(Level);
            SoundManager.Instance.PlaySFX("LevelUp");
        }

        OnExpChanged?.Invoke(Exp, Level);
    }
    //스텟 증가
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

    public void RecalculateEquipmentStats(List<EquipmentInstance> equippedItems)
    {
        AttackPower.ClearModifiers();
        Defense.ClearModifiers();
        MaxHp.ClearModifiers();
        MaxPosture.ClearModifiers();


        foreach (var item in equippedItems)
        {
            if (item != null)
            {
                ItemSpec fianlStats = item.GetFinalStats();
                AttackPower.AddModifier(fianlStats.attackPower, fianlStats);
                Defense.AddModifier(fianlStats.defense, fianlStats);
                MaxHp.AddModifier(fianlStats.maxHp, fianlStats);
                MaxPosture.AddModifier(fianlStats.posture, fianlStats);
            }
        }

        UpdateTotalStats();

        OnStatsChanged?.Invoke(this);
    }
    private void UpdateTotalStats()
    {
        if (CurrentHp > base.MaxHp.GetValue())
        {
            CurrentHp = base.MaxHp.GetValue();
        }
    }
}
