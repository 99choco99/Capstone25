using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioListener))]
public class LocalPlayerAudioListener : MonoBehaviour
{
    private AudioListener _audioListener;
    private Player player;

    void Start()
    {
        _audioListener = GetComponent<AudioListener>();
        player = Legacy_PlayerCamera.Instance.player;
        if (_audioListener == null)
        {
            _audioListener = GetComponentInChildren<AudioListener>();
        }
        else
        {
            if (!player.IsLocalPlayer)
            {
                _audioListener.enabled = false;
            }
            else
            {
                _audioListener.enabled = true;
            }
        }
    }
}