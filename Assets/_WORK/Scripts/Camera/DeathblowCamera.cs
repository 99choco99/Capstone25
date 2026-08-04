using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Timeline;


/// <summary>
/// 인살 중에만 FreeCam의 고정 구면 궤도를 벗어나는 연출 카메라입니다.
/// 진입할 때 실제 화면 출력을 이어받고, 종료할 때 마지막 출력 방향을
/// FreeCam이 허용하는 가장 가까운 궤도 자세로 인계합니다.
/// </summary>
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class DeathblowCamera : MonoBehaviour
{
    private const string CameraTrackName = "Camera";

    [Header("연결")]
    [Tooltip("인살을 재생하는 PlayerExecution. 비우면 로컬 플레이어에서 찾습니다.")]
    [SerializeField] private PlayerExecution execution;
    [Tooltip("인살 종료 자세를 이어받을 게임플레이 카메라. 비우면 같은 CameraSystem에서 찾습니다.")]
    [SerializeField] private SekiroCamera gameplayCamera;

    [Header("우선순위")]
    [SerializeField] private int activePriority = 100;
    [SerializeField] private int idlePriority = 0;

    [Header("샷 구도")]
    [Tooltip("공격 진행축의 뒤쪽으로 물러나는 거리")]
    [SerializeField, Min(0.1f)] private float distanceFront = 2f;
    [Tooltip("공격 진행축의 옆으로 떨어지는 거리")]
    [SerializeField, Min(0f)] private float distanceSide = 2.2f;
    [Tooltip("두 캐릭터의 중심점보다 카메라를 올리는 높이")]
    [SerializeField] private float height = 1.35f;
    [Tooltip("피격자 쪽 시선 기준점의 높이")]
    [SerializeField, Min(0f)] private float lookHeight = 1.2f;
    [Tooltip("공격자 쪽 시선 기준점의 높이")]
    [SerializeField, Min(0f)] private float attackerLookHeight = 1.05f;
    [Tooltip("0은 공격자, 1은 피격자를 중심으로 프레이밍합니다.")]
    [SerializeField, Range(0f, 1f)] private float victimFramingWeight = 0.55f;
    [SerializeField, Range(1f, 179f)] private float executionFieldOfView = 42f;

    [Header("카메라 움직임")]
    [Tooltip("인살 중 초당 전진하는 거리")]
    [SerializeField, Min(0f)] private float pushInPerSecond = 0.35f;
    [SerializeField, Min(0f)] private float maximumPushIn = 0.65f;
    [Tooltip("인살 샷 위치를 따라가는 반응 속도")]
    [SerializeField, Min(0f)] private float positionResponse = 8f;
    [Tooltip("두 캐릭터의 중심을 바라보는 반응 속도")]
    [SerializeField, Min(0f)] private float rotationResponse = 12f;
    [SerializeField, Min(0f)] private float lensResponse = 8f;

    [Header("충돌")]
    [SerializeField] private LayerMask collisionLayers = 1;
    [SerializeField, Min(0f)] private float cameraRadius = 0.2f;
    [SerializeField, Min(0f)] private float collisionPadding = 0.1f;

    private readonly RaycastHit[] collisionHits = new RaycastHit[16];

    private CinemachineCamera cam;
    private CinemachineBrain brain;
    private Transform attacker;
    private Transform victim;
    private Transform timelinePivot;
    private Animator timelinePivotAnimator;
    private Vector3 shotForward;
    private float shotSideSign = 1f;
    private bool active;
    private float elapsed;

    private void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
        cam.Priority = idlePriority;

        if (gameplayCamera == null && transform.parent != null)
        {
            gameplayCamera = transform.parent
                .GetComponentInChildren<SekiroCamera>(true);
        }

        CreateTimelinePivot();
        ResolveBrain();
    }

    private void OnEnable()
    {
        Player.OnLocalPlayerSpawned += TryBindFromPlayer;
        Subscribe();
    }

    private void Start()
    {
        if (execution == null && Player.LocalPlayer != null)
            TryBindFromPlayer(Player.LocalPlayer);
    }

    private void OnDisable()
    {
        Player.OnLocalPlayerSpawned -= TryBindFromPlayer;
        Unsubscribe();

        active = false;
        cam.Priority = idlePriority;
    }

    private void OnDestroy()
    {
        if (timelinePivot != null)
            Destroy(timelinePivot.gameObject);
    }

    private void TryBindFromPlayer(Player localPlayer)
    {
        if (execution != null
            || localPlayer == null
            || !localPlayer.IsLocalPlayer)
        {
            return;
        }

        execution = localPlayer.Execution;
        Subscribe();
    }

    private void Subscribe()
    {
        if (execution == null)
            return;

        execution.OnExecuteStart += Begin;
        execution.OnExecuteTimelineReady += BindTimelineCameraTrack;
        execution.OnExecuteEnd += End;
    }

    private void Unsubscribe()
    {
        if (execution == null)
            return;

        execution.OnExecuteStart -= Begin;
        execution.OnExecuteTimelineReady -= BindTimelineCameraTrack;
        execution.OnExecuteEnd -= End;
    }

    private void Begin(DeathblowPlan plan)
    {
        if (!plan.IsValid || execution == null)
            return;

        BeginShot(
            execution.transform,
            plan.Target.transform,
            plan.PlayerPose.position);
    }

    private void BeginShot(
        Transform attackerTransform,
        Transform victimTransform,
        Vector3 plannedAttackerPosition)
    {
        if (attackerTransform == null || victimTransform == null)
            return;

        attacker = attackerTransform;
        victim = victimTransform;
        elapsed = 0f;
        timelinePivot.SetLocalPositionAndRotation(
            Vector3.zero,
            Quaternion.identity);

        // 현재 Brain 출력을 복사하므로 우선순위가 바뀌는 첫 프레임에 점프하지 않는다.
        CaptureCurrentOutputPose();

        // 실제 시작 위치는 곧 정렬되므로, 정렬 전 attacker 위치가 아니라
        // DeathblowPlan의 최종 PlayerPose로 연출의 action axis를 고정한다.
        CacheShotBasis(
            plannedAttackerPosition,
            victimTransform.position);

        active = true;
        cam.Priority = activePriority;
    }

    private void End()
    {
        // 우선순위를 내리기 전에 플레이어가 실제로 본 마지막 출력 자세를 읽는다.
        // FreeCam은 이 방향을 자신의 고정 구면 궤도 안으로 Clamp해 이어받는다.
        if (active
            && gameplayCamera != null
            && TryGetCurrentOutputPose(
                out Vector3 outputPosition,
                out Quaternion outputRotation))
        {
            gameplayCamera.AdoptOutputPose(
                outputPosition,
                outputRotation);
        }

        active = false;
        cam.Priority = idlePriority;
        attacker = null;
        victim = null;
    }

    private void LateUpdate()
    {
        if (!active || attacker == null || victim == null)
            return;

        float deltaTime = Time.deltaTime;
        elapsed += deltaTime;
        UpdateCamera(deltaTime);
    }

    private void CaptureCurrentOutputPose()
    {
        if (ResolveBrain())
        {
            CameraState state = brain.State;
            transform.SetPositionAndRotation(
                state.GetFinalPosition(),
                state.GetFinalOrientation());

            LensSettings lens = cam.Lens;
            lens.FieldOfView = state.Lens.FieldOfView;
            cam.Lens = lens;
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            transform.SetPositionAndRotation(
                mainCamera.transform.position,
                mainCamera.transform.rotation);
        }
    }

    private bool TryGetCurrentOutputPose(
        out Vector3 position,
        out Quaternion rotation)
    {
        if (ResolveBrain())
        {
            CameraState state = brain.State;
            position = state.GetFinalPosition();
            rotation = state.GetFinalOrientation();
            return true;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            position = mainCamera.transform.position;
            rotation = mainCamera.transform.rotation;
            return true;
        }

        position = default;
        rotation = default;
        return false;
    }

    private void CacheShotBasis(
        Vector3 plannedAttackerPosition,
        Vector3 plannedVictimPosition)
    {
        Vector3 focus = GetFocusPoint();
        shotForward = plannedVictimPosition - plannedAttackerPosition;
        shotForward.y = 0f;

        if (shotForward.sqrMagnitude < 0.0001f)
        {
            shotForward = victim.position - attacker.position;
            shotForward.y = 0f;
        }

        if (shotForward.sqrMagnitude < 0.0001f)
        {
            shotForward = victim.forward;
            shotForward.y = 0f;
        }

        if (shotForward.sqrMagnitude < 0.0001f)
            shotForward = Vector3.forward;

        shotForward.Normalize();

        // 진입 카메라가 있던 쪽을 고정해 연출 중 임의로 180도 선을 넘지 않는다.
        Vector3 shotRight = Vector3.Cross(Vector3.up, shotForward);
        float side = Vector3.Dot(transform.position - focus, shotRight);
        shotSideSign = Mathf.Abs(side) > 0.001f
            ? Mathf.Sign(side)
            : 1f;
    }

    private void UpdateCamera(float deltaTime)
    {
        Vector3 focus = GetFocusPoint();
        Vector3 baseShotRight =
            Vector3.Cross(Vector3.up, shotForward) * shotSideSign;

        // Timeline은 양수 Yaw 곡선만 제공합니다. 진입한 쪽의 바깥 방향으로
        // 부호를 적용해야 기존 +60도 곡선이 action axis 반대편을 가로지르지 않는다.
        float authoredYaw = Mathf.DeltaAngle(
            0f,
            timelinePivot.localEulerAngles.y);
        float timelineYaw = -authoredYaw * shotSideSign;
        Quaternion timelineOrbit =
            Quaternion.AngleAxis(timelineYaw, Vector3.up);
        Vector3 currentShotForward = timelineOrbit * shotForward;
        Vector3 currentShotRight = timelineOrbit * baseShotRight;

        float pushIn = Mathf.Min(
            maximumPushIn,
            pushInPerSecond * elapsed);
        float front = Mathf.Max(0.5f, distanceFront - pushIn * 0.5f);
        float side = Mathf.Max(0.5f, distanceSide - pushIn);

        Vector3 desiredPosition =
            focus
            - currentShotForward * front
            + currentShotRight * side
            + Vector3.up * height;

        float positionBlend =
            1f - Mathf.Exp(-positionResponse * deltaTime);
        float rotationBlend =
            1f - Mathf.Exp(-rotationResponse * deltaTime);
        float lensBlend =
            1f - Mathf.Exp(-lensResponse * deltaTime);

        // 안전한 목적지를 먼저 만든 뒤 보간하면 중간 선분이 벽을 통과할 수 있다.
        // 이번 프레임의 보간 후보 자체를 충돌 검사한 뒤 최종 위치로 사용한다.
        Vector3 candidatePosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            positionBlend);
        transform.position = ResolveCollision(
            focus,
            candidatePosition);

        Vector3 lookDirection = focus - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation =
                Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationBlend);
        }

        LensSettings lens = cam.Lens;
        lens.FieldOfView = Mathf.Lerp(
            lens.FieldOfView,
            executionFieldOfView,
            lensBlend);
        cam.Lens = lens;
    }

    private Vector3 GetFocusPoint()
    {
        Vector3 attackerPoint =
            attacker.position + Vector3.up * attackerLookHeight;
        Vector3 victimPoint =
            victim.position + Vector3.up * lookHeight;

        return Vector3.Lerp(
            attackerPoint,
            victimPoint,
            victimFramingWeight);
    }

    private Vector3 ResolveCollision(
        Vector3 focus,
        Vector3 desiredPosition)
    {
        Vector3 offset = desiredPosition - focus;
        float distance = offset.magnitude;
        if (distance <= 0.001f)
            return desiredPosition;

        int hitCount = Physics.SphereCastNonAlloc(
            focus,
            cameraRadius,
            offset / distance,
            collisionHits,
            distance,
            collisionLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = collisionHits[i];
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;
            bool hitAttacker = attacker != null
                && (hitTransform == attacker
                    || hitTransform.IsChildOf(attacker));
            bool hitVictim = victim != null
                && (hitTransform == victim
                    || hitTransform.IsChildOf(victim));
            if (hitAttacker || hitVictim)
            {
                continue;
            }

            nearestDistance = Mathf.Min(
                nearestDistance,
                hit.distance);
        }

        if (float.IsPositiveInfinity(nearestDistance))
            return desiredPosition;

        float safeDistance = Mathf.Max(
            0.1f,
            nearestDistance - collisionPadding);
        return focus + offset.normalized * safeDistance;
    }

    private bool ResolveBrain()
    {
        if (brain != null)
            return true;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            brain = mainCamera.GetComponent<CinemachineBrain>();

        if (brain == null)
            brain = FindFirstObjectByType<CinemachineBrain>();

        return brain != null;
    }

    private void CreateTimelinePivot()
    {
        GameObject pivotObject = new GameObject("DeathblowTimelinePivot")
        {
            hideFlags = HideFlags.DontSave
        };
        timelinePivot = pivotObject.transform;
        timelinePivot.SetParent(transform.parent, false);
        timelinePivotAnimator = pivotObject.AddComponent<Animator>();
    }

    private void BindTimelineCameraTrack(TimelineAsset timeline)
    {
        if (timeline == null || execution == null)
            return;

        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            if (track is not AnimationTrack animationTrack
                || track.name != CameraTrackName)
            {
                continue;
            }

            // 현재 Timeline Camera 트랙은 pivot의 Yaw 곡선을 제공하고,
            // 실제 거리·높이·충돌·LookAt은 이 컴포넌트가 일관되게 담당한다.
            execution.DeathblowDirector.SetGenericBinding(
                animationTrack,
                timelinePivotAnimator);
            return;
        }
    }

    public void Play(
        Transform attackerTransform,
        Transform victimTransform)
    {
        Vector3 plannedAttackerPosition = attackerTransform != null
            ? attackerTransform.position
            : Vector3.zero;
        BeginShot(
            attackerTransform,
            victimTransform,
            plannedAttackerPosition);
    }

    public void Stop()
    {
        End();
    }
}
