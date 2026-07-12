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
    private void BindCameraToPlayer(Player player)
    {
        if (cam.Follow == null) { cam.Follow = player.transform; }
        if (cam.LookAt == null) { cam.LookAt = player.transform; }
    }
}
