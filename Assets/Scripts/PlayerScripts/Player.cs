using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using SocketIOClient;
using System;

public class Player : LivingEntity
{
    public PlayerUI playerUI;
    public PlayerDataClass setData;
    public PlayerDataClass getData;
    public string data;
    public bool Ishit; // 데미지를 입었는가?


    [Header("SocketIO Setting")]
    private SocketIOUnity socket;
    private string serverUrl = "http://localhost:3000";


    private async void Awake()
    {
        try
        {
            var uri = new Uri(serverUrl);
            socket = new SocketIOUnity(uri, new SocketIOOptions()
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });

            socket.OnConnected += async (sender, e) =>
            {
                Debug.Log("Socket connected!");
                socket.Emit("getPlayer");

                // 연결 완료 후 playerData 이벤트 핸들러 등록
                socket.On("playerData", (response) => {
                    try
                    {
                        getData = response.GetValue<PlayerDataClass>(); //JSON 받기
                        if(getData != null)
                        {
                            Debug.Log(getData.Damage);
                            Debug.Log(damage);
                        }
                        else
                        {
                            Debug.Log("실패");
                        }
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
        }
        catch (Exception ex)
        {
            Debug.LogError("Socket Connection error: " + ex.Message);
        }
    }


    private void Start()
    {
        //OnEnable();
    }

    //데미지를 입었을 때
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        base.OnDamage(damage, hitPoint, hitDirection);
        socket.Emit("Damaged", JsonConvert.SerializeObject(getData));
        playerUI.PlayerHpUI.value = currentHp;
        Ishit = true;
    }

    protected override void OnEnable()
    {
        //maxHp = getData.maxHp;
        //damage = getData.Damage;
        //base.OnEnable();
    }

    public class PlayerDataClass
    {
        public float maxHp{ get; set; }
        public float currentHp { get; set; }
        public float Damage { get; set; }
    }
}
