using System;

using UnityEngine;




public class DamageInfo
{
    public Player player;
    public AttackType attackType;
    public float finalDamage;
    public float knockbackForce;
    public float knockbackDuration;
    public Vector3 hitPoint;
    public Vector3 hitDirection;
    public bool wasGuarded;
    public bool wasParried;

}

public enum PlayerStatType { Damage, Health, Defense }

public class PlayerStats : LivingEntity
{
    public int Gold { get; private set; }
    public int Level { get; private set; }
    public int Exp { get; private set; }

    public int AbilityPoint { get; private set; }

    public float baseDamage;
    public float baseDefense;
    public float baseMaxHp;

    public float bonusDamage;
    public float bonusDefense;
    public float bonusMaxHp;

    //PlayerEvent
    public event Action<int, int> OnExpChanged;   // 경험치 적용
    public event Action OnStatsChanged;             // 스탯 변경사항 적용
    public event Action<DamageInfo> OnDamaged;
    public event Action OnLevelUp;           //레벨업시
    public event Action<int> OnChangedGold;  // 소유 골드가 변경됨.


    public bool isGuarding;

    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
        player.Inventory.OnQuickSlotUsed += Consume;

    }

    private void Start()
    {
        LoadPlayerData(DataManager.Instance.playerData);
    }

    private void OnDestroy()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnSave -= OnSavePlayerData;
        }
        player.Inventory.OnQuickSlotUsed -= Consume;
    }

    //게임 데이터 불러오기
    public void LoadPlayerData(PlayerData data)
    {
        gameObject.name = data.nickname;

        baseMaxHp = data.maxHp;
        baseDefense = data.defense;
        baseDamage = data.damage;
        currentHp = data.currentHp;
        AbilityPoint = data.AbilityPoint;


        Level = data.level;
        Exp = data.exp;
        Gold = data.gold;

        UpdateTotalStats();

        OnExpChanged?.Invoke(Exp, Level);
        OnChangedGold?.Invoke(Gold);

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

        PlayerData dataToSave = DataManager.Instance.playerData;
        dataToSave.maxHp = baseMaxHp;
        dataToSave.currentHp = currentHp;
        dataToSave.damage = baseDamage;
        dataToSave.defense = baseDefense;
        dataToSave.level = Level;
        dataToSave.exp = Exp;
        dataToSave.gold = Gold;
        dataToSave.AbilityPoint = AbilityPoint;

        PublicAPIManager.Instance.PlayerData.RequestSavePlayerData(dataToSave);
    }


    //데미지를 입었을 때
    public override void OnDamage(DamageInfo damageInfo)
    {
        if (dead) return;

        // --- 가드 및 패링 판정 ---
        isGuarding = player.StateMachine.CurrentState is PlayerGuardState;
        bool isParrying = false;

        if (isGuarding)
        {
            if (player.StateMachine.CurrentState is PlayerGuardState guardState && guardState.IsParryWindowActive())
            {
                isParrying = true;
            }
        }

        // --- 데미지 계산 ---
        float finalDamageToHp = damageInfo.finalDamage;

        if (isParrying)
        {
            finalDamageToHp = 0;
        }
        else if (isGuarding)
        {
            finalDamageToHp = 0;
            TakePostureDamage(damageInfo.finalDamage);
        }
        else
        {
            base.OnDamage(damageInfo);
            TakePostureDamage(damageInfo.finalDamage * 0.5f);
        }


        DamageInfo result = new DamageInfo()
        {
            finalDamage = finalDamageToHp,
            attackType = damageInfo.attackType,
            hitPoint = damageInfo.hitPoint,
            hitDirection = damageInfo.hitDirection,
            knockbackDuration = damageInfo.knockbackDuration,
            knockbackForce = damageInfo.knockbackForce,
            wasGuarded = isGuarding,
            wasParried = isParrying,
        };

        OnDamaged?.Invoke(result);
    }

    //경험치 증가
    public void AddExp(int addExp)
    {
        if (dead) return;
        Exp += addExp;
        int maxExp = DataManager.Instance.GetMaxExpForLevel(Level);
        if(maxExp == int.MaxValue) { Exp = 0; }
        // 레벨업 조건 체크
        while(Exp >= maxExp)
        {
            Exp -= maxExp;
            Level++;
            AbilityPoint += 3;
            OnLevelUp?.Invoke();
            OnStatsChanged?.Invoke();
            SoundManager.Instance.PlaySFX("LevelUp");
        }
        OnExpChanged?.Invoke(Exp, Level);
    }

    public void AddGold(int addGold)
    {
        Gold += addGold;
        OnChangedGold?.Invoke(Gold);
    }
    public void SetGold(int gold)
    {
        Gold = gold;
        OnChangedGold?.Invoke(Gold);
    }


    //스텟 증가
    public void UpAbility(PlayerStatType statToUpgrade)
    {
        if (AbilityPoint <= 0) { return; }
        AbilityPoint--;
        switch (statToUpgrade)
        {
            case PlayerStatType.Damage: baseDamage++; break;
            case PlayerStatType.Defense: baseDefense += 3; break;
            case PlayerStatType.Health: baseMaxHp += 10; break;
        }

        UpdateTotalStats();
    }

    public void UpgradeDamage()
    {
        UpAbility(PlayerStatType.Damage);
    }

    public void UpgradeDefense()
    {
        UpAbility(PlayerStatType.Defense);
    }

    public void UpgradeHealth()
    {
        UpAbility(PlayerStatType.Health);
    }

    public void ApplyStatChanges(ItemSpec spec, bool IsEquip = true) {
        if(IsEquip)
        {
            bonusDamage += spec.damage;
            bonusDefense += spec.defense;
            bonusMaxHp += spec.hp;
        }
        else
        {
            bonusDamage -= spec.damage;
            bonusDefense -= spec.defense;
            bonusMaxHp -= spec.hp;
        }
        UpdateTotalStats();
    }

    private void UpdateTotalStats()
    {
        damage = baseDamage + bonusDamage;
        maxPosture = baseDefense + bonusDefense;
        maxHp = baseMaxHp + bonusMaxHp;

        if (currentHp > maxHp)
        {
            currentHp = maxHp;
        }

        OnStatsChanged?.Invoke();
    }

    // 소비 아이템 사용
    public void Consume(ItemSpec spec)
    {
        bonusDamage += spec.damage;
        bonusDefense += spec.defense;
        RestoreHealth(spec.hp);
    }

}
