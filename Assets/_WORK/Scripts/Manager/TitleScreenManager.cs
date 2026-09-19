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
            if (!LoadLocalStaticData())
            {
                StartButton.interactable = true;
                return;
            }

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


    //��ư ������ �� ���� ����
    public async void OnConnectButtonClick()
    {
        string userId = TEST_STEAM_ID;
        if (!LoadLocalStaticData()) return;


        //API ���� �õ�
        if (NetworkManager.Instance != null)
        {
            SetStatus("������ ���� �� �Դϴ�...");
            //���� ���� �õ�
            var loginResponse = await NetworkManager.Instance.API.Login.RequestLogin(userId);

            if (!loginResponse.success)
            {
                SetStatus($"�α��� ����: {loginResponse.message}");
                return;
            }


        }
        else
        {
            SetStatus("NetworkManager Instance not found!");
            return;
        }

        //�������� ����
        SetStatus("�����͸� �ҷ����� ��...");
        NetworkManager.Instance.API.SetUserId(userId);

        var playerData = await NetworkManager.Instance.API.PlayerData.LoadPlayerData();
        var inventoryData = await NetworkManager.Instance.API.Inventory.GetInventoryItem();

        if(playerData == null)
        {
            SetStatus("�÷��̾� ������ �������µ� �����߽��ϴ�.");
            return;
        }

        //���� ����
        SetStatus("������ ���� ��...");
        NetworkManager.Instance.socket.ConnectToServer(userId);

        DataManager.Instance.PlayerData = playerData;
        DataManager.Instance.InventoryData = inventoryData;

        NetworkManager.Instance.JoinRoom(playerData, playerData.currentSceneName);
    }

    private bool LoadLocalStaticData()
    {
        // Assets/Resources/Data/itemData.json
        TextAsset itemJson = Resources.Load<TextAsset>("Data/itemData");
        if (itemJson != null)
        {
            ItemManager.Init(itemJson.text);
            Debug.Log("[����] ���� ������ �ε� �Ϸ�!");
        }
        else
        {
            Debug.LogError("[����] ���� ������ �ε� ����!");
            SetStatus("���� ���Ͽ� �ջ��� �ֽ��ϴ�.");
        }
        return itemJson != null;
    }


    //�޼��� ǥ��
    void SetStatus(string message)
    {
        StatusText.text = message;
        ErrorPopupObj.SetActive(true);
        StartButton.interactable = false;
    }


}


