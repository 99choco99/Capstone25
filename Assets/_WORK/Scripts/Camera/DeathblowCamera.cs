using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// 인살 준비·실행 중 FreeCam의 고정 구면 궤도를 벗어나는 연출 카메라입니다.
/// 진입할 때 실제 화면 출력을 이어받고, 종료할 때 마지막 출력 방향을
/// FreeCam이 허용하는 가장 가까운 궤도 자세로 인계합니다.
/// </summary>
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class DeathblowCamera : MonoBehaviour
{
    [System.Serializable]
    private class DeathblowShot
    {
        public TimelineAsset Timeline;
        public AnimationCurve PushIn = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        public AnimationCurve Yaw = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }

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
    [SerializeField, Min(0f)] private float sideDistance = 1.8f;
    [Tooltip("두 캐릭터의 중심점보다 카메라를 올리는 높이")]
    [SerializeField] private float height = 0.45f;
    [Tooltip("피격자 쪽 시선 기준점의 높이")]
    [SerializeField, Min(0f)] private float victimLookHeight = 1.2f;
    [Tooltip("공격자 쪽 시선 기준점의 높이")]
    [SerializeField, Min(0f)] private float attackerLookHeight = 1.05f;
    [Tooltip("0은 공격자, 1은 피격자를 중심으로 프레이밍합니다.")]
    [SerializeField, Range(0f, 1f)] private float victimFramingWeight = 0.6f;

    [Header("카메라 움직임")]
    [SerializeField] private DeathblowShot[] shots = System.Array.Empty<DeathblowShot>();
    [SerializeField, Min(0f)] private float maximumPushIn = 0.75f;
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
    private Player player;
    private PlayerInput playerInput;
    private PlayerExecution execution;
    private Enemy preparedTarget;
    private Transform attacker;
    private Transform victim;
    private Transform[] attackerBody;
    private Transform[] victimBody;
    private float attackerBodyRadius;
    private float victimBodyRadius;
    private Animator attackerAnimator;
    private AnimatorCullingMode attackerCullingMode;
    private Animator victimAnimator;
    private AnimatorCullingMode victimCullingMode;
    private Weapon attackerWeapon;
    private Vector3 shotForward;
    private float shotSideSign = 1f;
    private bool active;
    private float startFieldOfView;
    private float executionFieldOfView;
    private DeathblowShot activeShot;

    private void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
        executionFieldOfView = cam.Lens.FieldOfView;
        cam.Priority = idlePriority;
    }

    private void OnEnable()
    {
        Player.OnLocalPlayerSpawned += BindPlayer;
        CombatFeedback.OnPostureBreak += BeginPreparation;
        CinemachineCore.BlendCreatedEvent.AddListener(HandleBlendCreated);
        BindPlayer(Player.LocalPlayer);
    }

    private void OnDisable()
    {
        Player.OnLocalPlayerSpawned -= BindPlayer;
        CombatFeedback.OnPostureBreak -= BeginPreparation;
        CinemachineCore.BlendCreatedEvent.RemoveListener(HandleBlendCreated);
        Unsubscribe();
        RestoreAnimationCulling();

        active = false;
        preparedTarget = null;
        activeShot = null;
        attackerWeapon = null;
        cam.Priority = idlePriority;
    }

    private void BindPlayer(Player localPlayer)
    {
        if (localPlayer == null || !localPlayer.IsLocalPlayer)
        {
            return;
        }

        Unsubscribe();
        player = localPlayer;
        playerInput = localPlayer.GetComponent<PlayerInput>();
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

    private void BeginPreparation(LivingEntity target, Vector3 point)
    {
        if (active || player == null || execution.IsExecuting
            || !CinemachineCore.IsLive(gameplayCamera.GetComponent<CinemachineCamera>()))
            return;

        Enemy enemy = target.GetComponent<Enemy>();
        if (!CanPrepare(enemy)) return;

        CameraState state = brain.State;
        Vector3 view = Quaternion.Inverse(state.GetFinalOrientation()) * (point - state.GetFinalPosition());
        float halfHeight = view.z * Mathf.Tan(state.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
        if (view.z <= 0f || Mathf.Abs(view.y) > halfHeight || Mathf.Abs(view.x) > halfHeight * state.Lens.Aspect)
            return;

        Vector3 forward = enemy.transform.position - player.transform.position;
        forward.y = 0f;
        preparedTarget = enemy;
        BeginShot(enemy.transform, forward.normalized);
    }

    private bool CanPrepare(Enemy enemy)
    {
        if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead || !enemy.IsDeathblowReady || player.Stats.IsDead
            || !(enemy.Stats.IsPostureBroken || enemy.Stats.IsHealthDepleted)
            || playerInput.currentActionMap?.name == PlayerInputHandler.UIActionMapName)
            return false;

        ITargetable currentTarget = player.TargetingSystem.CurrentTarget;
        if (currentTarget != null && !ReferenceEquals(currentTarget, enemy)) return false;

        Vector3 distance = enemy.transform.position - player.transform.position;
        distance.y = 0f;
        return distance.sqrMagnitude > 0.0001f
            && distance.sqrMagnitude <= execution.MaxExecutionDistance * execution.MaxExecutionDistance;
    }

    private void HandleBlendCreated(CinemachineCore.BlendEventParams data)
    {
        if (ReferenceEquals(data.Origin, brain) && ReferenceEquals(data.Blend.CamB, cam) && preparedTarget != null)
            data.Blend.Duration = 0f;
    }

    private void Begin(DeathblowPlan plan)
    {
        if (active)
            startFieldOfView = cam.Lens.FieldOfView;

        activeShot = GetShot(plan.Timeline);

        // 실제 시작 위치는 곧 정렬되므로, 정렬 전 attacker 위치가 아니라
        // DeathblowPlan의 최종 PlayerPose로 연출의 action axis를 고정한다.
        BeginShot(plan.Target.transform, plan.PlayerPose.rotation * Vector3.forward);
    }

    private DeathblowShot GetShot(TimelineAsset timeline)
    {
        foreach (DeathblowShot shot in shots)
            if (shot != null && shot.Timeline == timeline) return shot;
        return null;
    }

    private void BeginShot(Transform target, Vector3 forward)
    {
        attacker = execution.transform;
        victim = target;
        attackerWeapon = player.GetComponentInChildren<Weapon>();
        Enemy enemy = target.GetComponent<Enemy>();
        KeepAnimationUpdating(player.AnimatorController.Animator, ref attackerAnimator, ref attackerCullingMode);
        KeepAnimationUpdating(enemy.AnimationController.Animator, ref victimAnimator, ref victimCullingMode);
        attackerBody = GetBodyPoints(player.AnimatorController.Animator);
        victimBody = GetBodyPoints(enemy.AnimationController.Animator);
        attackerBodyRadius = player.Motor.Controller.radius * Mathf.Max(attacker.lossyScale.x, attacker.lossyScale.z);
        victimBodyRadius = enemy.Motor.BodyRadius;

        // 현재 Brain 출력을 복사하므로 우선순위가 바뀌는 첫 프레임에 점프하지 않는다.
        if (!active)
        {
            CameraState state = brain.State;
            transform.SetPositionAndRotation(state.GetFinalPosition(), state.GetFinalOrientation());
            startFieldOfView = state.Lens.FieldOfView;
            LensSettings lens = cam.Lens;
            lens.FieldOfView = startFieldOfView;
            cam.Lens = lens;
        }

        shotForward = forward;

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
        RestoreAnimationCulling();

        active = false;
        preparedTarget = null;
        activeShot = null;
        attackerWeapon = null;
        cam.Priority = idlePriority;
        attacker = null;
        victim = null;
    }

    private void LateUpdate()
    {
        if (!active)
        {
            if (player != null && !execution.IsExecuting && player.TargetingSystem.CurrentTarget is Enemy enemy && enemy.IsDeathblowReady)
                BeginPreparation(enemy.Stats, enemy.LockOnPoint.position);
            return;
        }

        bool preparing = !execution.IsExecuting;
        if (attacker == null || victim == null
            || (preparing && !CanPrepare(preparedTarget)))
        {
            End();
            return;
        }

        float deltaTime = preparing ? Time.unscaledDeltaTime : Time.deltaTime;
        Vector3 baseShotRight = Vector3.Cross(Vector3.up, shotForward) * shotSideSign;

        // 인살 준비·정렬 중에는 이전 재생 시간이 아닌 0을 사용한다.
        PlayableDirector director = execution.DeathblowDirector;
        float playTime = !preparing && director.state == PlayState.Playing ? (float)director.time : 0f;

        // Yaw 곡선은 양수 각도만 제공합니다. 진입한 쪽의 바깥 방향으로
        // 부호를 적용해야 회전 곡선이 action axis 반대편을 가로지르지 않는다.
        float yaw = -(activeShot?.Yaw.Evaluate(playTime) ?? 0f) * shotSideSign;
        Quaternion orbit = Quaternion.AngleAxis(yaw, Vector3.up);
        Vector3 currentShotForward = orbit * shotForward;
        Vector3 currentShotRight = orbit * baseShotRight;

        // 정렬 대기나 프레임 수가 아니라 실제 Timeline 진행에 맞춰 접근·복귀한다.
        float pushInWeight = preparing ? 0f : Mathf.Clamp01(activeShot?.PushIn.Evaluate(playTime) ?? 0f);
        Vector3 focus = GetFocusPoint(pushInWeight);
        Vector3 shotOffset = -currentShotForward * backDistance + currentShotRight * sideDistance + Vector3.up * height;
        float targetFov = Mathf.Lerp(startFieldOfView, executionFieldOfView, pushInWeight);
        shotOffset = Vector3.MoveTowards(shotOffset, Vector3.zero, maximumPushIn * pushInWeight);
        shotOffset = FitBodyInShot(shotOffset, focus, Mathf.Min(cam.Lens.FieldOfView, targetFov), pushInWeight);
        Vector3 desiredPosition = focus + shotOffset;

        float positionBlend = 1f - Mathf.Exp(-positionResponse * deltaTime);
        float rotationBlend = 1f - Mathf.Exp(-rotationResponse * deltaTime);
        float lensBlend = 1f - Mathf.Exp(-lensResponse * deltaTime);

        // 안전한 목적지를 먼저 만든 뒤 보간하면 중간 선분이 벽을 통과할 수 있다.
        // 이번 프레임의 보간 후보 자체를 충돌 검사한 뒤 최종 위치로 사용한다.
        Vector3 candidatePosition = Vector3.Lerp(transform.position, desiredPosition, positionBlend);
        transform.position = ResolveCollision(focus, candidatePosition);

        Vector3 lookDirection = focus - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);
        }

        LensSettings lens = cam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, lensBlend);
        cam.Lens = lens;
    }

    private Vector3 GetFocusPoint(float pushInWeight = 0f)
    {
        Vector3 attackerPoint = GetBodyFocusPoint(attacker, attackerBody, attackerLookHeight, pushInWeight);
        Vector3 victimPoint = GetBodyFocusPoint(victim, victimBody, victimLookHeight, pushInWeight);

        return Vector3.Lerp(attackerPoint, victimPoint, victimFramingWeight);
    }

    private Transform[] GetBodyPoints(Animator animator)
    {
        if (!animator.isHuman) return System.Array.Empty<Transform>();

        return new[]
        {
            animator.GetBoneTransform(HumanBodyBones.Hips), animator.GetBoneTransform(HumanBodyBones.Head),
            animator.GetBoneTransform(HumanBodyBones.LeftFoot), animator.GetBoneTransform(HumanBodyBones.RightFoot),
            animator.GetBoneTransform(HumanBodyBones.LeftHand), animator.GetBoneTransform(HumanBodyBones.RightHand)
        };
    }

    private void KeepAnimationUpdating(Animator animator, ref Animator trackedAnimator, ref AnimatorCullingMode originalMode)
    {
        if (trackedAnimator == animator) return;
        if (trackedAnimator != null) trackedAnimator.cullingMode = originalMode;
        trackedAnimator = animator;
        originalMode = animator.cullingMode;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void RestoreAnimationCulling()
    {
        if (attackerAnimator != null) attackerAnimator.cullingMode = attackerCullingMode;
        if (victimAnimator != null) victimAnimator.cullingMode = victimCullingMode;
        attackerAnimator = null;
        victimAnimator = null;
    }

    private Vector3 GetBodyFocusPoint(Transform root, Transform[] body, float lookHeight, float pushInWeight)
    {
        Vector3 point = body.Length > 0 ? body[0].position : root.position;
        point.y = root.position.y + lookHeight;
        if (body.Length > 0) point.y = Mathf.Min(point.y, body[1].position.y);
        if (body.Length > 0) point = Vector3.Lerp(point, Vector3.Lerp(body[0].position, body[1].position, 0.6f), pushInWeight);
        return point;
    }

    private Bounds GetBodyBounds(Transform root, Transform[] body, float lookHeight, float radius, float pushInWeight)
    {
        if (body.Length == 0)
            return new Bounds(root.position + Vector3.up * lookHeight,
                new Vector3(radius * 2f, lookHeight * Mathf.Lerp(2f, 1f, pushInWeight), radius * 2f));

        Bounds bounds = new Bounds(body[0].position, Vector3.zero);
        for (int i = 0; i < body.Length; i++)
        {
            if (body[i] == null) continue;

            Vector3 point = body[i].position;
            if (i == 2 || i == 3) point = Vector3.Lerp(point, body[0].position, pushInWeight);
            bounds.Encapsulate(point);
        }
        bounds.Expand(radius * Mathf.Lerp(2f, 1f, pushInWeight));
        return bounds;
    }

    private Vector3 FitBodyInShot(Vector3 shotOffset, Vector3 focus, float fieldOfView, float pushInWeight)
    {
        Quaternion view = Quaternion.Inverse(Quaternion.LookRotation(-shotOffset, Vector3.up));
        float verticalTan = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        float horizontalTan = verticalTan * brain.State.Lens.Aspect;
        Bounds attackerBounds = GetBodyBounds(attacker, attackerBody, attackerLookHeight, attackerBodyRadius, pushInWeight);
        Bounds victimBounds = GetBodyBounds(victim, victimBody, victimLookHeight, victimBodyRadius, pushInWeight);
        if (attackerWeapon != null) attackerBounds.Encapsulate(attackerWeapon.GetClosestBladePoint(GetBodyFocusPoint(victim, victimBody, victimLookHeight, pushInWeight)));
        float distance = shotOffset.magnitude;
        float framingDistance = Mathf.Max(GetFramingDistance(attackerBounds, focus, view, horizontalTan, verticalTan),
            GetFramingDistance(victimBounds, focus, view, horizontalTan, verticalTan));
        return shotOffset.normalized * Mathf.Max(distance, framingDistance);
    }

    private float GetFramingDistance(Bounds bounds, Vector3 focus, Quaternion view, float horizontalTan, float verticalTan)
    {
        float distance = 0f;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        for (int i = 0; i < 8; i++)
        {
            Vector3 point = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
            point = view * (point - focus);
            float depth = Mathf.Max(Mathf.Abs(point.x) / horizontalTan, Mathf.Abs(point.y) / verticalTan);
            distance = Mathf.Max(distance, Mathf.Max(depth, cam.Lens.NearClipPlane) - point.z);
        }
        return distance;
    }

    private Vector3 ResolveCollision(Vector3 focus, Vector3 desiredPosition)
    {
        Vector3 offset = desiredPosition - focus;
        float distance = offset.magnitude;
        if (distance <= 0.001f)
            return desiredPosition;

        int hitCount = Physics.SphereCastNonAlloc(focus, cameraRadius, offset / distance, collisionHits, distance, collisionLayers, QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = collisionHits[i];
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;
            bool hitAttacker = attacker != null && (hitTransform == attacker || hitTransform.IsChildOf(attacker));
            bool hitVictim = victim != null && (hitTransform == victim || hitTransform.IsChildOf(victim));
            if (hitAttacker || hitVictim)
            {
                continue;
            }

            nearestDistance = Mathf.Min(nearestDistance, hit.distance);
        }

        if (float.IsPositiveInfinity(nearestDistance))
            return desiredPosition;

        float safeDistance = Mathf.Max(0.1f, nearestDistance - collisionPadding);
        return focus + offset.normalized * safeDistance;
    }

}
