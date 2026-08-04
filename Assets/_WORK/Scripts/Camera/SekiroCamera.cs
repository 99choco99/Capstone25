using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;


/// <summary>
/// FreeCam과 LockOn을 하나의 OrbitalFollow에서 제어합니다.
/// LockOn은 FreeCam이 허용하는 동일한 Yaw/Pitch 범위와 고정 반경만 사용하므로
/// 락온 해제 시 별도 카메라로 갈아타거나 궤도 위치를 초기화하지 않습니다.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineOrbitalFollow))]
[RequireComponent(typeof(CinemachineRotationComposer))]
[RequireComponent(typeof(CinemachineInputAxisController))]
public class SekiroCamera : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private TargetingSystem targetingSystem;
    [Tooltip("대상에 전용 프로필이 없을 때 사용할 공통 락온 프로필")]
    [SerializeField] private LockOnCameraProfile defaultLockOnProfile;

    [Header("FreeCam · LockOn 공통 구도")]
    [Tooltip("플레이어 루트에서 위로 올린 고정 LookAt 높이")]
    [SerializeField, Min(0f)] private float aimHeight = 1.3f;

    [Header("LockOn 구도")]
    [Tooltip("전용 프로필이 없을 때 LookAt을 적 쪽으로 옮기는 비율")]
    [SerializeField, Range(0f, 1f)] private float lockTargetFramingWeight = 0.15f;
    [Tooltip("전용 프로필이 없을 때 자유/락온 설정을 보간하는 시간")]
    [SerializeField, Min(0f)] private float profileBlendDuration = 0.25f;

    [Header("LockOn 수평 궤도")]
    [Tooltip("적의 반대편으로 공전하는 최대 각속도")]
    [SerializeField, Min(0f)] private float maxYawSpeed = 240f;

    [Header("LockOn 공통 반응")]
    [Tooltip("이 각도 이내의 작은 오차는 카메라가 쫓지 않습니다.")]
    [SerializeField, Range(0f, 5f)] private float orbitDeadZone = 1f;
    [Tooltip("Yaw와 Pitch가 오차를 줄이는 반응 강도입니다.")]
    [FormerlySerializedAs("pitchCorrectionGain")]
    [SerializeField, Min(0f)] private float orbitResponse = 8f;

    [Header("LockOn 수직 구도")]
    [Tooltip("화면에서 적 LockPoint가 플레이어 Aim보다 위에 놓일 목표 각도")]
    [SerializeField, Range(0f, 30f)] private float enemyAngleAbovePlayer = 12f;
    [Tooltip("Sphere 위를 오르내리는 최대 각속도")]
    [SerializeField, Min(0f)] private float maxPitchSpeed = 90f;

    private CinemachineCamera cinemachineCam;
    private CinemachineOrbitalFollow orbitalFollow;
    private CinemachineInputAxisController inputController;

    private Player player;
    private Transform followTarget;
    private Transform aimTarget;
    private ITargetable activeTarget;
    private Vector3 framingTargetPosition;
    private bool hasFramingTarget;

    private float fixedRadius;
    private float fixedRadialScale;
    private bool isLockedOn;

    private CameraSettings freeSettings;
    private CameraSettings activeSettings;
    private CameraSettings blendStartSettings;
    private CameraSettings blendTargetSettings;
    private float settingsBlendElapsed;
    private float settingsBlendDuration;

    private void Awake()
    {
        cinemachineCam = GetComponent<CinemachineCamera>();
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        inputController = GetComponent<CinemachineInputAxisController>();

        // 인살 카메라가 Live인 동안에도 복귀할 FreeCam 상태를 매 프레임 계산한다.
        // 그래야 인살 종료 시 오래된 Standby 상태로 되돌아가지 않는다.
        cinemachineCam.StandbyUpdate =
            CinemachineVirtualCameraBase.StandbyUpdateMode.Always;

        fixedRadius = orbitalFollow.Radius;
        fixedRadialScale = orbitalFollow.RadialAxis.ClampValue(
            orbitalFollow.RadialAxis.Value);

        freeSettings = CreateFreeSettings();
        activeSettings = freeSettings;
        blendStartSettings = freeSettings;
        blendTargetSettings = freeSettings;

        GameObject aimObject = new GameObject("SekiroCamera_PlayerAim")
        {
            hideFlags = HideFlags.DontSave
        };
        aimTarget = aimObject.transform;
        cinemachineCam.LookAt = aimTarget;
    }

    private void OnEnable()
    {
        Player.OnLocalPlayerSpawned += BindPlayer;

        // 카메라가 플레이어보다 늦게 활성화되면 스폰 이벤트를 놓칠 수 있다.
        if (Application.isPlaying)
            TryBindExistingLocalPlayer();
    }

    private void OnDisable()
    {
        Player.OnLocalPlayerSpawned -= BindPlayer;

        // 재활성화 시 현재 타깃을 다시 적용하도록 런타임 모드만 초기화한다.
        activeTarget = null;
        isLockedOn = false;
        hasFramingTarget = false;
        SetOrbitInputEnabled(true, true);
    }

    private void OnDestroy()
    {
        if (aimTarget != null)
            Destroy(aimTarget.gameObject);
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

        freeSettings = CreateFreeSettings();
        SetSettingsImmediately(freeSettings);
        aimTarget.position = GetPlayerAimPosition();

        activeTarget = null;
        isLockedOn = false;
        hasFramingTarget = false;
        SetOrbitInputEnabled(true, true);
    }

    private void LateUpdate()
    {
        if (player == null || followTarget == null)
            return;

        ITargetable currentTarget = GetCurrentTarget();
        Transform lockPoint = GetLockPoint(currentTarget);
        if (lockPoint != null)
        {
            framingTargetPosition = lockPoint.position;
            hasFramingTarget = true;
        }

        // 대상 변경은 같은 락온 모드 안에서도 프로필 전환을 일으킬 수 있다.
        if (currentTarget != activeTarget || (lockPoint != null) != isLockedOn)
            ApplyTarget(currentTarget, lockPoint != null);

        UpdateSettingsBlend(Time.deltaTime);
        ApplyCameraSettings();

        Vector3 playerAimPosition = GetPlayerAimPosition();
        aimTarget.position = hasFramingTarget
            ? Vector3.Lerp(
                playerAimPosition,
                framingTargetPosition,
                activeSettings.TargetFramingWeight)
            : playerAimPosition;

        if (!isLockedOn
            && activeSettings.TargetFramingWeight <= 0.0001f)
        {
            hasFramingTarget = false;
        }

        if (isLockedOn && lockPoint != null)
            UpdateLockOnOrbit(lockPoint.position);
    }

    private void ApplyTarget(ITargetable target, bool shouldLockOn)
    {
        activeTarget = target;
        isLockedOn = shouldLockOn;

        SetOrbitInputEnabled(!shouldLockOn, true);
        orbitalFollow.HorizontalAxis.CancelRecentering();
        orbitalFollow.VerticalAxis.CancelRecentering();

        if (!shouldLockOn)
        {
            BeginSettingsBlend(freeSettings, profileBlendDuration);
            return;
        }

        LockOnCameraProfile profile = ResolveProfile(target);
        CameraSettings settings = profile != null
            ? CameraSettings.FromProfile(profile)
            : CreateFallbackLockOnSettings();
        float duration = profile != null
            ? profile.BlendDuration
            : profileBlendDuration;

        BeginSettingsBlend(settings, duration);
    }

    private LockOnCameraProfile ResolveProfile(ITargetable target)
    {
        if (target is ILockOnCameraProfileProvider provider
            && provider.LockOnCameraProfile != null)
        {
            return provider.LockOnCameraProfile;
        }

        return defaultLockOnProfile;
    }

    private void BeginSettingsBlend(CameraSettings target, float duration)
    {
        blendStartSettings = activeSettings;
        blendTargetSettings = target;
        settingsBlendElapsed = 0f;
        settingsBlendDuration = Mathf.Max(0f, duration);

        if (settingsBlendDuration <= 0f)
            activeSettings = blendTargetSettings;
    }

    private void UpdateSettingsBlend(float deltaTime)
    {
        if (settingsBlendDuration <= 0f
            || settingsBlendElapsed >= settingsBlendDuration)
        {
            activeSettings = blendTargetSettings;
            return;
        }

        settingsBlendElapsed += Mathf.Max(0f, deltaTime);
        float t = Mathf.Clamp01(settingsBlendElapsed / settingsBlendDuration);
        activeSettings = CameraSettings.Lerp(
            blendStartSettings,
            blendTargetSettings,
            Mathf.SmoothStep(0f, 1f, t));
    }

    private void SetSettingsImmediately(CameraSettings settings)
    {
        activeSettings = settings;
        blendStartSettings = settings;
        blendTargetSettings = settings;
        settingsBlendElapsed = 0f;
        settingsBlendDuration = 0f;
        ApplyCameraSettings();
    }

    private void ApplyCameraSettings()
    {
        // LockOn은 FreeCam과 같은 구면 궤도의 부분집합이다.
        // 프로필은 구도와 추적 반응만 바꾸며 Radius/FOV/축 범위는 바꾸지 않는다.
        // 따라서 LockOn -> FreeCam 전환 순간에도 현재 궤도 위치가 그대로 유효하다.
        orbitalFollow.Radius = fixedRadius;
        orbitalFollow.RadialAxis.Value = fixedRadialScale;
    }

    /// <summary>
    /// 연출 카메라의 마지막 출력 방향을 FreeCam 궤도에 인계합니다.
    /// </summary>
    public void AdoptOutputPose(
        Vector3 outputPosition,
        Quaternion outputRotation)
    {
        if (followTarget == null)
            return;

        // Sphere OrbitalFollow의 ForceCameraPosition은 전달된 위치에서 Yaw/Pitch를
        // 역산하고 각각의 FreeCam 축 범위로 Clamp한다. 연출 샷이 구면 밖에 있어도
        // 반경을 늘리지 않고, 같은 방향에서 가장 가까운 허용 궤도로 복귀한다.
        cinemachineCam.ForceCameraPosition(
            outputPosition,
            outputRotation);
        orbitalFollow.Radius = fixedRadius;
        orbitalFollow.RadialAxis.Value = fixedRadialScale;
        orbitalFollow.HorizontalAxis.CancelRecentering();
        orbitalFollow.VerticalAxis.CancelRecentering();
        SetOrbitInputEnabled(!isLockedOn, true);
    }

    private CameraSettings CreateFreeSettings()
    {
        return new CameraSettings
        {
            PlayerAimHeight = aimHeight,
            TargetFramingWeight = 0f,
            EnemyAngleAbovePlayer = enemyAngleAbovePlayer,
            YawPlayAngle = orbitDeadZone,
            PitchPlayAngle = orbitDeadZone,
            YawResponse = orbitResponse,
            PitchResponse = orbitResponse,
            MaxYawSpeed = maxYawSpeed,
            MaxPitchSpeed = maxPitchSpeed
        };
    }

    private CameraSettings CreateFallbackLockOnSettings()
    {
        CameraSettings settings = freeSettings;
        settings.TargetFramingWeight = lockTargetFramingWeight;
        settings.EnemyAngleAbovePlayer = enemyAngleAbovePlayer;
        settings.YawPlayAngle = orbitDeadZone;
        settings.PitchPlayAngle = orbitDeadZone;
        settings.YawResponse = orbitResponse;
        settings.PitchResponse = orbitResponse;
        settings.MaxYawSpeed = maxYawSpeed;
        settings.MaxPitchSpeed = maxPitchSpeed;
        return settings;
    }

    private void UpdateLockOnOrbit(Vector3 enemyLockPosition)
    {
        Vector3 delta = enemyLockPosition - player.transform.position;
        float horizontalDistance = new Vector2(delta.x, delta.z).magnitude;

        UpdateYaw(delta, horizontalDistance);
        UpdatePitch(enemyLockPosition);
    }

    private void UpdateYaw(Vector3 delta, float horizontalDistance)
    {
        if (horizontalDistance <= 0.001f)
            return;

        float targetYaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
        float currentYaw = orbitalFollow.HorizontalAxis.Value;
        float yawError = Mathf.DeltaAngle(currentYaw, targetYaw);
        float yawStep = CalculateOrbitCorrection(
            yawError,
            activeSettings.YawPlayAngle,
            activeSettings.YawResponse,
            activeSettings.MaxYawSpeed,
            Time.deltaTime);

        orbitalFollow.HorizontalAxis.Value =
            orbitalFollow.HorizontalAxis.ClampValue(currentYaw + yawStep);
    }

    private void UpdatePitch(Vector3 enemyLockPosition)
    {
        // Deoccluder까지 처리된 직전 최종 CameraState를 사용한다.
        // 벽 때문에 카메라가 구면에서 밀려나도 실제 화면상의 수직 각도를 계산할 수 있다.
        // 두 Aim 지점의 간격을 맞추는 방식이므로 일반 체형의 구도는 안정화하지만,
        // 캐릭터 Renderer 전체가 화면 안에 든다는 보장까지 하지는 않는다.
        if (!cinemachineCam.PreviousStateIsValid)
            return;

        CameraState cameraState = cinemachineCam.State;
        Vector3 cameraPosition = cameraState.GetFinalPosition();
        Quaternion worldToCamera =
            Quaternion.Inverse(cameraState.GetFinalOrientation());

        Vector3 playerView =
            worldToCamera * (GetPlayerAimPosition() - cameraPosition);
        Vector3 enemyView =
            worldToCamera * (enemyLockPosition - cameraPosition);

        if (playerView.z <= 0.001f || enemyView.z <= 0.001f)
            return;

        float playerAngle =
            Mathf.Atan2(playerView.y, playerView.z) * Mathf.Rad2Deg;
        float enemyAngle =
            Mathf.Atan2(enemyView.y, enemyView.z) * Mathf.Rad2Deg;
        float currentSeparation = Mathf.DeltaAngle(playerAngle, enemyAngle);
        float error =
            activeSettings.EnemyAngleAbovePlayer - currentSeparation;

        float pitchStep = CalculateOrbitCorrection(
            error,
            activeSettings.PitchPlayAngle,
            activeSettings.PitchResponse,
            activeSettings.MaxPitchSpeed,
            Time.deltaTime);

        orbitalFollow.VerticalAxis.Value =
            orbitalFollow.VerticalAxis.ClampValue(
                orbitalFollow.VerticalAxis.Value + pitchStep);
    }

    /// <summary>
    /// 락온 축 오차를 이번 프레임에 적용할 각도 변화량으로 변환합니다.
    /// </summary>
    private static float CalculateOrbitCorrection(
        float error,
        float playAngle,
        float response,
        float maxSpeed,
        float deltaTime)
    {
        float absoluteError = Mathf.Abs(error);
        if (absoluteError <= playAngle || deltaTime <= 0f)
            return 0f;

        float remainingError = absoluteError - playAngle;
        float softZoneScale = playAngle > 0.0001f
            ? Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(remainingError / playAngle))
            : 1f;

        float signedError =
            Mathf.Sign(error) * remainingError * softZoneScale;
        float blend = 1f - Mathf.Exp(-Mathf.Max(0f, response) * deltaTime);
        float requestedStep = signedError * blend;
        float maximumStep = Mathf.Max(0f, maxSpeed) * deltaTime;

        return Mathf.Clamp(requestedStep, -maximumStep, maximumStep);
    }

    private ITargetable GetCurrentTarget()
    {
        return targetingSystem != null
            ? targetingSystem.CurrentTarget
            : null;
    }

    private static Transform GetLockPoint(ITargetable target)
    {
        if (target == null)
            return null;

        return target.LockOnPoint != null
            ? target.LockOnPoint
            : target.TargetTransform;
    }

    private Vector3 GetPlayerAimPosition()
    {
        return player.transform.position
            + Vector3.up * activeSettings.PlayerAimHeight;
    }

    private void SetOrbitInputEnabled(bool enabled, bool resetMomentum)
    {
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

    private struct CameraSettings
    {
        public float PlayerAimHeight;
        public float TargetFramingWeight;
        public float EnemyAngleAbovePlayer;
        public float YawPlayAngle;
        public float PitchPlayAngle;
        public float YawResponse;
        public float PitchResponse;
        public float MaxYawSpeed;
        public float MaxPitchSpeed;

        public static CameraSettings FromProfile(LockOnCameraProfile profile)
        {
            return new CameraSettings
            {
                PlayerAimHeight = profile.PlayerAimHeight,
                TargetFramingWeight = profile.TargetFramingWeight,
                EnemyAngleAbovePlayer = profile.EnemyAngleAbovePlayer,
                YawPlayAngle = profile.YawPlayAngle,
                PitchPlayAngle = profile.PitchPlayAngle,
                YawResponse = profile.YawResponse,
                PitchResponse = profile.PitchResponse,
                MaxYawSpeed = profile.MaxYawSpeed,
                MaxPitchSpeed = profile.MaxPitchSpeed
            };
        }

        public static CameraSettings Lerp(
            CameraSettings from,
            CameraSettings to,
            float t)
        {
            return new CameraSettings
            {
                PlayerAimHeight = Mathf.Lerp(from.PlayerAimHeight, to.PlayerAimHeight, t),
                TargetFramingWeight = Mathf.Lerp(from.TargetFramingWeight, to.TargetFramingWeight, t),
                EnemyAngleAbovePlayer = Mathf.Lerp(from.EnemyAngleAbovePlayer, to.EnemyAngleAbovePlayer, t),
                YawPlayAngle = Mathf.Lerp(from.YawPlayAngle, to.YawPlayAngle, t),
                PitchPlayAngle = Mathf.Lerp(from.PitchPlayAngle, to.PitchPlayAngle, t),
                YawResponse = Mathf.Lerp(from.YawResponse, to.YawResponse, t),
                PitchResponse = Mathf.Lerp(from.PitchResponse, to.PitchResponse, t),
                MaxYawSpeed = Mathf.Lerp(from.MaxYawSpeed, to.MaxYawSpeed, t),
                MaxPitchSpeed = Mathf.Lerp(from.MaxPitchSpeed, to.MaxPitchSpeed, t)
            };
        }
    }
}
