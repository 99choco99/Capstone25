using Newtonsoft.Json;
using SocketIOClient;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSetting : LivingEntity
{
    PlayerController player;
    public string id;
    public string nickname;
    PlayerData playerData;

    public PlayerUIManager playerUI; //플레이어 UI
    public Animator anim;
    public bool Ishit; // 데미지를 입었는가?
    public event Action OnStatsChanged;  // 스탯 변경사항 적용

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
            DataManager.Instance.OnLoadPlayerData += OnLoadPlayerData;
        }
        SocketManager.Instance.socket.Instance.Emit("requestPlayerData", SocketManager.Instance.loginData.user_id, true);
    }

    //게임 데이터 불러오기
    public void OnLoadPlayerData(PlayerData data)
    {
        id = data.user_id;
        nickname = data.nickname;
        gameObject.name = nickname;

        maxHp = data.maxHp;
        currentHp = data.currentHp;
        damage = data.damage;
        defense = data.defense;
        speed = data.speed;

        level = data.level;
        exp = data.exp;

        playerData = data;
        Debug.Log("데이터 불러오기 성공");
    }

    //게임 데이터 저장하기
    public void OnSavePlayerData()
    {
        playerData.maxHp = maxHp;
        playerData.currentHp = currentHp;
        playerData.damage = damage;
        playerData.defense = defense;
        playerData.speed = speed;

        playerData.level = level;
        playerData.exp = exp;

        string json = JsonConvert.SerializeObject(playerData);
        SocketManager.Instance.socket.Instance.Emit("SavePlayerData", json, false);
        Debug.Log("데이터 저장 완료");
    }

    //데미지를 입었을 때
    public override void OnDamage(Attack currentPattern, int currentAnimationIndex, Vector3 hitDirection)
    {
        Ishit = true;
        this.hitDirection = hitDirection;
        player.playerBehaviour.KnockBackInit(currentPattern.knockbackPower[currentAnimationIndex], currentPattern.knockbackDuration);

        if (!currentPattern.canGuard)
        {
            anim.SetBool("AirBornState", true);
            anim.SetTrigger("AirBorne");
        }
        if (player.guard)
        {
            player.currentState = PlayerState.Guard;
        }
        else
        {
            player.currentState = PlayerState.Damaged;
            //정면을 맞았을 때
            if (Vector3.Dot(hitDirection, transform.forward) < 0)
            {
                anim.SetTrigger("Hit");
                anim.SetFloat("hitDirX", Vector3.Dot(hitDirection, transform.right)); // 맞은 방향의 좌우를 구분
            }
            else //뒤로 맞았을 때
            {
                anim.SetTrigger("BackHit");
            }
        }

    }


    public void LevelUp()
    {
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
            DataManager.Instance.OnLoadPlayerData -= OnLoadPlayerData;
        }
    }
}
