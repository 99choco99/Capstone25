using Newtonsoft.Json;
using UnityEngine;
using SocketIOClient;
using System;

public class PlayerData : LivingEntity
{
    public PlayerUIManager playerUI; //플레이어 UI
    public bool Ishit; // 데미지를 입었는가?

    //플레이어 체력 변화 적용
    private void LateUpdate()
    {
        playerUI.PlayerHpUI.value = (float)(currentHp / maxHp);
    }

    //데미지를 입었을 때
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        //socket.Emit("Damaged", damage);
        Ishit = true;
    }

    public void LevelUp()
    {
        level++;
        QuestManager.instance.UnlockQuests(level);
    }

    protected override void OnEnable()
    {

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
}
