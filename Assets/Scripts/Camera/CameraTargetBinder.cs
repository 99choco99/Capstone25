using Unity.Cinemachine;
using UnityEngine;

[RequireComponent (typeof(CinemachineCamera))]
public class CameraTargetBinder : MonoBehaviour
{
    private CinemachineCamera cam;

    void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
    }
    private void OnEnable()
    {
        Player.OnLocalPlayerSpawned += BindCameraToPlayer;
    }
    private void OnDisable()
    {
        Player.OnLocalPlayerSpawned -= BindCameraToPlayer;
    }
    private void BindCameraToPlayer(Transform playerTransform)
    {
        cam.Follow = playerTransform;
        cam.LookAt = playerTransform;
    }
}
