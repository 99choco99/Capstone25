using UnityEngine;
using Unity.Netcode;

public class LocalPlayerCamera : MonoBehaviour
{
    public Camera playerCamera; // Inspector 창에서 카메라 컴포넌트를 드래그 앤 드롭할 변수
    Player player;

    void Start()
    {
        if (playerCamera == null || player == null)
        {
            player = Legacy_PlayerCamera.Instance.player;
            playerCamera = GetComponentInChildren<Camera>();
        }
        else
        {
            if (!player.IsLocalPlayer)
            {
                playerCamera.enabled = false;
            }
            else
            {
                playerCamera.enabled = true;
            }
        }

    }
}