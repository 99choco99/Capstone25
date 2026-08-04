using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XInput;

public class CameraInputController : MonoBehaviour
{
    private CinemachineInputAxisController inputController;
    void Awake()
    {
        inputController = GetComponent<CinemachineInputAxisController>();
    }

    private void Start()
    {
        Player.OnLocalPlayerSpawned += ConnectToPlayerInput;
    }

    public void ConnectToPlayerInput(Player localPlayer)
    {
        if (localPlayer != null && localPlayer.InputHandler != null)
        {
            localPlayer.InputHandler.OnCursorStateChanged += HandleCursorState;
        }
    }

    private void HandleCursorState(bool isUIOpen)
    {
        if (inputController != null) {

            inputController.enabled = !isUIOpen;
        }
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= ConnectToPlayerInput;
    }
}
