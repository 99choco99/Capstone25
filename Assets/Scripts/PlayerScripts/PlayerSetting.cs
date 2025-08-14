using Newtonsoft.Json;
using SocketIOClient;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSetting : LivingEntity
{
    PlayerController player;
    public PlayerUIManager playerUI; //플레이어 UI
    public Animator anim;


    public string id;
    public string nickname;
    public int gold;
    public int []maxExp;

    public float canParryTime;  // Parry로 인정해주는 최대 시간

    [Header("PlayerStatChanges")]
    public float D_health;
    public float D_speed;
    public float D_damage;
    public float D_defense;

    [Header("PlayerAttackSetting")]
    public Attack [] playerNormalAttack;
    public Attack playerHeavyAttack;
    public Attack currentAttack;
    public int currentAnimationIndex;

    //PlayerEvent
    public event Action<float> OnHpChanged;  // hp 변경
    public event Action<int, int> OnExpChanged;   // 경험치 적용
    public event Action OnStatsChanged;  // 스탯 변경사항 적용

    private void Awake()
    {
        anim = GetComponent<Animator>();
        player = GetComponent<PlayerController>();
        playerUI = GetComponentInChildren<PlayerUIManager>();
    }

    private void Start()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnSavePlayerData += OnSavePlayerData;
            LoadPlayerData(DataManager.Instance.playerData);
        }

    }

    //게임 데이터 불러오기
    public void LoadPlayerData(PlayerData data)
    {
        id = data.id;
        nickname = data.nickname;
        gameObject.name = nickname;

        maxHp = data.maxHp;
        currentHp = data.currentHp;
        damage = data.damage;
        defense = data.defense;
        speed = data.speed;

        level = data.level;
        exp = data.exp;
        gold = data.gold;
        Debug.Log("데이터 불러오기 성공");

        OnHpChanged?.Invoke(currentHp);
        OnExpChanged?.Invoke(exp,level);
        OnStatsChanged?.Invoke();
    }

    //게임 데이터 저장하기
    public void OnSavePlayerData()
    {
        DataManager.Instance.playerData.maxHp = maxHp;
        DataManager.Instance.playerData.currentHp = currentHp;
        DataManager.Instance.playerData.damage = damage;
        DataManager.Instance.playerData.defense = defense;
        DataManager.Instance.playerData.speed = speed;

        DataManager.Instance.playerData.level = level;
        DataManager.Instance.playerData.exp = exp;

        Debug.Log("데이터 저장 시도");
    }

    //데미지를 입었을 때
    public override void OnDamage(Attack currentPattern, int currentAnimationIndex, Vector3 hitDirection)
    {
        this.hitDir = hitDirection;
        player.playerBehaviour.KnockBackInit(currentPattern.knockbackPower[currentAnimationIndex], currentPattern.knockbackDuration);

        if (!currentPattern.canGuard)
        {
            anim.SetBool("AirBornState", true);
            anim.SetTrigger("AirBorne");
        }
        if (player.guard)
        {
            player.currentState = PlayerState.Guard;
            if (player.playerBehaviour.guardDuration <= canParryTime)
            {
                player.anim.SetTrigger("Parry");
            }
            player.anim.SetTrigger("GuardHit");
        }
        else
        {
            player.currentState = PlayerState.Damaged;
            //정면을 맞았을 때
            if (Vector3.Dot(hitDirection, transform.forward) < 0)
            {
                anim.SetTrigger("Hit");
                anim.SetFloat("hitDirX", Vector3.Dot(hitDirection, transform.right)); // 맞은 방향의 좌우를 구분
                currentHp -= currentPattern.damage[currentAnimationIndex];
            }
            else //뒤로 맞았을 때
            {
                anim.SetTrigger("BackHit");
                currentHp -= currentPattern.damage[currentAnimationIndex] * 1.2f;
            }
            OnHpChanged?.Invoke(currentHp);
        }

    }

    public void AddExp(int addExp)
    {
        exp += addExp;
        // 레벨업 조건 체크
        if (level < maxExp.Length && exp >= maxExp[level])
        {
            LevelUp();
        }
        OnExpChanged?.Invoke(exp, level);
    }


    public void LevelUp()
    {
        exp -= maxExp[level];
        level++;
        QuestManager.instance.UnlockQuests(level);
    }

    public void ApplyStatChanges(float damageChange, float healthChange, float defenseChange, float speedChange)
    {
        // 스탯 값 변경 로직
        D_damage += damageChange;
        D_health += healthChange;
        D_defense += defenseChange;
        D_speed += speedChange;


        damage += damageChange;
        maxHp += healthChange;
        defense += defenseChange;
        speed += speedChange;

        OnStatsChanged?.Invoke();
    }


    private void OnDestroy()
    {
        if(DataManager.Instance != null)
        {
            DataManager.Instance.OnSavePlayerData -= OnSavePlayerData;
        }
    }
}
