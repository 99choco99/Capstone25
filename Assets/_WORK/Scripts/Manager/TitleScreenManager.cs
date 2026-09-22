using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SoundEffectManager;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] Button StartButton;
    [SerializeField] GameObject ErrorPopupObj;
    [SerializeField] TextMeshProUGUI StatusText;



    private readonly string TEST_STEAM_ID = "DEV_TEST_USER_001";

    /// <summary>서버에 접속하지 않고 로컬 저장 데이터로 게임을 시작합니다.</summary>
    public async void OnStartButtonClick()
    {
        if (!StartButton.interactable) return;
        StartButton.interactable = false;

        try
        {
            await GameManager.Instance.StartGame();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            SetStatus($"게임 시작 실패: {exception.Message}");
            StartButton.interactable = true;
        }
    }


    private void Start()
    {
        SoundManager.Instance.PlayBGM("BGM_Login", 0f);
    }


    //버튼 눌렀을 때 게임 시작
    public async void OnConnectButtonClick()
    {
        string userId = TEST_STEAM_ID;


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

        if(playerData == null)
        {
            SetStatus("플레이어 정보를 가져오는데 실패했습니다.");
            return;
        }

        //소켓 연결
        SetStatus("서버에 연결 중...");
        NetworkManager.Instance.socket.ConnectToServer(userId);

        DataManager.Instance.PlayerData = playerData;

        NetworkManager.Instance.JoinRoom(playerData, playerData.currentSceneName);
    }


    //메세지 표시
    void SetStatus(string message)
    {
        StatusText.text = message;
        ErrorPopupObj.SetActive(true);
        StartButton.interactable = false;
    }


}


