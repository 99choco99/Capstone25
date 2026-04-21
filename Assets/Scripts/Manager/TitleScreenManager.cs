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

        //API 연결 시도
        if (NetworkManager.instance != null)
        {
            SetStatus("서버에 연결 중 입니다...");
            //서버 연결 시도
            var loginResponse = await NetworkManager.instance.API.Login.RequestLogin(userId);

            if (!loginResponse.success)
            {
                SetStatus($"로그인 실패: {loginResponse.message}");
                return;
            }


        }
        else
        {
            SetStatus("NetworkManager instance not found!");
            return;
        }

        //유저정보 세팅
        SetStatus("데이터를 불러오는 중...");
        NetworkManager.instance.API.SetUserId(userId);

        var playerData = await NetworkManager.instance.API.PlayerData.LoadPlayerData();
        var questData = await NetworkManager.instance.API.Quest.GetQuestData();
        var inventoryData = await NetworkManager.instance.API.Inventory.GetInventoryItem();
        var dialogueData = await NetworkManager.instance.API.Dialogue.GetDialogueData();

        if(playerData == null)
        {
            SetStatus("플레이어 정보를 가져오는데 실패했습니다.");
            return;
        }

        //소켓 연결
        SetStatus("서버에 연결 중...");
        NetworkManager.instance.socket.ConnectToServer(userId);

        DataManager.Instance.Server_PlayerData = playerData;
        DataManager.Instance.Server_QuestData = questData;
        DataManager.Instance.Server_DialogueData = dialogueData;
        DataManager.Instance.Server_InventoryData = inventoryData;


        GameManager.instance.GameStart(playerData);
    }


    //메세지 표시
    void SetStatus(string message)
    {
        StatusText.text = message;
        ErrorPopupObj.SetActive(true);
        StartButton.interactable = false;
    }


}
