using Unity.Cinemachine;
using UnityEngine;

[RequireComponent (typeof(CinemachineCamera))]
public class Camear : MonoBehaviour
{
    public static Camear Instance;

    private CinemachineCamera cam;

    private void Awake()
    {
        Instance = this;
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
