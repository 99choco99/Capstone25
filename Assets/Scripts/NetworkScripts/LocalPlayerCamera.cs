using UnityEngine;
using Unity.Netcode;

public class LocalPlayerCamera : MonoBehaviour
{
    public Camera playerCamera; // Inspector 창에서 카메라 컴포넌트를 드래그 앤 드롭할 변수
    Player player;

    void Start()
    {
        player = GetComponentInParent<Player>();
        // playerCamera 변수가 비어있다면 자식 오브젝트에서 Camera 컴포넌트를 찾습니다.
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("플레이어 프리팹 또는 자식 오브젝트에서 카메라를 찾을 수 없습니다.");
                return;
            }
        }

        // 이 NetworkObject가 로컬 플레이어의 소유인지 확인합니다.
        if (!player.IsLocalPlayer)
        {
            // 로컬 플레이어의 소유가 아니면 카메라를 비활성화합니다.
            playerCamera.enabled = false;
        }
        else
        {
            // 로컬 플레이어의 소유이면 카메라를 활성화합니다.
            playerCamera.enabled = true;

            // 추가적으로, 로컬 플레이어의 카메라를 제어하는 스크립트를 여기서 활성화하거나 초기화할 수 있습니다.
            // 예: GetComponent<PlayerCameraController>().enabled = true;
        }
    }
}