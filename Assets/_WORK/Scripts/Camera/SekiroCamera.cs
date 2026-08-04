using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// FreeCam과 LockOn이 동일한 구면 궤도와 동일한 LookAt 규칙을 공유하는 카메라.
/// LockOn은 FreeCam에서 가능한 Yaw/Pitch 이동을 자동으로 대신 수행할 뿐이다.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineOrbitalFollow))]
[RequireComponent(typeof(CinemachineRotationComposer))]
public class SekiroCamera : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private TargetingSystem targetingSystem;

    [Header("FreeCam · LockOn 공통 구도")]
    [Tooltip("플레이어 루트에서 위로 올린 고정 LookAt 높이")]
    [SerializeField, Min(0f)] private float aimHeight = 1.3f;

    [Header("LockOn 수평 궤도")]
    [Tooltip("적의 반대편으로 공전하는 최대 각속도")]
    [SerializeField, Min(0f)] private float maxYawSpeed = 240f;

    [Header("LockOn 수직 구도")]
    [Tooltip("화면에서 적 LockPoint가 플레이어 Aim보다 위에 놓일 목표 각도")]
    [SerializeField, Range(0f, 30f)] private float enemyAngleAbovePlayer = 12f;

    [Tooltip("수직 구도 오차에 대한 반응 강도")]
    [SerializeField, Min(0f)] private float pitchCorrectionGain = 5f;

    [Tooltip("Sphere 위를 오르내리는 최대 각속도")]
    [SerializeField, Min(0f)] private float maxPitchSpeed = 90f;

    private CinemachineCamera cinemachineCam;
    private CinemachineOrbitalFollow orbitalFollow;
    private CinemachineInputAxisController inputController;

    private Player player;
    private Transform followTarget;
    private Transform aimTarget;

    private float fixedRadius;
    private float fixedRadialScale;
    private bool isLockedOn;

    private void Awake()
    {
        cinemachineCam = GetComponent<CinemachineCamera>();
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        inputController = GetComponent<CinemachineInputAxisController>();

        fixedRadius = orbitalFollow.Radius;
        fixedRadialScale = orbitalFollow.RadialAxis.ClampValue(
            orbitalFollow.RadialAxis.Value);

        aimTarget = new GameObject("SekiroCamera_PlayerAim").transform;
        aimTarget.SetParent(transform, false);
        cinemachineCam.LookAt = aimTarget;
    }

    private void OnEnable()
    {
        Player.OnLocalPlayerSpawned += BindPlayer;

        // 카메라가 플레이어보다 늦게 활성화되면 스폰 이벤트를 놓칠 수 있다.
        // 이때도 Follow가 비지 않도록 이미 존재하는 로컬 플레이어를 찾는다.
        if (Application.isPlaying)
            TryBindExistingLocalPlayer();
    }

    private void OnDisable()
    {
        Player.OnLocalPlayerSpawned -= BindPlayer;
        SetOrbitInputEnabled(true, true);
    }

    private void Start()
    {
        if (player == null)
            TryBindExistingLocalPlayer();
    }

    private bool TryBindExistingLocalPlayer()
    {
        if (player != null)
            return true;

        if (targetingSystem != null)
        {
            Player owner = targetingSystem.GetComponentInParent<Player>();
            if (owner != null && owner.IsLocalPlayer)
            {
                BindPlayer(owner);
                return player != null;
            }
        }

        Player[] candidates = FindObjectsByType<Player>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < candidates.Length; i++)
        {
            if (!candidates[i].IsLocalPlayer)
                continue;

            BindPlayer(candidates[i]);
            return player != null;
        }

        return false;
    }

    private void BindPlayer(Player localPlayer)
    {
        if (localPlayer == null || !localPlayer.IsLocalPlayer)
            return;

        player = localPlayer;
        targetingSystem = localPlayer.TargetingSystem;
        followTarget = localPlayer.cameraRoot != null
            ? localPlayer.cameraRoot
            : localPlayer.transform;

        cinemachineCam.Follow = followTarget;
        aimTarget.position = GetPlayerAimPosition();

        isLockedOn = false;
        SetOrbitInputEnabled(true, true);
    }

    private void LateUpdate()
    {
        if (player == null || followTarget == null)
            return;

        // 모드 불변식: Radius, Radial Scale, Follow, LookAt은 절대 전환하지 않는다.
        orbitalFollow.Radius = fixedRadius;
        orbitalFollow.RadialAxis.Value = fixedRadialScale;
        aimTarget.position = GetPlayerAimPosition();

        Transform lockPoint = GetCurrentLockPoint();
        bool shouldLockOn = lockPoint != null;

        if (shouldLockOn != isLockedOn)
        {
            // 해제 시 카메라 자세는 전혀 변경하지 않고 현재 축에서 입력만 재개한다.
            SetOrbitInputEnabled(!shouldLockOn, true);
            orbitalFollow.HorizontalAxis.CancelRecentering();
            orbitalFollow.VerticalAxis.CancelRecentering();
            isLockedOn = shouldLockOn;
        }

        if (shouldLockOn)
            UpdateLockOnOrbit(lockPoint.position);
    }

    private void UpdateLockOnOrbit(Vector3 enemyLockPosition)
    {
        Vector3 delta = enemyLockPosition - player.transform.position;
        float horizontalDistance = new Vector2(delta.x, delta.z).magnitude;

        UpdateYaw(delta, horizontalDistance);
        UpdatePitch(enemyLockPosition, horizontalDistance);
    }

    private void UpdateYaw(Vector3 delta, float horizontalDistance)
    {
        if (horizontalDistance <= 0.001f)
            return;

        float targetYaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
        float currentYaw = orbitalFollow.HorizontalAxis.Value;
        float nextYaw = Mathf.MoveTowardsAngle(
            currentYaw,
            targetYaw,
            maxYawSpeed * Time.deltaTime);

        orbitalFollow.HorizontalAxis.Value =
            orbitalFollow.HorizontalAxis.ClampValue(nextYaw);
    }

    private void UpdatePitch(Vector3 enemyLockPosition, float horizontalDistance)
    {
        // 현재 Sphere 위치에서 플레이어 Aim과 적 LockPoint가 이루는 수직 시야각을 계산한다.
        // 목표 각도와의 오차는 오직 VerticalAxis 이동으로만 해결한다.
        Vector3 orbitCenter = followTarget.position;
        float pitchRadians = orbitalFollow.VerticalAxis.Value * Mathf.Deg2Rad;
        float cameraHeight = fixedRadius * Mathf.Sin(pitchRadians);
        float cameraBack = Mathf.Max(0.001f, fixedRadius * Mathf.Cos(pitchRadians));

        float playerAimHeight = GetPlayerAimPosition().y - orbitCenter.y;
        float enemyHeight = enemyLockPosition.y - orbitCenter.y;

        float playerAngle = Mathf.Atan2(
            playerAimHeight - cameraHeight,
            cameraBack);
        float enemyAngle = Mathf.Atan2(
            enemyHeight - cameraHeight,
            horizontalDistance + cameraBack);

        float currentSeparation = (enemyAngle - playerAngle) * Mathf.Rad2Deg;
        float error = enemyAngleAbovePlayer - currentSeparation;

        float requestedSpeed = error * pitchCorrectionGain;
        float pitchStep = Mathf.Clamp(
            requestedSpeed,
            -maxPitchSpeed,
            maxPitchSpeed) * Time.deltaTime;

        orbitalFollow.VerticalAxis.Value = orbitalFollow.VerticalAxis.ClampValue(
            orbitalFollow.VerticalAxis.Value + pitchStep);
    }

    private Transform GetCurrentLockPoint()
    {
        ITargetable target = targetingSystem != null
            ? targetingSystem.CurrentTarget
            : null;

        if (target == null)
            return null;

        return target.LockOnPoint != null
            ? target.LockOnPoint
            : target.TargetTransform;
    }

    private Vector3 GetPlayerAimPosition()
    {
        return player.transform.position + Vector3.up * aimHeight;
    }

    private void SetOrbitInputEnabled(bool enabled, bool resetMomentum)
    {
        if (inputController == null || orbitalFollow == null)
            return;

        for (int i = 0; i < inputController.Controllers.Count; i++)
        {
            var controller = inputController.Controllers[i];
            if (controller == null || controller.Owner != orbitalFollow)
                continue;

            controller.Enabled = enabled;

            if (!resetMomentum)
                continue;

            var oldDriver = controller.Driver;
            controller.Driver = new DefaultInputAxisDriver
            {
                AccelTime = oldDriver.AccelTime,
                DecelTime = oldDriver.DecelTime
            };
            controller.InputValue = 0f;
        }
    }
}
