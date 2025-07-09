using Newtonsoft.Json;
using SocketIOClient;
using System;
using UnityEngine;
using static PlayerSetting;

public class SocketManager : MonoBehaviour
{
    public PlayerDataClass setData;  // 보낼 데이터
    public PlayerDataClass getData;  // 받은 데이터

    [Header("SocketIO Setting")]
    public static SocketManager Instance { get; private set; }
    private SocketIOUnity socket;
    private string serverUrl = "http://localhost:3000";


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSocket();
        }
        else
        {
            Destroy(gameObject);
        }
    }

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
                    //LoadData(getData);
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
            //LoadData(getData);
        });
        //죽었을 경우 실행
        socket.On("Die", _ =>
        {
            //Die();
        });
    }
    async void InitializeSocket()
    {
        try
        {
            var uri = new Uri(serverUrl);
            socket = new SocketIOUnity(uri, new SocketIOOptions()
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Socket Connection error: " + ex.Message);
        }
        await socket.ConnectAsync();
    }

    public SocketIOUnity GetSocket()
    {
        return socket;
    }

}
