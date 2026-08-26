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


    //��ư ������ �� ���� ����
    public async void OnConnectButtonClick()
    {
        string userId = TEST_STEAM_ID;
        LoadLocalStaticData();


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

        DataManager.Instance.Server_PlayerData = playerData;
        DataManager.Instance.Server_InventoryData = inventoryData;

        NetworkManager.Instance.JoinRoom(playerData, playerData.currentSceneName);
    }

    private void LoadLocalStaticData()
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
    }


    //�޼��� ǥ��
    void SetStatus(string message)
    {
        StatusText.text = message;
        ErrorPopupObj.SetActive(true);
        StartButton.interactable = false;
    }


}


