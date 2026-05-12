using System;
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
    [Header("패링 시스템")]
    public float parryWindow = 0.2f; // 가드 버튼을 누르고 0.2초 안에 맞으면 패링 성공!
    private float guardStartTime;    // 가드를 올린 정확한 시간 기록

    public int Gold { get; private set; }
    public int Level { get; private set; }
    public int Exp { get; private set; }
    public int AbilityPoint { get; private set; }

    //UI 변경 이벤트
    public event Action<int, int> OnExpChanged;
    public event Action<PlayerStats> OnStatsChanged;
    public event Action<int> OnGoldChanged;

    //로직 이벤트
    public event Action OnStatsSaved;
    public event Action<DamageInfo> OnDamaged;
    public event Action OnLevelUp;                  //레벨업시

    //상태
    public bool isGuarding;
    public bool isStunned;
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

        AttackPower = new Stat(data.damage);
        Defense = new Stat(data.defense);
        MaxHp = new Stat(data.maxHp);
        MaxPosture = new Stat(100f);

        CurrentHp = data.currentHp;
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


    //가드를 올릴 때
    public void SetGuardState(bool isGuarding)
    {
        this.isGuarding = isGuarding;
        if (isGuarding)
        {
            guardStartTime = Time.time;
        }
    }

    //데미지를 입었을 때
    public override void OnDamage(DamageInfo finalDamage)
    {
        if (dead || isInvincible) return;

        bool isFrontHit = Vector3.Dot(transform.forward, finalDamage.hitDirection) < 0.2f;

        if (isGuarding && isFrontHit)
        {
            //패링 성공시
            if (Time.time - guardStartTime <= parryWindow)
            {
                finalDamage.wasParried = true;
                finalDamage.amount = 0f;
            }
            else //일반 가드 시
            {
                finalDamage.wasGuarded = true;
                finalDamage.amount = 0f;

                TakePostureDamage(finalDamage.postureDamage);
            }
        } else
        {
            finalDamage.amount = Math.Max(1f, finalDamage.amount - Defense.GetValue());
            if (finalDamage.amount > 0)
            {
                base.OnDamage(finalDamage);
            }

        }
        OnDamaged?.Invoke(finalDamage);
    }

    //경험치 증가
    public void AddExp(int addExp)
    {
        if (dead) return;
        Exp += addExp;
        int maxExp = DataManager.Instance.GetMaxExpForLevel(Level);

        // 레벨업 조건 체크
        while (Exp >= maxExp)
        {
            Exp -= maxExp;
            Level++;
            AbilityPoint += 3;
            OnLevelUp?.Invoke();
            OnStatsChanged?.Invoke(this);
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
    }

    public void UpgradeDamage() => UpAbility(PlayerStatType.AttackPower);
    public void UpgradeDefense() => UpAbility(PlayerStatType.Defense);
    public void UpgradeHealth() => UpAbility(PlayerStatType.Health);

    public void AddStatsModifier(ItemSpec spec) {
        AttackPower.AddModifier(spec.attackPower);
        Defense.AddModifier(spec.defense);
        MaxHp.AddModifier(spec.hp);
        UpdateTotalStats();
    }
    public void RemoveStatsModifier(ItemSpec spec)
    {
        AttackPower.RemoveModifier(spec.attackPower);
        Defense.RemoveModifier(spec.defense);
        MaxHp.RemoveModifier(spec.hp);
        UpdateTotalStats();
    }

    private void UpdateTotalStats()
    {
        if (CurrentHp > base.MaxHp.GetValue())
        {
            CurrentHp = base.MaxHp.GetValue();
        }

        OnStatsSaved?.Invoke();
    }
}
