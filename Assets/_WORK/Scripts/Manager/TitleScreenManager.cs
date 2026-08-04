using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] Button StartButton;
    [SerializeField] GameObject ErrorPopupObj;
    [SerializeField] TextMeshProUGUI StatusText;



    private readonly string TEST_STEAM_ID = "DEV_TEST_USER_001";


    private void Start()
    {
        SoundManager.Instance.PlayBGM("BGM_Login");
    }


    //버튼 눌렀을 때 게임 시작
    public async void OnConnectButtonClick()
    {
        string userId = TEST_STEAM_ID;
        LoadLocalStaticData();


        //API 연결 시도
        if (NetworkManager.Instance != null)
        {
            SetStatus("서버에 연결 중 입니다...");
            //서버 연결 시도
            var loginResponse = await NetworkManager.Instance.API.Login.RequestLogin(userId);

            if (!loginResponse.success)
            {
                SetStatus($"로그인 실패: {loginResponse.message}");
                return;
            }


        }
        else
        {
            SetStatus("NetworkManager Instance not found!");
            return;
        }

        //유저정보 세팅
        SetStatus("데이터를 불러오는 중...");
        NetworkManager.Instance.API.SetUserId(userId);

        var playerData = await NetworkManager.Instance.API.PlayerData.LoadPlayerData();
        var questProgress = await NetworkManager.Instance.API.Quest.GetQuestData();
        var inventoryData = await NetworkManager.Instance.API.Inventory.GetInventoryItem();

        if(playerData == null)
        {
            SetStatus("플레이어 정보를 가져오는데 실패했습니다.");
            return;
        }

        //소켓 연결
        SetStatus("서버에 연결 중...");
        NetworkManager.Instance.socket.ConnectToServer(userId);

        DataManager.Instance.Server_PlayerData = playerData;
        DataManager.Instance.Server_QuestProgress = questProgress;
        DataManager.Instance.Server_InventoryData = inventoryData;

        NetworkManager.Instance.JoinRoom(playerData, playerData.currentSceneName);
    }

    private void LoadLocalStaticData()
    {
        // Assets/Resources/Data/itemData.json
        TextAsset itemJson = Resources.Load<TextAsset>("Data/itemData");
        TextAsset questJson = Resources.Load<TextAsset>("Data/questData");
        TextAsset dialogueJson = Resources.Load<TextAsset>("Data/dialogue");

        if (itemJson != null && dialogueJson != null && questJson != null)
        {
            ItemManager.Init(itemJson.text);
            DialogueManager.Init(dialogueJson.text);
            QuestManager.Init(questJson.text);
            Debug.Log("[로컬] 정적 데이터 로드 완료!");
        }
        else
        {
            Debug.LogError("[로컬] 정적 데이터 로드 실패!");
            SetStatus("로컬 파일에 손상이 있습니다.");
        }
    }


    //메세지 표시
    void SetStatus(string message)
    {
        StatusText.text = message;
        ErrorPopupObj.SetActive(true);
        StartButton.interactable = false;
    }


}
