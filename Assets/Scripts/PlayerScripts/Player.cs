using Newtonsoft.Json;
using UnityEngine;
using SocketIOClient;
using System;

public class Player : LivingEntity
{
    SocketIOUnity socket;
    public PlayerUIManager playerUI; //플레이어 UI
    public PlayerDataClass setData;  // 보낼 데이터
    public PlayerDataClass getData;  // 받은 데이터
    public string data;  //
    public bool Ishit; // 데미지를 입었는가?

    async void Start()
    {
        socket = SocketManager.Instance.GetSocket();
        //소켓 연결시
        socket.OnConnected += async (sender, e) =>
        {
            Debug.Log("Socket connected!");
            // 연결 완료 후 playerData 이벤트 핸들러 등록
            socket.On("playerData", (response) => {
                try
                {
                    // 데이터 불러오기
                    getData = response.GetValue<PlayerDataClass>(); //JSON 받기
                    LoadData(getData);
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON Deserialize Error: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.LogError("General Error: " + ex.Message);
                }
            });
        };
        await socket.ConnectAsync();
        // 데이터 변화시 다시 불러오기
        socket.On("UpdateData", response =>
        {
            getData = response.GetValue<PlayerDataClass>(); //JSON 받기
            LoadData(getData);
        });
        //죽었을 경우 실행
        socket.On("Die", _ =>
        {
            Die();
        });
    }


    //플레이어 체력 변화 적용
    private void LateUpdate()
    {
        playerUI.PlayerHpUI.value = (float)(currentHp / maxHp);
    }

    //데미지를 입었을 때
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        socket.Emit("Damaged", damage);
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
        public bool dead { get; set; }
    }
}
