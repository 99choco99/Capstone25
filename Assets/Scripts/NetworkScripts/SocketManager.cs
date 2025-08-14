using Firesplash.GameDevAssets.SocketIO;
using Newtonsoft.Json;
using SocketIOClient;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
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

    public LoginData loginData;

    private const string BASE_API_URL = "http://localhost:3000";

    public event Action<RegisterSuccessResponse> OnItemRegisterSuccess;
    public event Action<string> OnItemRegisterFailed;
    public event Action<string> OnBuyItemFailed;
    public event Action<BuyItemSuccessResponse> OnBuyItemSuccess;

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
        LoginData testData = new LoginData { user_id = "editor_user_id", nickname = "에디터_테스터" };
        Debug.Log("에디터로 실행");
        ReceiveLoginData(JsonConvert.SerializeObject(testData));
#endif

    }


    public void ReceiveLoginData(string loginJson)
    {
        Debug.Log("웹으로부터 로그인 데이터 수신: " + loginJson);

        //JSON 문자열을 파싱
        loginData = JsonConvert.DeserializeObject<LoginData>(loginJson);

        StartCoroutine(LoadPlayerDataCoroutine(loginData.user_id));
    }

    IEnumerator LoadPlayerDataCoroutine(string userId)
    {
        string url = $"{BASE_API_URL}/playerData/{userId}";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    PlayerData data = JsonConvert.DeserializeObject<PlayerData>(webRequest.downloadHandler.text);
                    Debug.Log("플레이어 데이터 로드 성공: " + data.id);
                    DataManager.Instance.LoadPlayerData(data);

                    // 로그인 성공 후 메인 씬으로 이동
                    InvokeRepeating("AutoSaveData", 5f, 5f);
                    LoadingScene.LoadScene("Main");
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError($"플레이어 데이터 로드 실패: {webRequest.error}");
            }
        }
    }

    IEnumerator SavePlayerDataCourotine(PlayerData data)
    {
        string json = JsonConvert.SerializeObject(data);

        string url = $"{BASE_API_URL}/playerData/{loginData.user_id}";

        using(UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("데이터 저장 성공!");
            }
            else
            {
                Debug.LogError("데이터 저장 실패: " + webRequest.error);
            }
        }

    }


    IEnumerator RequestToSell(string itemId, string price, string count)
    {
        yield return null;
    }

    private void AutoSaveData()
    {
        DataManager.Instance.SavePlayerData();
        StartCoroutine(SavePlayerDataCourotine(DataManager.Instance.playerData));
        Debug.Log("Client : 데이터 저장 완료");
    }

    private void SetupSocketEvents()
    {
        socket.Instance.On("connect", (string payload) =>
        {
            socket.Instance.Emit("login", loginData.user_id, true);
            Debug.Log("소켓 연결 완료!");
        });

        // 서버로부터 로그인 성공 응답을 받을 때의 이벤트 핸들러 추가
        socket.Instance.On("loginSuccess", _ =>
        {
            LoadingScene.LoadScene("Main");
            Debug.Log("게임 시작");
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
            RegisterSuccessResponse json = JsonConvert.DeserializeObject<RegisterSuccessResponse>(response);
            OnItemRegisterSuccess?.Invoke(json);
        });

        socket.Instance.On("registerItemFailed", response =>
        {
            OnItemRegisterFailed?.Invoke(response);
        });

        socket.Instance.On("buyItemFailed", response =>
        {
            OnBuyItemFailed?.Invoke(response);
        });

        socket.Instance.On("buyItemSuccess", response =>
        {
            BuyItemSuccessResponse json = JsonConvert.DeserializeObject<BuyItemSuccessResponse>(response);
            OnBuyItemSuccess?.Invoke(json);
        });
    }

    public void RequestToBuyItem(string marketId, string count)
    {
        var itemData = new { loginData.user_id, marketId, count };
        string json = JsonConvert.SerializeObject(itemData);

        socket.Instance.Emit("RequestToBuyItem",json,false);
    }

    public void RequestToSellItem(string itemId, string price ,string count)
    {
        var itemData = new { loginData.user_id, itemId, price, count };
        string json = JsonConvert.SerializeObject(itemData);

        StartCoroutine(RequestToSell(itemId, price, count));
    }


    // 애플리케이션 종료 시 소켓 연결을 끊기
    private void OnApplicationQuit()
    {

    }


    public class LoginData
    {
        public string user_id;
        public string nickname;
    }

    public class RegisterSuccessResponse
    {
        public string message { get; set; }
        public string marketId { get; set; }
    }

    public class BuyItemSuccessResponse
    {
        public string message { get; set; }
        public int purchasedItemCount { get; set; }
        public int gold {  get; set; }
    }
}