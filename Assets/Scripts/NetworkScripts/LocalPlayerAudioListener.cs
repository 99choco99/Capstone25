using UnityEngine;
using Unity.Netcode;

public class LocalPlayerAudioListener : MonoBehaviour
{
    private AudioListener _audioListener;
    private Player player;

    void Start()
    {
        _audioListener = GetComponent<AudioListener>();
        player = GetComponentInParent<Player>();
        if (_audioListener == null)
        {
            // 카메라에 Audio Listener가 없을 경우, 플레이어 오브젝트 자체에서 찾습니다.
            _audioListener = GetComponentInChildren<AudioListener>();
            if (_audioListener == null)
            {
                Debug.LogError("플레이어 프리팹 또는 자식 오브젝트에서 Audio Listener를 찾을 수 없습니다.");
                return;
            }
        }

        // 이 NetworkObject가 로컬 플레이어의 소유인지 확인합니다.
        if (!player.IsLocalPlayer)
        {
            // 로컬 플레이어의 소유가 아니면 Audio Listener를 비활성화합니다.
            _audioListener.enabled = false;
        }
        else
        {
            // 로컬 플레이어의 소유이면 Audio Listener를 활성화합니다.
            _audioListener.enabled = true;
        }
    }
}