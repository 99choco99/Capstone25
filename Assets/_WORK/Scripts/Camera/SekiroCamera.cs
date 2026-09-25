using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("FreeCam & LockOn 공통 구도")]
    [Tooltip("플레이어 루트에서 위로 올린 고정 LookAt 높이")]
    [SerializeField, Min(0f)] private float aimHeight = 1.3f;

    [Header("LockOn 구도")]
    [Tooltip("락온 중 LookAt을 적 쪽으로 옮기는 비율")]
    [SerializeField, Range(0f, 1f)] private float lockTargetFramingWeight = 0.15f;
    [Tooltip("락온 시작·해제 시 바라볼 지점을 전환하는 시간")]
    [SerializeField, Min(0f)] private float lockOnBlendDuration = 0.25f;

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
    private Vector3 framingTargetPosition;
    private bool isLockedOn;

    private float framingWeight;
    private float blendStartWeight;
    private float blendElapsed;

    private void Awake()
    {
        cinemachineCam = GetComponent<CinemachineCamera>();
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        inputController = GetComponent<CinemachineInputAxisController>();

        // 인살 카메라가 Live인 동안에도 복귀할 FreeCam 상태를 매 프레임 계산한다.
        // 그래야 인살 종료 시 오래된 Standby 상태로 되돌아가지 않는다.
        cinemachineCam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Always;

        GameObject aimObject = new("SekiroCamera_PlayerAim")
        {
            hideFlags = HideFlags.DontSave
        };
        aimTarget = aimObject.transform;
        cinemachineCam.LookAt = aimTarget;
    }

    private void OnEnable()
    {
        Player.OnLocalPlayerSpawned += BindPlayer;

        if (Application.isPlaying)
            BindExistingLocalPlayer();
    }

    private void OnDisable()
    {
        Player.OnLocalPlayerSpawned -= BindPlayer;
        if (player != null)
            player.InputHandler.OnCursorStateChanged -= HandleCursorState;

        // 재활성화 시 현재 타깃을 다시 적용하도록 런타임 모드만 초기화한다.
        isLockedOn = false;
        SetOrbitInputEnabled(enabled: false, resetMomentum: true);
    }

    private void OnDestroy()
    {
        if (aimTarget != null)
            Destroy(aimTarget.gameObject);
    }

    private void Start()
    {
        if (player == null)
            BindExistingLocalPlayer();
    }

    private void BindExistingLocalPlayer()
    {
        Player localPlayer = Player.LocalPlayer;
        if (localPlayer == null && targetingSystem != null)
            localPlayer = targetingSystem.GetComponentInParent<Player>();

        BindPlayer(localPlayer);
    }

    private void BindPlayer(Player localPlayer)
    {
        if (localPlayer == null || !localPlayer.IsLocalPlayer)
            return;

        if (player != null)
            player.InputHandler.OnCursorStateChanged -= HandleCursorState;

        player = localPlayer;
        player.InputHandler.OnCursorStateChanged += HandleCursorState;
        targetingSystem = localPlayer.TargetingSystem;
        followTarget = localPlayer.cameraRoot != null ? localPlayer.cameraRoot : localPlayer.transform;

        cinemachineCam.Follow = followTarget;

        framingWeight = 0f;
        blendStartWeight = 0f;
        blendElapsed = 0f;
        aimTarget.position = GetPlayerAimPosition();

        isLockedOn = false;
        // 이미 UI가 열린 뒤 카메라가 연결되는 경우에도 현재 입력 모드를 따른다.
        HandleCursorState(player.GetComponent<PlayerInput>().currentActionMap?.name == "UI");
    }

    /// <summary>UI 입력 차단도 카메라의 입력 제어와 같은 곳에서 처리합니다.</summary>
    private void HandleCursorState(bool isUIOpen)
    {
        inputController.enabled = !isUIOpen;
        SetOrbitInputEnabled(!isLockedOn, resetMomentum: true);
    }

    private void LateUpdate()
    {
        if (player == null || followTarget == null)
            return;

        ITargetable currentTarget = targetingSystem != null ? targetingSystem.CurrentTarget : null;
        Transform lockPoint = GetLockPoint(currentTarget);
        if (lockPoint != null)
        {
            // 락온 해제 중에도 마지막 적 위치를 사용해 시선을 부드럽게 되돌린다.
            framingTargetPosition = lockPoint.position;
        }

        bool shouldLockOn = lockPoint != null;
        if (shouldLockOn != isLockedOn)
        {
            isLockedOn = shouldLockOn;
            blendStartWeight = framingWeight;
            blendElapsed = 0f;
            SetOrbitInputEnabled(!isLockedOn, resetMomentum: true);
            orbitalFollow.HorizontalAxis.CancelRecentering();
            orbitalFollow.VerticalAxis.CancelRecentering();
        }

        // 인살 중에는 보이지 않는 FreeCam에 마우스 입력이 누적되지 않게 한다.
        SetOrbitInputEnabled(!isLockedOn, resetMomentum: false);

        // 카메라 설정은 공통값을 쓰고, 적 쪽을 바라보는 비율만 전환한다.
        blendElapsed = Mathf.Min(blendElapsed + Time.deltaTime, lockOnBlendDuration);
        float t = lockOnBlendDuration <= 0f ? 1f : blendElapsed / lockOnBlendDuration;
        float targetWeight = isLockedOn ? lockTargetFramingWeight : 0f;
        framingWeight = Mathf.Lerp(blendStartWeight, targetWeight, Mathf.SmoothStep(0f, 1f, t));

        Vector3 playerAimPosition = GetPlayerAimPosition();
        aimTarget.position = Vector3.Lerp(playerAimPosition, framingTargetPosition, framingWeight);

        if (isLockedOn && lockPoint != null)
            UpdateLockOnOrbit(lockPoint.position);
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
        orbitalFollow.HorizontalAxis.CancelRecentering();
        orbitalFollow.VerticalAxis.CancelRecentering();
        SetOrbitInputEnabled(!isLockedOn, resetMomentum: true);
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
            orbitDeadZone,
            orbitResponse,
            maxYawSpeed,
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

        Vector3 playerView = worldToCamera * (GetPlayerAimPosition() - cameraPosition);
        Vector3 enemyView = worldToCamera * (enemyLockPosition - cameraPosition);

        if (playerView.z <= 0.001f || enemyView.z <= 0.001f)
            return;

        float playerAngle = Mathf.Atan2(playerView.y, playerView.z) * Mathf.Rad2Deg;
        float enemyAngle = Mathf.Atan2(enemyView.y, enemyView.z) * Mathf.Rad2Deg;
        float currentSeparation = Mathf.DeltaAngle(playerAngle, enemyAngle);
        float pitchError = enemyAngleAbovePlayer - currentSeparation;

        float pitchStep = CalculateOrbitCorrection(pitchError, orbitDeadZone, orbitResponse, maxPitchSpeed, Time.deltaTime);

        float currentPitch = orbitalFollow.VerticalAxis.Value;
        orbitalFollow.VerticalAxis.Value = orbitalFollow.VerticalAxis.ClampValue(currentPitch + pitchStep);
    }

    /// <summary>
    /// 락온 축 오차를 이번 프레임에 적용할 각도 변화량으로 변환합니다.
    /// </summary>
    private static float CalculateOrbitCorrection(float error, float deadZone, float response, float maxSpeed, float deltaTime)
    {
        float absoluteError = Mathf.Abs(error);
        if (absoluteError <= deadZone || deltaTime <= 0f)
            return 0f;

        float remainingError = absoluteError - deadZone;
        float softZoneProgress = deadZone > 0.0001f ? Mathf.Clamp01(remainingError / deadZone) : 1f;
        float softZoneScale = Mathf.SmoothStep(0f, 1f, softZoneProgress);

        float signedError = Mathf.Sign(error) * remainingError * softZoneScale;
        float blend = 1f - Mathf.Exp(-Mathf.Max(0f, response) * deltaTime);
        float requestedStep = signedError * blend;
        float maximumStep = Mathf.Max(0f, maxSpeed) * deltaTime;

        return Mathf.Clamp(requestedStep, -maximumStep, maximumStep);
    }

    private void SetOrbitInputEnabled(bool enabled, bool resetMomentum)
    {
        enabled = enabled && inputController.enabled && (player == null || !player.Execution.IsExecuting);

        for (int i = 0; i < inputController.Controllers.Count; i++)
        {
            var controller = inputController.Controllers[i];
            if (controller == null || controller.Owner != orbitalFollow)
                continue;

            if (controller.Enabled == enabled && !resetMomentum)
                continue;

            controller.Enabled = enabled;

            var oldDriver = controller.Driver;
            controller.Driver = new DefaultInputAxisDriver
            {
                AccelTime = oldDriver.AccelTime,
                DecelTime = oldDriver.DecelTime
            };
            controller.InputValue = 0f;
        }
    }

    //================================== 조회 함수 ==========================================

    private static Transform GetLockPoint(ITargetable target)
    {
        if (target == null)
            return null;

        return target.LockOnPoint != null ? target.LockOnPoint : target.TargetTransform;
    }

    private Vector3 GetPlayerAimPosition()
    {
        return player.transform.position + Vector3.up * aimHeight;
    }
}
