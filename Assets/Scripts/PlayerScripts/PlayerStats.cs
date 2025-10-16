using System;

using UnityEngine;




public class DamageInfo
{
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
    // 데이터를 보관하고, 변경 시 외부에 이벤트를 통해 알리는 것
    // 플레이어만의 고유한 데이터
    public string ID { get; private set; }
    public string Nickname { get; private set; }
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
    public event Action OnStatsChanged;  // 스탯 변경사항 적용
    public event Action<DamageInfo> OnDamaged;
    public event Action OnLevelUp;  //레벨업시


    public bool isGuarding;

    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();

    }
    private void Start()
    {
        LoadPlayerData(DataManager.Instance.playerData);
        InventoryEvents.OnQuickSlotUsed += Consume;
    }

    private void OnDestroy()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnSave -= OnSavePlayerData;
        }
        InventoryEvents.OnQuickSlotUsed -= Consume;
    }


    //게임 데이터 불러오기
    public void LoadPlayerData(PlayerData data)
    {
        ID = data.id;
        Nickname = data.nickname;
        gameObject.name = Nickname;

        baseMaxHp = data.maxHp;
        baseDefense = data.defense;
        baseDamage = data.damage;
        currentHp = data.currentHp;



        Level = data.level;
        Exp = data.exp;
        Gold = data.gold;

        UpdateTotalStats();

        OnExpChanged?.Invoke(Exp, Level);

        InventoryEvents.OnChangedGold?.Invoke(Gold);

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

        Debug.Log($"[{Nickname}] 데이터 저장 준비 완료.");
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

        // 레벨업 조건 체크
        while(Exp >= maxExp)
        {
            Exp -= maxExp;
            Level++;
            OnLevelUp?.Invoke();
        }
        OnExpChanged?.Invoke(Exp, Level);
    }

    public void AddGold(int addGold)
    {
        Gold += addGold;
        InventoryEvents.OnChangedGold?.Invoke(Gold);
    }


    //스텟 증가
    public void UpAbility(PlayerStatType statToUpgrade)
    {
        if (AbilityPoint <= 0) { return; }
        AbilityPoint--;
        switch (statToUpgrade)
        {
            case PlayerStatType.Damage: baseDamage++; break;
            case PlayerStatType.Defense: baseDefense++; break;
            case PlayerStatType.Health: baseMaxHp++; break;
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
