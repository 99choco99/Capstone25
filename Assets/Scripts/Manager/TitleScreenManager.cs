using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] Button StartButton;

    private void Start()
    {
        SoundManager.Instance.PlayBGM("BGM_Login");
    }


    public void OnConnectButtonClick()
    {
        Debug.Log("Connect button clicked. Attempting to connect...");


        if (SocketManager.instance != null)
        {
            StartButton.interactable = false;
            SocketManager.instance.ConnectToServer(PublicAPIManager.Instance.loginData.user_id);
            GameManager.instance.ChangeState(GameState.Gameplay);
        }
        else
        {
            Debug.LogError("SocketManager instance not found!");
        }
    }
}
