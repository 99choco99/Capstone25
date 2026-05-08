using System;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class DamageInfo
{
    public Enemy enemy;
    public Player player;
    public AttackType attackType;
    public float damage;
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

    //UI 변경 이벤트
    public static event Action<float, float> OnLocalPlayerHpChanged;        //체력
    public static event Action<float, float> OnLocalPlayerPostureChanged;   //가드게이지
    public static event Action<int, int> OnLocalPlayerExpChanged;           //경험치 변경
    public static event Action<PlayerStats> OnLocalPlayerStatsChanged;      //스탯 변경
    public static event Action<int> OnLocalPlayerGoldChanged;               //소유 골드 변경

    //로직 이벤트
    public event Action OnStatsSaved;
    public event Action<DamageInfo> OnDamaged;
    public event Action OnLevelUp;                  //레벨업시


    //상태
    public bool isGuarding;
    public bool isGroggy;
    public bool isInvincible;

    [SerializeField] private Player player;


    private void Start()
    {
        if (!player.IsLocalPlayer) { return; }

        OnStatsSaved += DataManager.Instance.SaveData;
        OnLevelUp += DataManager.Instance.SaveData;

    }

    private void OnDestroy()
    {
        if (!player.IsLocalPlayer) { return; }
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnSave -= OnSavePlayerData;
        }

        OnStatsSaved -= DataManager.Instance.SaveData;
        OnLevelUp -= DataManager.Instance.SaveData;
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


    //데미지를 입었을 때
    public override void OnDamage(DamageInfo finalDamage)
    {
        if (dead || isInvincible) return;

        if(finalDamage.damage > 0)
        {
            base.OnDamage(finalDamage);
        }
        OnDamaged?.Invoke(finalDamage);
        if (player.IsLocalPlayer)
        {
            OnLocalPlayerHpChanged?.Invoke(currentHp, maxHp);
        }
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
            AbilityPoint += 3;
            OnLevelUp?.Invoke();
            OnLocalPlayerStatsChanged?.Invoke(this);
            SoundManager.Instance.PlaySFX("LevelUp");
        }
        if (player.IsLocalPlayer) { OnLocalPlayerExpChanged?.Invoke(Exp, Level); }
    }

    public void AddGold(int addGold)
    {
        Gold += addGold;
        OnLocalPlayerGoldChanged?.Invoke(Gold);
    }
    public void SetGold(int gold)
    {
        Gold = gold;
        OnLocalPlayerGoldChanged?.Invoke(Gold);
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

        OnStatsSaved?.Invoke();
    }

    // 소비 아이템 사용
    public void Consume(ItemSpec spec)
    {
        bonusDamage += spec.damage;
        bonusDefense += spec.defense;
        RestoreHealth(spec.hp);
    }

}
