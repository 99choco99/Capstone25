
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using UnityEngine;

public class PublicAPIManager : MonoBehaviour
{
#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void MySignalReady();
#endif
    
    public static PublicAPIManager Instance { get; private set; }


    public LoginData loginData;
    public DialogueAPI Dialogue;
    public MarketAPI Market;
    public PlayerDataAPI PlayerData;



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
        MySignalReady();
#endif


#if UNITY_EDITOR
        //LoginData testData = new LoginData { user_id = "editor_user_id2", nickname = "에디터_테스터2" };
        //Debug.Log("에디터로 실행");
        //ReceiveLoginData(JsonConvert.SerializeObject(testData));
#endif


    }


    //로그인 데이터 받기
    public void ReceiveLoginData(string loginJson)
    {
        Debug.Log("웹으로부터 로그인 데이터 수신: " + loginJson);
        
        //JSON 문자열을 파싱
        loginData = JsonConvert.DeserializeObject<LoginData>(loginJson);

        Market = new MarketAPI(this);
        Dialogue = new DialogueAPI(this);
        PlayerData = new PlayerDataAPI(this);

        RequestPlayerData();
    }

    public void RequestPlayerData()
    {
        PlayerData.RequestLoadPlayerData(loginData.user_id);
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




}