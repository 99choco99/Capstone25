using Firesplash.GameDevAssets.SocketIO;
using Newtonsoft.Json;
using SocketIOClient;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using static Firesplash.GameDevAssets.SocketIO.SocketIOInstance;
using static SocketManager;

public class SocketManager : MonoBehaviour
{
#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void MySignalReady();
#endif

    public static SocketManager Instance { get; private set; }
    public SocketIOCommunicator socket;

    LoginData loginData;



    public event Action<string> OnItemRegisterSuccess;
    public event Action<string> OnItemRegisterFailed;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        socket = GetComponent<SocketIOCommunicator>();
    }

    public void Start()
    {
#if UNITY_WEBGL
        //MySignalReady();
        Debug.Log("Unity -> 웹: 준비 완료 신호 보냄");
#endif


#if UNITY_EDITOR
        LoginData testData = new LoginData { id = "editor_user_id", nickname = "에디터_테스터" };
        ReceiveLoginData(JsonUtility.ToJson(testData));
#endif
        SetupSocketEvents();
    }


    public void ReceiveLoginData(string loginJson)
    {
        Debug.Log("웹으로부터 로그인 데이터 수신: " + loginJson);

        // JSON 문자열을 파싱
        //DataManager.Instance.loginData = JsonConvert.DeserializeObject<LoginData>(loginJson);
    }



    private void SetupSocketEvents()
    {
        socket.Instance.On("connect", (string payload) =>
        {
            //string json = JsonConvert.SerializeObject(DataManager.Instance.loginData);
            //socket.Instance.Emit("login", json,false);
            LoadingScene.LoadScene("Main");
            Debug.Log("소켓 연결 완료!");
        });

        socket.Instance.On("disconnect", (string payload) =>
        {
            DataManager.Instance.SavePlayerData();
            Debug.Log("소켓 연결 끊김!");
        });

        // "playerData" 이벤트 리스너
        socket.Instance.On("loadPlayerData", response => {
            try
            {
                PlayerData data = JsonConvert.DeserializeObject<PlayerData>(response);

                Debug.Log("수신된 플레이어 이름: " + data.id);

                if(DataManager.Instance != null)
                {
                    DataManager.Instance.LoadPlayerData(data);
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError("JSON 역직렬화 오류: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogError("일반 오류: " + ex.Message);
            }
        });


        // "UpdateData" 이벤트 리스너
        socket.Instance.On("UpdateData", response => {
            // ... 데이터 업데이트 로직
        });

        // "Die" 이벤트 리스너
        socket.Instance.On("Die", _ => {
            // ... 플레이어 사망 로직
        });

        socket.Instance.On("registerItemSuccess", response =>
        {
            string successMessage = response;
            OnItemRegisterSuccess?.Invoke(successMessage);
        });

        socket.Instance.On("registerItemFailed", response =>
        {
            string errorMessage = response;
            OnItemRegisterFailed?.Invoke(errorMessage);
        });
    }

    public void RequestToBuyItem(string itemId, string count)
    {
        var itemData = new { loginData.id, itemId, count };
        string json = JsonConvert.SerializeObject(itemData);

        socket.Instance.Emit("RequestToBuyItem",json,false);
    }

    public void RequestToSellItem(string itemId, string price ,string count)
    {
        var itemData = new { loginData.id, itemId, price, count };
        string json = JsonConvert.SerializeObject(itemData);

        socket.Instance.Emit("RequestToSellItem", json, false);
    }


    // 애플리케이션 종료 시 소켓 연결을 끊기
    private void OnApplicationQuit()
    {
        if (socket != null && socket.Instance.IsConnected())
        {
            socket.Instance.Close();
            socket.Instance.Emit("Disconnet");
        }
    }


    public class LoginData
    {
        public string id;
        public string nickname;
    }
}