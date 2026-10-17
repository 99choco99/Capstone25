using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>보스의 마지막 인살에만 자동 진입, 입력 대기, 슬로모션을 적용합니다.</summary>
[RequireComponent(typeof(PlayerExecution), typeof(PlayerInput))]
[DisallowMultipleComponent]
public class BossDeathblowSequence : MonoBehaviour
{
    private enum Phase
    {
        None,
        Preparation,
        AwaitingInput,
        Finishing
    }

    [Header("붕괴에서 인살로 진입 (실제 시간)")]
    [SerializeField, Min(0.1f)] private float preparationDuration = 0.4f;
    [SerializeField, Range(0.01f, 1f)] private float preparationTimeScale = 0.35f;

    [Header("결정타 입력 대기 (실제 시간)")]
    [SerializeField, Min(0.1f)] private float inputWaitDuration = 2.2f;
    [SerializeField, Range(0.01f, 1f)] private float waitingTimeScale = 0.08f;
    [SerializeField] private SignalAsset impactSignal;
    [Tooltip("실제 타격 Signal보다 이 시간만큼 앞에서 대기 구간을 제한합니다.")]
    [SerializeField, Min(0.01f)] private float impactLeadTime = 0.05f;

    [Header("마무리")]
    [SerializeField, Range(0.01f, 1f)] private float finishingTimeScale = 0.82f;
    [SerializeField, Min(0f)] private float speedRecoveryDuration = 0.12f;

    public bool IsActive => phase != Phase.None;
    public bool IsAwaitingInput => phase == Phase.AwaitingInput;
    public Enemy Target { get; private set; }

    public float FramingWeight
    {
        get
        {
            if (!IsActive)
                return 0f;
            if (phase == Phase.Preparation)
                return 0.65f * Mathf.SmoothStep(0f, 1f, phaseTime / preparationDuration);
            if (phase == Phase.AwaitingInput)
                return 0.65f;
            PlayableDirector director = execution.DeathblowDirector;
            float progress = director.duration > 0d ? (float)(director.time / director.duration) : 1f;
            return 0.65f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 1f, progress)));
        }
    }

    private Player player;
    private PlayerExecution execution;
    private PlayerInput playerInput;
    private InputAction attackInput;
    private HitStopManager timeController;
    private Enemy pendingTarget;
    private Phase phase;
    private float phaseTime;
    private int promptStartFrame;
    private double gateTime;
    private double[] rootSpeeds;

    private void Awake()
    {
        player = GetComponent<Player>();
        execution = GetComponent<PlayerExecution>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        CombatFeedback.OnPostureBreak += QueueFinalDeathblow;
        execution.OnExecuteStart += Begin;
        execution.OnExecuteEnd += End;
    }

    private void OnDisable()
    {
        CombatFeedback.OnPostureBreak -= QueueFinalDeathblow;
        execution.OnExecuteStart -= Begin;
        execution.OnExecuteEnd -= End;
        try
        {
            if (IsActive)
                CancelAndEnd();
            else
                End();
        }
        finally
        {
            pendingTarget = null;
        }
    }

    private static bool IsFinalBoss(Enemy target)
    {
        return target != null && target.isActiveAndEnabled && !target.IsDead
            && target.Stats.CurrentLife == 1 && target.Stats.Data != null
            && target.Stats.Data.automaticFinalDeathblow;
    }

    private void QueueFinalDeathblow(LivingEntity entity, Vector3 point)
    {
        if (IsActive || !player.IsLocalPlayer || entity is not EnemyStats stats)
            return;
        Enemy target = stats.GetComponent<Enemy>();
        if (IsFinalBoss(target))
            pendingTarget = target;
    }

    private void Update()
    {
        if (!IsActive)
        {
            TryStartPending();
            return;
        }

        if (Target == null || !Target.isActiveAndEnabled || Target.IsDead || player.Stats.IsDead)
        {
            CancelAndEnd();
            return;
        }

        phaseTime += Time.unscaledDeltaTime;
        if (phase == Phase.Preparation && phaseTime >= preparationDuration)
        {
            phase = Phase.AwaitingInput;
            phaseTime = 0f;
            promptStartFrame = Time.frameCount;
            player.InputBuffer.Clear();
            timeController.SetCinematicTimeScale(this, waitingTimeScale);
        }

        if (phase == Phase.AwaitingInput
            && (phaseTime >= inputWaitDuration || Time.frameCount > promptStartFrame
                && attackInput != null && attackInput.WasPressedThisFrame()))
        {
            phase = Phase.Finishing;
            phaseTime = 0f;
            player.InputBuffer.Clear();
            RestoreTimelineSpeed();
        }

        if (phase == Phase.Finishing)
        {
            float blend = speedRecoveryDuration > 0f
                ? Mathf.SmoothStep(0f, 1f, phaseTime / speedRecoveryDuration) : 1f;
            timeController.SetCinematicTimeScale(this, Mathf.Lerp(waitingTimeScale, finishingTimeScale, blend));
        }
        else
        {
            LimitTimelineBeforeImpact();
        }
    }

    private void TryStartPending()
    {
        if (pendingTarget == null || !player.IsLocalPlayer || player.StateMachine == null)
            return;
        if (!IsFinalBoss(pendingTarget)
            || !(pendingTarget.Stats.IsPostureBroken || pendingTarget.Stats.IsHealthDepleted))
        {
            pendingTarget = null;
            return;
        }

        if (execution.IsExecuting || !pendingTarget.IsDeathblowReady || player.Stats.IsDead
            || player.Stats.IsHealthDepleted || player.Stats.IsPostureBroken || !player.Motor.IsGrounded
            || !playerInput.inputIsActive || playerInput.currentActionMap?.name != PlayerInputHandler.GameplayActionMapName)
            return;

        ITargetable selected = player.TargetingSystem.CurrentTarget;
        if (selected != null && !ReferenceEquals(selected, pendingTarget))
            return;
        if (!execution.CreateDeathblowPlan(pendingTarget, out DeathblowPlan plan)
            || !TryGetGateTime(plan.Timeline, out _) || HitStopManager.Instance == null)
            return;

        pendingTarget = null;
        player.InputBuffer.Clear();
        player.StateMachine.RequestedDeathblowPlan = plan;
        player.StateMachine.TransitionTo(player.StateMachine.PlayerExecuteState);
    }

    private void Begin(DeathblowPlan plan)
    {
        if (!IsFinalBoss(plan.Target) || !TryGetGateTime(plan.Timeline, out gateTime))
            return;
        timeController = HitStopManager.Instance;
        if (timeController == null || !timeController.BeginCinematic(this, preparationTimeScale))
            return;

        Target = plan.Target;
        pendingTarget = null;
        phase = Phase.Preparation;
        phaseTime = 0f;
        rootSpeeds = null;
        attackInput = playerInput.actions.FindAction("Player/Attack", false);
        player.InputBuffer.Clear();
    }

    private bool TryGetGateTime(TimelineAsset timeline, out double time)
    {
        double firstImpact = double.PositiveInfinity;
        if (impactSignal != null)
        {
            foreach (TrackAsset track in timeline.GetRootTracks())
                FindImpact(track, ref firstImpact);
        }
        time = firstImpact - impactLeadTime;
        return !double.IsInfinity(firstImpact) && time > 0d;
    }

    private void FindImpact(TrackAsset track, ref double firstImpact)
    {
        foreach (IMarker marker in track.GetMarkers())
            if (marker is SignalEmitter signal && signal.asset == impactSignal)
                firstImpact = Math.Min(firstImpact, marker.time);
        foreach (TrackAsset child in track.GetChildTracks())
            FindImpact(child, ref firstImpact);
    }

    private void LimitTimelineBeforeImpact()
    {
        PlayableDirector director = execution.DeathblowDirector;
        PlayableGraph graph = director.playableGraph;
        if (director.state != PlayState.Playing || !graph.IsValid())
            return;
        int count = graph.GetRootPlayableCount();
        if (rootSpeeds == null)
        {
            rootSpeeds = new double[count];
            for (int i = 0; i < count; i++)
                rootSpeeds[i] = graph.GetRootPlayable(i).GetSpeed();
        }

        // 대기 시간이 길거나 프레임이 늦어져도 타격 Signal을 먼저 넘기지 않습니다.
        double remaining = Math.Max(0d, gateTime - director.time);
        // 짧은 원본 모션도 대기 중 계속 움직이도록 남은 동작을 실제 대기 시간에 나눕니다.
        float remainingGameTime = phase == Phase.Preparation
            ? Mathf.Max(0f, preparationDuration - phaseTime) * preparationTimeScale + inputWaitDuration * waitingTimeScale
            : Mathf.Max(0f, inputWaitDuration - phaseTime) * waitingTimeScale;
        for (int i = 0; i < count; i++)
        {
            float availableTime = Mathf.Max(Time.deltaTime, remainingGameTime);
            double maximumSpeed = availableTime > 0f ? remaining / availableTime : rootSpeeds[i];
            graph.GetRootPlayable(i).SetSpeed(Math.Min(rootSpeeds[i], maximumSpeed));
        }
    }

    private void RestoreTimelineSpeed()
    {
        PlayableGraph graph = execution.DeathblowDirector.playableGraph;
        if (rootSpeeds != null && graph.IsValid())
            for (int i = 0; i < Math.Min(rootSpeeds.Length, graph.GetRootPlayableCount()); i++)
                graph.GetRootPlayable(i).SetSpeed(rootSpeeds[i]);
        rootSpeeds = null;
    }

    private void CancelAndEnd()
    {
        try
        {
            execution.CancelDeathblow();
        }
        finally
        {
            End();
        }
    }

    private void End()
    {
        RestoreTimelineSpeed();
        if (timeController != null)
            timeController.EndCinematic(this);
        if (IsActive)
            player.InputBuffer.Clear();
        Target = null;
        phase = Phase.None;
        phaseTime = 0f;
        timeController = null;
    }
}
