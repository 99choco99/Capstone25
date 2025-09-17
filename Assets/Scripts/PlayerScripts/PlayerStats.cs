using System;
using UnityEditor.Playables;
using UnityEngine;
using WebSocketSharp;
using static Cinemachine.DocumentationSortingAttribute;


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

    public float MoveSpeed;
    public float SprintSpeed;
    public float JumpPower;


    public float baseMaxHp;
    public float baseDamage;
    public float baseDefense;
    public float bonusmaxHp { get; protected set; }
    public float bonusDamage { get; protected set; }
    public float bonusDefense { get; protected set; }

    
    public int[] maxExp;
    //PlayerEvent
    public event Action<float> OnHpChanged;  // hp 변경
    public event Action<int, int> OnExpChanged;   // 경험치 적용
    public event Action OnStatsChanged;  // 스탯 변경사항 적용
    public event Action<DamageInfo> OnDamaged;
    public event Action OnLevelUp;  //레벨업시


    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
        LoadPlayerData(DataManager.Instance.playerData);
    }

    //게임 데이터 불러오기
    public void LoadPlayerData(PlayerData data)
    {
        ID = data.id;
        Nickname = data.nickname;
        gameObject.name = Nickname;

        maxHp = data.maxHp;
        currentHp = data.currentHp;
        damage = data.damage;
        maxPosture = data.defense;

        Level = data.level;
        Exp = data.exp;
        Gold = data.gold;

        ApplyStatChanges();

        OnHpChanged?.Invoke(currentHp);
        OnExpChanged?.Invoke(Exp, Level);
        OnStatsChanged?.Invoke();

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
        dataToSave.maxHp = maxHp;
        dataToSave.currentHp = currentHp;
        dataToSave.damage = damage;
        dataToSave.defense = maxPosture;
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
        bool isGuarding = player.StateMachine.CurrentState is PlayerGuardState;
        bool isParrying = false;

        if (isGuarding)
        {
            PlayerGuardState guardState = player.StateMachine.CurrentState as PlayerGuardState;
            if (guardState != null && guardState.IsParryWindowActive())
            {
                isParrying = true;
            }
        }

        // --- 데미지 계산 ---
        float finalDamageToHp = damageInfo.finalDamage;

        if (isParrying)
        {
            finalDamageToHp = 0;
            Debug.Log("패링 성공!");
            // TODO: 패링 성공 관련 이벤트(OnParrySuccess)를 별도로 발생시켜 PlayerCombat이 반격 등을 처리하게 할 수 있음
        }
        else if (isGuarding)
        {
            finalDamageToHp = 0;
            TakePostureDamage(damageInfo.finalDamage);
            Debug.Log("가드 성공!");
            // TODO: 가드 게이지 감소 로직
        }
        else
        {
            // 가드/패링 실패 시에만 체력 감소
            base.OnDamage(damageInfo);
            TakePostureDamage(damageInfo.finalDamage * 0.5f);
        }


        OnHpChanged?.Invoke(currentHp);

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



    //스텟 증가
    public void UpAbility(PlayerStatType statToUpgrade)
    {
        if (AbilityPoint <= 0) { return; }
        AbilityPoint--;
        switch (statToUpgrade)
        {
            case PlayerStatType.Damage:
                bonusDamage++;
                break;
            case PlayerStatType.Defense:
                bonusDefense++;
                break;
            case PlayerStatType.Health:
                bonusmaxHp++;
                break;
        }

        OnStatsChanged?.Invoke();
    }


    public void ApplyStatChanges() { }
    private void OnDestroy()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnSave -= OnSavePlayerData;
        }
    }

}
