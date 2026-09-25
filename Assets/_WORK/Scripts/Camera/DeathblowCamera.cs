using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;


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
    [Header("연결")]
    [Tooltip("같은 CameraSystem에서 실제 화면을 출력하는 Brain")]
    [SerializeField] private CinemachineBrain brain;
    [Tooltip("인살 종료 자세를 이어받을 같은 CameraSystem의 게임플레이 카메라")]
    [SerializeField] private SekiroCamera gameplayCamera;

    [Header("우선순위")]
    [SerializeField] private int activePriority = 100;
    [SerializeField] private int idlePriority = 0;

    [Header("샷 구도")]
    [Tooltip("공격 진행축의 뒤쪽으로 물러나는 거리")]
    [SerializeField, Min(0.1f)] private float backDistance = 2f;
    [Tooltip("공격 진행축의 옆으로 떨어지는 거리")]
    [SerializeField, Min(0f)] private float sideDistance = 2.2f;
    [Tooltip("두 캐릭터의 중심점보다 카메라를 올리는 높이")]
    [SerializeField] private float height = 0.65f;
    [Tooltip("피격자 쪽 시선 기준점의 높이")]
    [SerializeField, Min(0f)] private float victimLookHeight = 1.2f;
    [Tooltip("공격자 쪽 시선 기준점의 높이")]
    [SerializeField, Min(0f)] private float attackerLookHeight = 1.05f;
    [Tooltip("0은 공격자, 1은 피격자를 중심으로 프레이밍합니다.")]
    [SerializeField, Range(0f, 1f)] private float victimFramingWeight = 0.55f;
    [SerializeField, Range(1f, 179f)] private float executionFieldOfView = 42f;

    [Header("카메라 움직임")]
    [Tooltip("정면 인살 재생 시간(초)에 따른 회전 각도")]
    [SerializeField] private AnimationCurve frontYaw = AnimationCurve.EaseInOut(0f, 0f, 0.45f, 25f);
    [Tooltip("후방 인살 재생 시간(초)에 따른 회전 각도")]
    [SerializeField] private AnimationCurve behindYaw = AnimationCurve.EaseInOut(0f, 0f, 0.3f, 15f);
    [Tooltip("정면 인살 진행률(0~1)에 따른 접근 강도. 찌를 때 접근하고 끝나기 전에 물러납니다.")]
    [SerializeField] private AnimationCurve frontPushIn = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.12f, 1f),
        new Keyframe(0.55f, 1f), new Keyframe(1f, 0f));
    [Tooltip("후방 인살 진행률(0~1)에 따른 접근 강도. 정면보다 작은 움직임을 사용합니다.")]
    [SerializeField] private AnimationCurve behindPushIn = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.13f, 0.65f),
        new Keyframe(0.65f, 0.65f), new Keyframe(1f, 0f));
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
    private PlayerExecution execution;
    private Transform attacker;
    private Transform victim;
    private Vector3 shotForward;
    private float shotSideSign = 1f;
    private bool active;
    private DeathblowDirection direction;
    private float startFieldOfView;

    private void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
        cam.Priority = idlePriority;
    }

    private void OnEnable()
    {
        Player.OnLocalPlayerSpawned += BindPlayer;
        BindPlayer(Player.LocalPlayer);
    }

    private void OnDisable()
    {
        Player.OnLocalPlayerSpawned -= BindPlayer;
        Unsubscribe();

        active = false;
        cam.Priority = idlePriority;
    }

    private void BindPlayer(Player localPlayer)
    {
        if (localPlayer == null || !localPlayer.IsLocalPlayer)
        {
            return;
        }

        Unsubscribe();
        execution = localPlayer.Execution;
        execution.OnExecuteStart += Begin;
        execution.OnExecuteEnd += End;
    }

    private void Unsubscribe()
    {
        if (execution == null)
            return;

        execution.OnExecuteStart -= Begin;
        execution.OnExecuteEnd -= End;
    }

    private void Begin(DeathblowPlan plan)
    {
        direction = plan.Direction;
        attacker = execution.transform;
        victim = plan.Target.transform;

        // 현재 Brain 출력을 복사하므로 우선순위가 바뀌는 첫 프레임에 점프하지 않는다.
        CameraState state = brain.State;
        transform.SetPositionAndRotation(state.GetFinalPosition(), state.GetFinalOrientation());
        startFieldOfView = state.Lens.FieldOfView;
        LensSettings lens = cam.Lens;
        lens.FieldOfView = startFieldOfView;
        cam.Lens = lens;

        // 실제 시작 위치는 곧 정렬되므로, 정렬 전 attacker 위치가 아니라
        // DeathblowPlan의 최종 PlayerPose로 연출의 action axis를 고정한다.
        shotForward = plan.PlayerPose.rotation * Vector3.forward;

        // 진입 카메라가 있던 쪽을 고정해 연출 중 임의로 180도 선을 넘지 않는다.
        Vector3 shotRight = Vector3.Cross(Vector3.up, shotForward);
        float side = Vector3.Dot(transform.position - GetFocusPoint(), shotRight);
        shotSideSign = Mathf.Abs(side) > 0.001f ? Mathf.Sign(side) : 1f;

        active = true;
        cam.Priority = activePriority;
    }

    private void End()
    {
        if (!active)
            return;

        // 우선순위를 내리기 전에 플레이어가 실제로 본 마지막 출력 자세를 읽는다.
        // FreeCam은 이 방향을 자신의 고정 구면 궤도 안으로 Clamp해 이어받는다.
        CameraState state = brain.State;
        gameplayCamera.AdoptOutputPose(state.GetFinalPosition(), state.GetFinalOrientation());

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
        Vector3 focus = GetFocusPoint();
        Vector3 baseShotRight =
            Vector3.Cross(Vector3.up, shotForward) * shotSideSign;

        // 정렬 중에는 이전 재생 시간이 아닌 0을 사용한다.
        PlayableDirector director = execution.DeathblowDirector;
        float playTime = director.state == PlayState.Playing ? (float)director.time : 0f;

        // Yaw 곡선은 양수 각도만 제공합니다. 진입한 쪽의 바깥 방향으로
        // 부호를 적용해야 회전 곡선이 action axis 반대편을 가로지르지 않는다.
        AnimationCurve yawCurve = direction == DeathblowDirection.Front ? frontYaw : behindYaw;
        float yaw = -yawCurve.Evaluate(playTime) * shotSideSign;
        Quaternion orbit = Quaternion.AngleAxis(yaw, Vector3.up);
        Vector3 currentShotForward = orbit * shotForward;
        Vector3 currentShotRight = orbit * baseShotRight;

        // 정렬 대기나 프레임 수가 아니라 실제 Timeline 진행에 맞춰 접근·복귀한다.
        float progress = director.duration > 0 ? Mathf.Clamp01(playTime / (float)director.duration) : 0f;

        AnimationCurve pushInCurve = direction == DeathblowDirection.Front ? frontPushIn : behindPushIn;
        float pushInWeight = Mathf.Clamp01(pushInCurve.Evaluate(progress));
        float pushIn = maximumPushIn * pushInWeight;
        float backOffset = Mathf.Max(0.5f, backDistance - pushIn * 0.5f);
        float sideOffset = Mathf.Max(0.5f, sideDistance - pushIn);

        Vector3 desiredPosition =
            focus
            - currentShotForward * backOffset
            + currentShotRight * sideOffset
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

        float targetFov = Mathf.Lerp(startFieldOfView, executionFieldOfView, pushInWeight);
        LensSettings lens = cam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, lensBlend);
        cam.Lens = lens;
    }

    private Vector3 GetFocusPoint()
    {
        Vector3 attackerPoint =
            attacker.position + Vector3.up * attackerLookHeight;
        Vector3 victimPoint =
            victim.position + Vector3.up * victimLookHeight;

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

}
