
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class SocketManager : MonoBehaviour
{
#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void MySignalReady();
#endif

    public static SocketManager Instance { get; private set; }

    public LoginData loginData;

    private const string BASE_API_URL = "http://localhost:3000";

    public event Action<ItemRegistResponse> OnItemRegisterSuccess;
    public event Action<string> OnItemRegisterFailed;
    public event Action<BuyItemResponse> OnBuyItemSuccess;
    public event Action<string> OnBuyItemFailed;
    public event Action<GetSellingListResponse> OnGetSellingListSuccess;  //아이템 판매 목록 가져오기 성공 이벤트
    public event Action<GetSellingListResponse> OnGetMySellingListSuccess; //내 판매 목록 가져오기 성공 이벤트

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
    }

    public void Start()
    {
#if UNITY_WEBGL
        //MySignalReady();
        //Debug.Log("Unity -> 웹: 준비 완료 신호 보냄");
#endif


#if UNITY_EDITOR
        LoginData testData = new LoginData { user_id = "editor_user_id2", nickname = "에디터_테스터" };
        Debug.Log("에디터로 실행");
        ReceiveLoginData(JsonConvert.SerializeObject(testData));
#endif

    }


    //로그인 데이터 받기
    public void ReceiveLoginData(string loginJson)
    {
        Debug.Log("웹으로부터 로그인 데이터 수신: " + loginJson);

        //JSON 문자열을 파싱
        loginData = JsonConvert.DeserializeObject<LoginData>(loginJson);

        StartCoroutine(LoadPlayerDataCoroutine(loginData.user_id));
    }

    // 플레이어 데이터 요청
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
                    InvokeRepeating("AutoSaveData", 10f, 10f);
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

    //플레이어 데이터 저장
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
                Debug.Log($"Client {loginData.user_id} : 데이터 저장 완료");
            }
            else
            {
                Debug.LogError("데이터 저장 실패: " + webRequest.error);
            }
        }

    }


    //판매 목록 가져오기
    IEnumerator GetSellingList()
    {
        string url = $"{BASE_API_URL}/market/items";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
            
            if(webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GetSellingListResponse[] responseList = JsonConvert.DeserializeObject<GetSellingListResponse[]>(webRequest.downloadHandler.text);
                    foreach (var response in responseList)
                    {
                        OnGetSellingListSuccess?.Invoke(response);
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError(webRequest.downloadHandler);
            }
        }
    }


    IEnumerator GetSellingList(string id)
    {
        string url = $"{BASE_API_URL}/market/items/{id}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if(webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GetSellingListResponse[] responseList = JsonConvert.DeserializeObject<GetSellingListResponse[]>(webRequest.downloadHandler.text);
                    foreach (var response in responseList)
                    {
                        Debug.Log($"{response}");
                        OnGetMySellingListSuccess?.Invoke(response);
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류: " + ex.Message);
                }
            }
        }
    }

    //판매 요청
    IEnumerator RequestToSell(int ItemId, ItemSpec data, string price, string count)
    {
        var itemData = new
        {
            userId = loginData.user_id,
            ItemId = ItemId,
            ItemData = data,
            price = price,
            itemCount = count
        };
        string json = JsonConvert.SerializeObject(itemData);


        string url = $"{BASE_API_URL}/market/items";  // 아이템 경매 등록 url설정해야됨.

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("서버 응답 성공!");
                string responseJson = webRequest.downloadHandler.text;
                try
                {
                    ItemRegistResponse response = JsonConvert.DeserializeObject<ItemRegistResponse>(responseJson);

                    if (response.success)
                    {
                        OnItemRegisterSuccess?.Invoke(response);
                    }
                    else
                    {
                        OnItemRegisterFailed?.Invoke(response.message);
                    }
                }
                catch { }
            }
            else
            {
                Debug.LogError("아이템 등록 실패: " + webRequest.error);
            }
        }

    }

    // 구매 요청
    IEnumerator RequestToBuy(int marketId, string count)
    {
        string url = $"{BASE_API_URL}/market/buy?userId={loginData.user_id}&marketId={marketId}&count={count}";

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "GET"))
        {
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return webRequest.SendWebRequest();

            if(webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("서버 응답 성공!");
                string responseJson = webRequest.downloadHandler.text;
                try
                {
                    BuyItemResponse response = JsonConvert.DeserializeObject<BuyItemResponse>(responseJson);

                    if (response.success)
                    {
                        OnBuyItemSuccess?.Invoke(response);
                    }
                    else
                    {
                        OnBuyItemFailed?.Invoke(response.message);
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError("JSON 역직렬화 오류" + ex.Message);
                }
            }
            else
            {
                Debug.LogError("서버 통신 장애");
            }
        }
    }

    //플레이어 데이터 자동 저장
    private void AutoSaveData()
    {
        DataManager.Instance.SavePlayerData();
        StartCoroutine(SavePlayerDataCourotine(DataManager.Instance.playerData));

    }

    //아이템 목록 가져오기 요청
    public void RequestToGetSellingList()
    {
        StartCoroutine(GetSellingList());
    }
    public void RequestToGetMyList()
    {
        StartCoroutine(GetSellingList(loginData.user_id));
    }

    //아이템 구매 요청
    public void RequestToBuyItem(int marketId, string count)
    {
        StartCoroutine(RequestToBuy(marketId, count));
    }

    //아이템 판매 요청
    public void RequestToSellItem(int Itemid, ItemSpec itemspec, string price ,string count)
    {
        StartCoroutine(RequestToSell(Itemid,itemspec, price, count));
    }


    // 애플리케이션 종료 시 소켓 연결을 끊기
    private void OnApplicationQuit()
    {
        CancelInvoke("AutoSaveData");
    }


    public class LoginData
    {
        public string user_id;
        public string nickname;
    }


    //판매목록 가져오기 응답
    public class GetSellingListResponse
    {
        public int marketId;
        public int ItemId;
        public int ItemCount;
        public int price;
    }

    public class ItemRegistResponse
    {
        public bool success;  //등록 성공 여부
        public string message { get; set; }  // 성공 or 실패 메세지
        public int marketId { get; set; }  //마켓 id
        public int ItemCount { get; set; }  // 등록된 아이템 개수
        public int price { get; set; }   //등록한 가격
    }

    public class BuyItemResponse
    {
        public bool success;
        public string message { get; set; }
        public int marketId {  get; set; }
        public int ItemId {  get; set; }
        public ItemSpec spec { get; set; }
        public int purchasedItemCount { get; set; }
        public int remainingItemCount { get; set; }
        public int gold {  get; set; }
    }
}