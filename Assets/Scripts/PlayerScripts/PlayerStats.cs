using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
public struct DamageInfo
{
    public GameObject attacker;
    public GameObject target;
    public AttackType attackType;
    public float amount;
    public float postureDamage;

    public float knockbackForce;
    public float knockbackDuration;
    public Vector3 hitPoint;
    public Vector3 hitDirection;

    public bool wasGuarded;
    public bool wasParried;
}

public enum PlayerStatType { AttackPower, Health, Defense ,MaxPosture }

public class PlayerStats : LivingEntity
{
    public override Faction TargetFaction => Faction.PlayerTeam;
    public int Gold { get; private set; }
    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int AbilityPoint { get; private set; }

    //UI 변경 이벤트
    public event Action<int, int> OnExpChanged;
    public event Action<PlayerStats> OnStatsChanged;
    public event Action<int> OnGoldChanged;

    //로직 이벤트
    public event Action<DamageInfo> OnDamaged;
    public event Action<int> OnLevelUp;                  //레벨업시

    //상태
    public bool IsStunned { get; set; }
    public bool IsInvincible { get; set; }

    [SerializeField] private Player player;

    private void Start()
    {
        if (!player.IsLocalPlayer) { return; }
        player.Inventory.OnEquipmentChanged += RecalculateEquipmentStats;
    }

    private void OnDestroy()
    {
        if (!player.IsLocalPlayer) { return; }
        if (player.Inventory != null) player.Inventory.OnEquipmentChanged -= RecalculateEquipmentStats;
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnSave -= OnSavePlayerData;
        }
    }

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
        Gold = data.gold;
        AbilityPoint = data.abilityPoint;

        UpdateTotalStats();


        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnSave += OnSavePlayerData;
        }


        Debug.Log("데이터 불러오기 성공");
    }

    //게임 데이터 저장하기
    public void OnSavePlayerData()
    {
        if (DataManager.Instance == null) return;
    }

    //================= 게임 로직 ===================

    //데미지를 입었을 때
    public override void TakeDamage(DamageInfo info)
    {
        if (IsDead || IsInvincible) return;
        info = player.Combat.ProcessDefense(info);

        if (!info.wasParried) { TakePostureDamage(info.postureDamage); }
        if (info.amount > 0)
        {
            info.amount = Math.Max(1f, info.amount - Defense.GetValue());
            base.TakeDamage(info);
        }
        OnDamaged?.Invoke(info);
    }

    //체간 붕괴
    protected override void ProcessPostureBroken()
    {
        player.StateMachine.TransitionTo(player.StateMachine.PlayerStunState);
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
            AbilityPoint += 3;
            OnLevelUp?.Invoke(Level);
            SoundManager.Instance.PlaySFX("LevelUp");
        }
        if (player.IsLocalPlayer) { OnExpChanged?.Invoke(Exp, Level); }
    }

    public void AddGold(int addGold)
    {
        Gold += addGold;
        OnGoldChanged?.Invoke(Gold);
    }
    public void SetGold(int amount)
    {
        Gold = amount;
        OnGoldChanged?.Invoke(Gold);
    }


    //스텟 증가
    public void UpAbility(PlayerStatType statToUpgrade)
    {
        if (AbilityPoint <= 0) { return; }
        AbilityPoint--;
        switch (statToUpgrade)
        {
            case PlayerStatType.AttackPower: AttackPower.AddBaseValue(1f); break;
            case PlayerStatType.Defense: Defense.AddBaseValue(3f); break;
            case PlayerStatType.Health: MaxHp.AddBaseValue(10f); break;
        }

        UpdateTotalStats();

        OnStatsChanged?.Invoke(this);
    }

    public void UpgradeAttackPower() => UpAbility(PlayerStatType.AttackPower);
    public void UpgradeDefense() => UpAbility(PlayerStatType.Defense);
    public void UpgradeHealth() => UpAbility(PlayerStatType.Health);
    public void UpgradeMaxPosture() => UpAbility(PlayerStatType.MaxPosture);

    public void AddStatsModifier(ItemSpec spec) {
        AttackPower.AddModifier(spec.attackPower, spec);
        Defense.AddModifier(spec.defense, spec);
        MaxHp.AddModifier(spec.maxHp, spec);
        MaxPosture.AddModifier(spec.posture, spec);
        UpdateTotalStats();
    }
    public void RemoveStatsModifier(ItemSpec spec)
    {
        AttackPower.RemoveModifier(spec);
        Defense.RemoveModifier(spec);
        MaxHp.RemoveModifier(spec);
        MaxPosture.RemoveModifier(spec);
        UpdateTotalStats();
    }
    public void RecalculateEquipmentStats(List<EquipmentItemData> equippedItems)
    {
        AttackPower.ClearModifiers();
        Defense.ClearModifiers();
        MaxHp.ClearModifiers();
        MaxPosture.ClearModifiers();


        foreach (var item in equippedItems)
        {
            if (item != null)
            {
                AttackPower.AddModifier(item.baseStats.attackPower, item.baseStats);
                Defense.AddModifier(item.baseStats.defense, item.baseStats);
                MaxHp.AddModifier(item.baseStats.maxHp, item.baseStats);
                MaxPosture.AddModifier(item.baseStats.posture, item.baseStats);
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
