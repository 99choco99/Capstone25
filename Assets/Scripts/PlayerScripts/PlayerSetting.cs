using Newtonsoft.Json;
using UnityEngine;
using SocketIOClient;
using System;

public class PlayerSetting : LivingEntity
{
    PlayerController player;
    public PlayerUIManager playerUI; //플레이어 UI
    public Animator anim;
    public event Action OnStatsChanged;  // 스탯 변경사항 적용
    public bool Ishit; // 데미지를 입었는가?

    private void Awake()
    {
        anim = GetComponent<Animator>();
        player = GetComponent<PlayerController>();
        playerUI = GetComponentInChildren<PlayerUIManager>();
    }


    //플레이어 체력 변화 적용
    //private void LateUpdate()
    //{
    //    playerUI.PlayerHpUI.value = (float)(currentHp / maxHp);
    //}

    //데미지를 입었을 때
    public override void OnDamage(Attack currentPattern, int currentAnimationIndex, Vector3 hitDirection)
    {
        Ishit = true;
        this.hitDirection = hitDirection;
        player.playerBehaviour.KnockBackInit(currentPattern.knockbackPower[currentAnimationIndex]);

        if (currentPattern.isheavyAttack)
        {
            player.anim.SetBool("AirBornState",true);
            player.anim.SetTrigger("AirBorne");
        }
        //정면을 맞았을 때
        if (player.playerSetting.Ishit && Vector3.Dot(hitDirection, player.transform.forward) > 0.1)
        {
            anim.SetTrigger("Hit");
            anim.SetFloat("hitDirX", Vector3.Dot(hitDirection, player.transform.right)); // 맞은 방향의 좌우를 구분
        }
        else //뒤로 맞았을 때
        {
            anim.SetTrigger("BackHit");
        }
        player.currentState = PlayerState.Damaged;
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

    //불러온 데이터 적용하기
    private void LoadData(PlayerDataClass getData)
    {
        maxHp = getData.maxHp;
        currentHp = getData.currentHp;
        damage = getData.damage;
    }
    

    //보내고 받을 데이터 형식
    public class PlayerDataClass
    {
        public float maxHp{ get; set; }
        public float currentHp { get; set; }
        public float damage { get; set; }
        public float exp { get; set; }
        public int level { get; set; }
        public bool dead { get; set; }
    }

    public void Test()
    {
        Debug.Log("Animation Event 작동함");
    }
}
