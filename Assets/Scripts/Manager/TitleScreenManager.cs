using UnityEngine;
using Unity.Netcode;

public class TitleScreenManager : MonoBehaviour
{
    public void OnConnectButtonClick()
    {
        Debug.Log("Connect button clicked. Attempting to connect...");


        if (SocketManager.instance != null)
        {
            SocketManager.instance.ConnectToServer(PublicAPIManager.Instance.loginData.user_id);
            GameManager.instance.ChangeState(GameState.Gameplay);
        }
        else
        {
            Debug.LogError("SocketManager instance not found!");
        }
    }
}
