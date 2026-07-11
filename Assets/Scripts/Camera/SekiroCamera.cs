using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineOrbitalFollow))]
public class SekiroCamera : MonoBehaviour
{
    [Header("Free Look")]
    [SerializeField] float freeLookSensitivity = 50f;

    [Header("Lock-On")]
    [SerializeField] AnimationCurve pitchByDistance = AnimationCurve.Linear(2f, 35f, 15f, 10f);
    [SerializeField] float pitchDamping = 0.2f;
    [SerializeField] float horizontalRecenterSpeed = 2f;

    CinemachineCamera vcam;
    CinemachineOrbitalFollow orbital;
    Player player;
    TargetingSystem targetingSystem;
    Transform aimPoint;
    float pitchVelocity;

    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        orbital = GetComponent<CinemachineOrbitalFollow>();
        aimPoint = new GameObject("SekiroCamera_AimPoint").transform;
    }

    void OnEnable() => Player.OnLocalPlayerSpawned += HandlePlayerSpawned;
    void OnDisable() => Player.OnLocalPlayerSpawned -= HandlePlayerSpawned;

    void HandlePlayerSpawned(Transform playerTransform)
    {
        var spawned = playerTransform.GetComponent<Player>();
        if (spawned == null || !spawned.IsLocalPlayer) return;

        player = spawned;
        targetingSystem = player.TargetingSystem;
        targetingSystem.OnChangedTarget += HandleLockOn;
        targetingSystem.OnTargetDeselected += HandleLockOff;

        vcam.Follow = playerTransform;
    }

    void HandleLockOn(IDamageable _) => vcam.LookAt = aimPoint;
    void HandleLockOff() => vcam.LookAt = null;

    void Update()
    {
        if (player == null || player.Combat.IsPlayingDirector) return;

        if (player.isLockOn && targetingSystem.CurrentTarget != null)
            UpdateLockOn();
        else
            UpdateFreeLook();
    }

    void UpdateLockOn()
    {
        Vector3 targetPos = targetingSystem.targetCollider != null
            ? targetingSystem.targetCollider.bounds.center
            : targetingSystem.CurrentTarget.transform.position;
        aimPoint.position = targetPos;

        float distance = Vector3.Distance(player.transform.position, targetPos);
        float desiredPitch = pitchByDistance.Evaluate(distance);

        var v = orbital.VerticalAxis;
        v.Value = Mathf.SmoothDamp(v.Value, desiredPitch, ref pitchVelocity, pitchDamping);
        orbital.VerticalAxis = v;

        var h = orbital.HorizontalAxis;
        h.Value = Mathf.Lerp(h.Value, 0f, horizontalRecenterSpeed * Time.deltaTime);
        orbital.HorizontalAxis = h;
    }

    void UpdateFreeLook()
    {
        Vector2 look = player.InputHandler.LookInput;
        float sensitivity = SettingManager.instance.MouseSensitivity * freeLookSensitivity * Time.deltaTime;

        var h = orbital.HorizontalAxis;
        h.Value += look.x * sensitivity;
        orbital.HorizontalAxis = h;

        var v = orbital.VerticalAxis;
        v.Value = Mathf.Clamp(v.Value - look.y * sensitivity, v.Range.x, v.Range.y);
        orbital.VerticalAxis = v;
    }
}
