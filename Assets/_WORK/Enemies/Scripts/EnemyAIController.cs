using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI의 전투 모드
/// </summary>
public enum EnemyTacticalMode
{
    Idle,
    Chase,
    Engage
}

/// <summary>AI의 의도 종류</summary>
public enum EnemyIntentType
{
    HoldPosition,
    Chase,
    Space,
    Strafe,
    Attack,
    Guard
}

/// <summary>
/// AI의 의도 정보
/// </summary>
public readonly struct EnemyIntent
{
    public EnemyIntentType Type { get; }
    public Vector3 TargetPosition { get; }
    public EnemyAttackData AttackData { get; }
    public DefenseType Defense { get; }

    private EnemyIntent(
        EnemyIntentType type,
        Vector3 targetPosition = default,
        EnemyAttackData attack = null,
        DefenseType defense = DefenseType.None)
    {
        Type = type;
        TargetPosition = targetPosition;
        AttackData = attack;
        Defense = defense;
    }

    public static EnemyIntent Hold() => new(EnemyIntentType.HoldPosition);
    public static EnemyIntent Chase(Vector3 position) => new(EnemyIntentType.Chase, position);
    public static EnemyIntent Space(Vector3 position) => new(EnemyIntentType.Space, position);
    public static EnemyIntent Strafe(Vector3 position) => new(EnemyIntentType.Strafe, position);
    public static EnemyIntent Attack(EnemyAttackData attack) => new(EnemyIntentType.Attack, default, attack);
    public static EnemyIntent Guard(DefenseType defense) => new(EnemyIntentType.Guard, default, null, defense);
}

/// <summary>
/// 다음 행동을 결정하는 AI 두뇌
/// </summary>
[RequireComponent(typeof(EnemyAttackObserver))]
public class EnemyAIController : MonoBehaviour
{
    private enum CombatStep
    {
        Engage,
        Space,
        Observe
    }

    private readonly struct RecentAttack
    {
        public readonly EnemyAttackData Attack;
        public readonly EnemyAttackRole Role;

        public RecentAttack(EnemyAttackData attack)
        {
            Attack = attack;
            Role = attack.PatternRole;
        }
    }

    private const int LongComboHitCount = 3;

    private EnemyAttackObserver attackObserver;
    private EnemyStats stats;
    private EnemyMotor motor;

    private EnemyData data;
    private readonly EnemyExchange exchange = new();
    private readonly EnemyCombatDistance combatDistance = new();
    public float ObservationDistance => combatDistance.ObservationDistance;
    public bool UsesMeasuredSpacing => combatDistance.UsesMeasuredReach;
    private bool UsesExchange => data.exchange != null && data.exchange.enabled;
    private bool UsesAttackPolicy => data.attackPolicy != null && data.attackPolicy.UsesReferenceDecisions;
    private readonly EnemyReferenceInput referenceInput = new();
    private readonly int[] referenceWeights = new int[EnemyReferenceDecision.BudgetSize];
    private EnemyAttackPolicy.ReferenceBinding selectedReferenceBinding;
    private EnemyAttackData selectedReferenceAttack;
    private bool referenceRetreatInProgress;
    private int reboundReferenceEvent = 200200;
    public EnemyReferenceChoice LastReferenceChoice { get; private set; }
    public EnemyReferenceMapping LastReferenceMapping { get; private set; }
    public string LastReferenceAdaptation { get; private set; }
    private bool isReboundSelection;
    private readonly Dictionary<int, float> policyActionTimes = new();
    private bool hasChosenObservation;
    private float nextDefenseDecisionTime = float.NegativeInfinity;
    private float counterContactTime;
    private float policyCounterStartTime = float.NegativeInfinity;
    public float DefenseDecisionReadyTime { get; private set; }

    public EnemyTacticalMode TacticalMode { get; private set; } = EnemyTacticalMode.Idle;
    public float StrafeDistance =>
        Time.time < responseOpportunityEndTime ? recoverySpacingDistance
        : exchange.HasPlan ? exchange.GetWaitingDistance(ObservationDistance)
        : combatStep != CombatStep.Engage || approachAttack == null ? ObservationDistance
        : GetApproachTargetDistance(approachAttack);
    public float ChaseStoppingDistance => approachAttack != null
        ? GetApproachTargetDistance(approachAttack) : data.combatRange;
    public EnemyAttackData ReservedAttack => approachAttack;
    public float PreparationTimeRemaining => approachAttack != null ? Mathf.Max(0f, approachDeadline - Time.time) : 0f;
    public bool IsPreparingRetreat { get; private set; }
    public bool IsBurstApproach => approachAttack != null
        && (IsGapClosingAttack(approachAttack)
        || isRetreatReentryApproach);
    public EnemyAttackData LastStartedPattern { get; private set; }

    /// <summary>
    /// 현재 가능한 공격들
    /// </summary>
    private readonly List<EnemyAttackData> validAttacks = new();
    private readonly List<float> validAttackWeights = new();
    private readonly List<float> referenceFallbackScores = new();
    private readonly List<int> referenceCandidateIndices = new();
    private readonly Dictionary<EnemyAttackData, float> coolTimes = new();
    private readonly Dictionary<EnemyAttackData, float> failedPreparations = new();
    private readonly List<RecentAttack> recentAttacks = new();
    private int consecutiveMotionUses;
    private readonly Dictionary<EnemyAttackRole, int> executedPatternCounts = new();
    private readonly Queue<float> recentPlayerAttacks = new();
    private EnemyAttackData lastAttack;
    private EnemyAttackData approachAttack;
    private EnemyAttackData closeRangeFallbackAttack;
    private float approachDistanceError;
    private float approachProgressTime;
    private float approachDeadline;
    private float preparedRetreatDistance;
    private EnemyAttackData queuedRetreatReentry;
    private float retreatObservationDuration;
    private float retreatReentryDeadline;
    private bool isRetreatReentryApproach;
    private EnemyAttackContext attackContext = EnemyAttackContext.Neutral;
    private CombatStep combatStep;
    private float combatStepStartTime;
    private float observationSpacingDeadline;
    private bool hasRespondedToCloseRange;
    private Vector3 observationDirection;
    private Vector3 observationTargetPosition;
    private float observationDuration;

    private float attackTimer;
    private bool IsAttackReady => Time.time >= attackTimer;
    private float responseOpportunityEndTime = float.NegativeInfinity;
    private float recoverySpacingDistance;

    // 같은 공격을 중복 판단하지 않기 위한 식별자
    private int lastAttackVersion = -1;
    private DefenseType pendingDefense;
    private float pendingDefenseTime;
    private bool hasPendingDefense;
    private int recentGuardCount;
    private float lastGuardTime;
    private Player guardTarget;
    private int passiveParryCount;
    private int lastWhiffVersion = -1;
    private float lastWhiffPunishTime = float.NegativeInfinity;
    private float navigationRetryTime;
    private Vector3 failedNavigationTarget;

    private bool counterReserved;
    public bool canCounter
    {
        get => counterReserved;
        set
        {
            counterReserved = value;
            if (value)
            {
                exchange.Reset();
                queuedRetreatReentry = null;
                attackContext = EnemyAttackContext.AfterDeflect;
                SetApproachAttack(null);
                combatStep = CombatStep.Engage;
                return;
            }
            if (attackContext != EnemyAttackContext.AfterDeflect)
                return;

            attackContext = EnemyAttackContext.Neutral;
            SetApproachAttack(null);
        }
    }
    private float counterDelayTime;
    public bool IsCounterAttack { get; private set; }

    private bool NeedsPostureRecovery => stats.CurrentPosture >= stats.MaxPosture.GetValue() * data.counterRecoveryPostureRatio;

    //============ 초기화와 전투 문맥 정리 ============

    private void Awake()
    {
        attackObserver = GetComponent<EnemyAttackObserver>();
        stats = GetComponent<EnemyStats>();
        motor = GetComponent<EnemyMotor>();
        data = stats.Data;
        combatDistance.Refresh(data, transform, Player.LocalPlayer);
    }

    private void OnDisable()
    {
        ResetCombatContext();
        recentAttacks.Clear();
        consecutiveMotionUses = 0;
        executedPatternCounts.Clear();
        policyActionTimes.Clear();
        referenceInput.Reset();
        LastReferenceChoice = default;
        LastReferenceMapping = EnemyReferenceMapping.None;
        LastReferenceAdaptation = null;
        recentPlayerAttacks.Clear();
        LastStartedPattern = null;
        lastWhiffVersion = -1;
        lastWhiffPunishTime = float.NegativeInfinity;
        navigationRetryTime = 0f;
    }

    public void ResetCombatContext()
    {
        exchange.Reset();
        isReboundSelection = false;
        referenceInput.ResetExchange();
        selectedReferenceBinding = null;
        selectedReferenceAttack = null;
        referenceRetreatInProgress = false;
        nextDefenseDecisionTime = policyCounterStartTime = float.NegativeInfinity;
        hasChosenObservation = false;


        queuedRetreatReentry = null;
        hasRespondedToCloseRange = false;
        passiveParryCount = 0;
        ResetGuardPressure();
        combatStep = CombatStep.Engage;
        canCounter = false;
        IsCounterAttack = false;
        attackContext = EnemyAttackContext.Neutral;
        SetApproachAttack(null);
        hasPendingDefense = false;
        lastAttackVersion = -1;
        responseOpportunityEndTime = float.NegativeInfinity;
    }

    public void FinishHitReaction()
    {
        ResetCombatContext();
        ResetActionTimer();
    }

    //============ 행동 의도 선택 ============

    /// <summary>
    /// 현재 감지 값을 읽고 이번 프레임의 행동 의도를 반환
    /// </summary>
    public EnemyIntent SelectIntent(in EnemyTargetInfo perception, bool canStrafeWhileWaiting = true)
    {
        float navigationTolerance = Mathf.Max(0.5f, data.distanceTolerance);
        bool waitingForNavigation = Time.time < navigationRetryTime
            && (perception.TargetPosition - failedNavigationTarget).sqrMagnitude <= navigationTolerance * navigationTolerance;
        if (!perception.HasTarget)
        {
            stats.ReleasePlayerLevel();
            ResetCombatContext();
            SetTacticalMode(EnemyTacticalMode.Idle);

            return EnemyIntent.Hold();
        }

        if (!perception.CanSeeTarget)
        {
            ResetCombatContext();
            SetTacticalMode(EnemyTacticalMode.Chase);
            return waitingForNavigation ? EnemyIntent.Hold() : EnemyIntent.Chase(perception.TargetPosition);
        }

        stats.CapturePlayerLevel();
        combatDistance.Refresh(data, transform, Player.LocalPlayer);
        UpdateTacticalModeByDistance(perception.Distance);

        if (approachAttack != null
            && !isRetreatReentryApproach
            && !SupportsSelectionContext(
            approachAttack,
            attackContext))
            SetApproachAttack(null);

        // 대응에 성공한 상대의 공격 준비를 기다리는 동안에는 현재 간격에서 횡이동합니다.
        if (Time.time < responseOpportunityEndTime)
        {
            recoverySpacingDistance = perception.Distance;
            return canStrafeWhileWaiting ? EnemyIntent.Strafe(perception.TargetPosition) : EnemyIntent.Hold();
        }

        //반격
        if (canCounter)
        {
            if (Time.time < counterDelayTime
                || !UsesAttackPolicy && Player.LocalPlayer?.StateMachine?.CurrentState is PlayerAttackState { IsRebounding: true })
                return EnemyIntent.Hold();

            return SelectDeflectResponse(perception);
        }

        if (UsesAttackPolicy && approachAttack != null && Time.time < policyCounterStartTime)
            return canStrafeWhileWaiting ? EnemyIntent.Strafe(perception.TargetPosition) : EnemyIntent.Hold();

        if (canStrafeWhileWaiting && TryGetWhiffIntent(perception, out EnemyIntent whiffIntent))
            return whiffIntent;

        // 방어판단 우선
        if (GetDefenseIntent(out EnemyIntent defenseIntent))
        {
            exchange.Reset();
            queuedRetreatReentry = null;
            if (combatStep != CombatStep.Engage)
                combatStepStartTime = Time.time;
            if (attackContext == EnemyAttackContext.AfterDeflectApproach
                || attackContext == EnemyAttackContext.AfterDeflectFollowup)
                attackContext = EnemyAttackContext.Neutral;
            SetApproachAttack(null);
            return defenseIntent;
        }

        if (waitingForNavigation)
            return EnemyIntent.Hold();

        if (TryGetExchangeIntent(perception, canStrafeWhileWaiting, out EnemyIntent exchangeIntent))
            return exchangeIntent;

        return SelectEngagementIntent(perception, canStrafeWhileWaiting);
    }

    private EnemyIntent SelectEngagementIntent(in EnemyTargetInfo perception, bool canStrafeWhileWaiting)
    {
        // 근접 공방과 원거리 진입은 관찰보다 우선합니다. 공격/반격의 대기 시간은 유지합니다.
        if (perception.Distance > data.closePressureDistance + data.distanceTolerance)
            hasRespondedToCloseRange = false;
        if (queuedRetreatReentry == null && !IsPreparingRetreat && !NeedsPostureRecovery)
        {
            Vector3 playerMovement = perception.TargetPosition - observationTargetPosition;
            playerMovement.y = 0f;
            bool playerClosed = Vector3.Dot(playerMovement, observationDirection.normalized)
                >= (data.exchange?.closingInterruptDistance ?? data.distanceTolerance);
            if (perception.Distance <= data.closePressureDistance && (combatStep == CombatStep.Engage || playerClosed))
            {
                combatStep = CombatStep.Engage;
                if (!hasRespondedToCloseRange
                    && approachAttack != null
                    && approachAttack != closeRangeFallbackAttack
                    && approachAttack.PatternRole != EnemyAttackRole.Retreat
                    && perception.Distance < approachAttack.StartMinDistance - data.distanceTolerance)
                    SetApproachAttack(null);
                hasRespondedToCloseRange = true;
                if (!IsAttackReady)
                    return EnemyIntent.Hold();
            }
            else if (combatStep != CombatStep.Engage && perception.Distance >= data.distantAttackDistance
                && IsAttackReady && (perception.Distance > ObservationDistance + data.observationDistanceTolerance
                    || combatStep == CombatStep.Observe && Time.time - combatStepStartTime >= observationDuration))
            {
                combatStep = CombatStep.Engage;
                // 관찰을 끝낼 때 한 번 재선택합니다. 접근 중에는 예약/제한 시간을 유지합니다.
                SetApproachAttack(null);
            }
        }

        if (combatStep == CombatStep.Engage && queuedRetreatReentry == null && !NeedsPostureRecovery
            && perception.Distance >= data.distantAttackDistance)
        {
            if (approachAttack == null)
                SetApproachAttack(ChooseAttack(perception.Distance, attackContext, allowApproach: true));
            if (!IsAttackReady && approachAttack != null && IsGapClosingAttack(approachAttack))
                return perception.Distance > approachAttack.StartMaxDistance
                    ? EnemyIntent.Chase(perception.TargetPosition) : EnemyIntent.Hold();
        }

        if (combatStep != CombatStep.Engage)
        {
            if (queuedRetreatReentry != null && Time.time >= retreatReentryDeadline)
                queuedRetreatReentry = null;
            bool spacingTimedOut = Time.time >= observationSpacingDeadline;
            bool tooClose = perception.Distance < ObservationDistance - data.observationDistanceTolerance;
            if (combatStep == CombatStep.Observe && tooClose)
                combatStep = CombatStep.Space;

            if (combatStep == CombatStep.Space)
            {
                EnemyAttackData retreat = data.retreatAttack;
                if (!spacingTimedOut && !attackObserver.IsPlayerAttacking && retreat != null
                    && NeedsPostureRecovery
                    && retreat.SupportsContext(attackContext) && retreat.IsInStartRange(perception.Distance) && CanRetreat()
                    && (!coolTimes.TryGetValue(retreat, out float cooldown) || cooldown <= Time.time))
                    return BeginAttack(retreat);

                if (perception.Distance >= ObservationDistance - Mathf.Max(
                    data.arrivalTolerance,
                    data.observationDistanceTolerance * 0.5f))
                    StartObservation(perception.TargetPosition);
                else if (!spacingTimedOut && !attackObserver.IsPlayerAttacking)
                    return EnemyIntent.Space(perception.TargetPosition);
                else if (!IsAttackReady)
                    return EnemyIntent.Hold();
                // 후퇴가 막히거나 상대가 계속 붙으면 관찰을 끝내고 다음 공격을 판단합니다.
            }

            if (combatStep == CombatStep.Observe)
            {
                float minimumObservation = queuedRetreatReentry != null
                    ? retreatObservationDuration
                    : observationDuration;
                if (!IsAttackReady || Time.time - combatStepStartTime < minimumObservation)
                    return EnemyIntent.Strafe(perception.TargetPosition);
            }

            bool observingRetreat = queuedRetreatReentry != null;
            combatStep = CombatStep.Engage;

            if (observingRetreat)
            {
                EnemyAttackData reentry = queuedRetreatReentry;
                queuedRetreatReentry = null;
                SetApproachAttack(reentry);
                isRetreatReentryApproach = true;
                approachDeadline = Mathf.Min(approachDeadline, retreatReentryDeadline);
            }
            else if (approachAttack != null)
            {
                // 관찰에 쓴 시간은 선택한 공격의 거리 준비 제한 시간에서 제외합니다.
                SetApproachAttack(approachAttack);
            }
        }

        //공격
        // 추적 중에도 사거리가 긴 공격이 등록되어 있으면 사용 가능
        if (IsAttackReady)
        {
            if (approachAttack == null)
                SetApproachAttack(ChooseAttack(perception.Distance, attackContext, allowApproach: true));

            if (approachAttack != null)
                return GetApproachIntent(perception);
            if (exchange.Step == EnemyExchangeStep.Observe)
                return canStrafeWhileWaiting ? EnemyIntent.Strafe(perception.TargetPosition) : EnemyIntent.Hold();
        }

        if (!IsAttackReady && TacticalMode == EnemyTacticalMode.Engage)
        {
            if (!canStrafeWhileWaiting)
                return EnemyIntent.Hold();
            BeginObservation(perception.TargetPosition);
            return combatStep == CombatStep.Space
                ? EnemyIntent.Space(perception.TargetPosition)
                : EnemyIntent.Strafe(perception.TargetPosition);
        }

        //추격
        if (TacticalMode == EnemyTacticalMode.Chase)
            return EnemyIntent.Chase(perception.TargetPosition);

        return EnemyIntent.Hold();
    }

    //============ 거리별 전술 모드 ============

    /// <summary>
    /// 거리에 따라 전투 모드 전환
    /// </summary>
    private void UpdateTacticalModeByDistance(float distance)
    {
        if (TacticalMode == EnemyTacticalMode.Idle)
        {
            SetTacticalMode(distance <= data.combatRange ? EnemyTacticalMode.Engage : EnemyTacticalMode.Chase);
            return;
        }

        if (TacticalMode == EnemyTacticalMode.Chase && distance <= data.combatRange)
        {
            SetTacticalMode(EnemyTacticalMode.Engage);
            return;
        }

        if (TacticalMode == EnemyTacticalMode.Engage && distance > data.combatRange * data.combatExitRangeMultiplier)
        {
            SetTacticalMode(EnemyTacticalMode.Chase);
        }
    }

    /// <summary>
    /// 전투 모드 설정
    /// </summary>
    private void SetTacticalMode(EnemyTacticalMode nextMode)
    {
        if (TacticalMode == nextMode)
            return;

        TacticalMode = nextMode;
        if (combatStep == CombatStep.Engage && approachAttack == null && !exchange.HasPlan)
            ResetActionTimer();
    }

    //============ 방어 판단과 기록 ============

    /// <summary>
    /// 방어의도를 가져오기
    /// </summary>
    private bool GetDefenseIntent(out EnemyIntent intent)
    {
        intent = default;

        bool isCommittedResponse = approachAttack != null && Time.time < approachDeadline
            && (attackContext is EnemyAttackContext.AfterDeflectApproach or EnemyAttackContext.AfterDeflectFollowup
                || UsesAttackPolicy && attackContext == EnemyAttackContext.AfterDeflect);
        if (Time.time < responseOpportunityEndTime || isCommittedResponse)
        {
            hasPendingDefense = false;
            return false;
        }

        if (!attackObserver.IsPlayerAttacking)
        {
            hasPendingDefense = false;
            return false;
        }

        if (!attackObserver.IsAttackInRange())
            return false;

        int version = attackObserver.curAttackVersion;
        if (version != lastAttackVersion)
        {
            pendingDefense = GetDefenseDecision(version);
            pendingDefenseTime = Mathf.Max(attackObserver.DefenseStartTime,
                Mathf.Max(
                    DefenseDecisionReadyTime,
                    attackObserver.AttackStartedTime + Mathf.Max(0f, data.defenseReactionTime)));
            hasPendingDefense = pendingDefense != DefenseType.None;
        }

        if (!hasPendingDefense)
            return false;

        if (Time.time < pendingDefenseTime)
        {
            intent = EnemyIntent.Hold();
            return true;
        }

        hasPendingDefense = false;
        intent = EnemyIntent.Guard(pendingDefense);
        return true;
    }

    /// <summary>
    /// 같은 AttackVersion에는 항상 같은 결정
    /// </summary>
    public DefenseType GetDefenseDecision(int attackVersion)
    {
        UpdateGuardTarget();
        if (attackVersion == lastAttackVersion)
            return pendingDefense;

        lastAttackVersion = attackVersion;
        DefenseDecisionReadyTime = UsesAttackPolicy ? Mathf.Max(Time.time, nextDefenseDecisionTime) : Time.time;
        nextDefenseDecisionTime = DefenseDecisionReadyTime + (UsesAttackPolicy
            ? data.attackPolicy.defenseDecisionInterval
            : 0f);
        recentPlayerAttacks.Enqueue(Time.time);
        TrimPlayerAttackMemory();
        pendingDefense = RollDefenseType();
        return pendingDefense;
    }

    /// <summary>
    /// 공격 하나에 대한 방어 결과를 확률적으로 판정
    /// </summary>
    private DefenseType RollDefenseType()
    {
        if (Time.time >= lastGuardTime + CombatSettings.Current.EnemyGuardPressureMemoryDuration)
            ResetGuardPressure();

        float totalDefenseChance = data.parryChance + data.blockChance;
        float parryChance = Mathf.Min(
            totalDefenseChance,
            data.parryChance + recentGuardCount * data.guardPressureParryIncrement);
        int roll = Random.Range(1, 101);

        if (roll <= parryChance * 100f)
            return DefenseType.Parry;

        if (roll <= totalDefenseChance * 100f)
            return DefenseType.NormalGuard;

        return DefenseType.None;
    }

    public void RecordDefenseResult(DefenseType defense, float contactSide = 0f)
    {
        exchange.Reset();
        UpdateGuardTarget();
        if (UsesAttackPolicy)
        {
            // 이 프로젝트의 접촉 좌우를 원본 이벤트 쌍에 대응합니다. 원본 TAE 이벤트와 동일하다는 뜻은 아닙니다.
            referenceInput.Event = defense == DefenseType.Parry ? contactSide > 0f ? 200211 : 200210
                : defense == DefenseType.NormalGuard ? contactSide > 0f ? 200216 : 200215 : 0;
            if (defense == DefenseType.None)
                referenceInput.ResetExchange();
        }
        isReboundSelection = false;
        if (defense == DefenseType.Parry)
            passiveParryCount++;
        else if (defense == DefenseType.None)
            passiveParryCount = 0;
        if (defense == DefenseType.NormalGuard)
        {
            if (Time.time >= lastGuardTime + CombatSettings.Current.EnemyGuardPressureMemoryDuration)
                ResetGuardPressure();
            recentGuardCount++;
            lastGuardTime = Time.time;
        }
        else
            ResetGuardPressure();

        canCounter = false;
        SetApproachAttack(null);
        attackContext = defense == DefenseType.NormalGuard ? EnemyAttackContext.AfterGuard : EnemyAttackContext.Neutral;
    }

    private void UpdateGuardTarget()
    {
        if (guardTarget == Player.LocalPlayer)
            return;

        guardTarget = Player.LocalPlayer;
        passiveParryCount = 0;
        lastWhiffVersion = -1;
        lastWhiffPunishTime = float.NegativeInfinity;
        recentPlayerAttacks.Clear();
        isReboundSelection = false;
        referenceInput.ResetExchange();
        ResetGuardPressure();
        lastAttackVersion = -1;
        hasPendingDefense = false;
    }

    private void ResetGuardPressure()
    {
        recentGuardCount = 0;
        lastGuardTime = 0f;
    }

    //============ 반격과 빈틈 대응 ============

    /// <summary>GuardState가 패링에 성공했을 때 호출. 반격 여부 결정</summary>
    public void DecideCounterAttack()
    {
        canCounter = false;
        attackContext = EnemyAttackContext.Neutral;
        SetApproachAttack(null);
        canCounter = Random.value <= data.counterChance;
        if (canCounter)
        {
            CombatSettings settings = CombatSettings.Current;
            counterContactTime = Time.time;
            if (UsesAttackPolicy)
            {
                counterDelayTime = Time.time + data.attackPolicy.parryReactionDuration;
                return;
            }
            float defenseReadyDelay = settings.PlayerParriedRecoveryDuration + settings.CounterDefenseMargin;
            counterDelayTime = Time.time + Mathf.Max(
                data.counterDelay,
                Mathf.Max(settings.ParriedRecoveryDuration, defenseReadyDelay));
        }
    }

    private EnemyIntent SelectDeflectResponse(in EnemyTargetInfo perception)
    {
        if (UsesAttackPolicy && TryBeginPolicyCounter(perception, out EnemyIntent policyIntent))
            return policyIntent;
        bool shouldRecover = NeedsPostureRecovery;
        bool mustRespond = passiveParryCount >= Mathf.Max(1, data.maximumPassiveParries);
        bool hasPostureRoom = stats.CurrentPosture < stats.MaxPosture.GetValue() * data.counterRecoveryPostureRatio;
        float followupChance = GetCounterFollowupChance();
        if (!mustRespond && hasPostureRoom && followupChance > 0f && Random.value < followupChance
            && TryBeginCounterFollowup(perception, out EnemyIntent followupIntent))
            return followupIntent;

        if (shouldRecover && !mustRespond && TryBeginDeflectRetreat(perception, out EnemyIntent retreatIntent))
            return retreatIntent;

        if (!shouldRecover || mustRespond)
        {
            bool preferSingleAttack = !attackObserver.IsPlayerAttacking && !attackObserver.IsPlayerGuarding
                && (lastAttack == null || !IsSingleAttack(lastAttack));
            EnemyAttackData counter = ChooseAttack(
                perception.Distance,
                EnemyAttackContext.AfterDeflect,
                isCounter: true,
                preferSingleAttack: preferSingleAttack);
            if (counter != null)
                return BeginAttack(counter);
        }

        canCounter = false;
        if (!shouldRecover || mustRespond)
        {
            if (TryBeginCounterFollowup(perception, out EnemyIntent followupFallback))
                return followupFallback;
            attackContext = EnemyAttackContext.AfterDeflectApproach;
            SetApproachAttack(ChooseAttack(perception.Distance, attackContext, allowApproach: true));
            if (approachAttack != null)
                return GetApproachIntent(perception);
        }

        attackContext = EnemyAttackContext.Neutral;
        BeginRecovery(perception);
        ResetActionTimer();
        return combatStep == CombatStep.Observe
            ? EnemyIntent.Strafe(perception.TargetPosition)
            : EnemyIntent.Space(perception.TargetPosition);
    }

    private bool TryBeginPolicyCounter(in EnemyTargetInfo perception, out EnemyIntent intent)
    {
        intent = default;
        EnemyAttackContext contexts = EnemyAttackContext.AfterDeflect
            | EnemyAttackContext.AfterDeflectApproach
            | EnemyAttackContext.AfterDeflectFollowup;
        EnemyAttackData attack = ChooseAttack(
            perception.Distance,
            contexts,
            allowApproach: true,
            preferInRange: true,
            allowObservation: false);
        if (attack == null)
        {
            if (!UsesAttackPolicy)
                return false;
            canCounter = false;
            intent = EnemyIntent.Strafe(perception.TargetPosition);
            return true;
        }
        // 예약 해제는 문맥과 다음 공격을 정하기 전에 끝냅니다.
        canCounter = false;
        attackContext = attack.SupportsContext(EnemyAttackContext.AfterDeflectFollowup)
            ? EnemyAttackContext.AfterDeflectFollowup
            : attack.SupportsContext(EnemyAttackContext.AfterDeflectApproach)
            ? EnemyAttackContext.AfterDeflectApproach
            : EnemyAttackContext.AfterDeflect;
        SetApproachAttack(attack);
        // 원작 전환 시간 이후 준비를 시작하되, 우리 빠른 모션이 상대의 방어 복귀보다 먼저 맞지는 않게 합니다.
        float defenseReady = counterContactTime + CombatSettings.Current.PlayerParriedRecoveryDuration + CombatSettings.Current.CounterDefenseMargin;
        policyCounterStartTime = Mathf.Max(Time.time, defenseReady - EstimateAttackStartup(attack));
        attackTimer = policyCounterStartTime;
        intent = Time.time < policyCounterStartTime ? EnemyIntent.Hold() : GetApproachIntent(perception);
        return true;
    }

    private bool TryBeginDeflectRetreat(in EnemyTargetInfo perception, out EnemyIntent intent)
    {
        intent = default;
        EnemyAttackData retreat = data.retreatAttack;
        if (retreat == null || retreat.HasHitWindow || retreat.SelectionWeight <= 0f
            || !retreat.SupportsContext(EnemyAttackContext.Neutral) || !retreat.IsInStartRange(perception.Distance) || !CanRetreat()
            || coolTimes.TryGetValue(retreat, out float cooldown) && cooldown > Time.time
            || failedPreparations.TryGetValue(retreat, out float retryTime) && retryTime > Time.time)
            return false;

        intent = BeginAttack(retreat);
        return true;
    }

    private bool TryGetWhiffIntent(in EnemyTargetInfo perception, out EnemyIntent intent)
    {
        intent = default;
        if (data.whiffPunishChance <= 0f || NeedsPostureRecovery
            || Time.time < lastWhiffPunishTime + data.whiffPunishCooldown
            || attackObserver.curAttackVersion == lastWhiffVersion
            || !attackObserver.TryGetWhiffOpportunity(out float elapsed, out float remaining)
            || elapsed < data.whiffReactionTime)
            return false;

        // 같은 헛공격을 매 프레임 다시 추첨하지 않습니다. 다음 공격에는 새 판단을 합니다.
        lastWhiffVersion = attackObserver.curAttackVersion;
        if (Random.value > data.whiffPunishChance)
            return false;

        EnemyAttackData attack = ChooseWhiffAttack(perception.Distance, remaining);
        if (attack == null)
            return false;

        lastWhiffPunishTime = Time.time;
        exchange.Reset();
        hasPendingDefense = false;
        intent = BeginAttack(attack);
        return true;
    }

    private float GetCounterFollowupChance()
    {
        if (data.counterFollowupChance <= 0f)
            return 0f;

        float chance = data.counterFollowupChance;
        if (attackObserver.IsPlayerGuarding)
            chance += data.guardedCounterFollowupBonus;
        if (IsUnderPlayerPressure())
            chance += data.pressureCounterFollowupBonus;
        return Mathf.Clamp01(chance);
    }

    private bool TryBeginCounterFollowup(in EnemyTargetInfo perception, out EnemyIntent intent)
    {
        intent = default;
        if (data.counterFollowupChance <= 0f)
            return false;

        EnemyAttackData attack = ChooseAttack(
            perception.Distance,
            EnemyAttackContext.AfterDeflectFollowup,
            allowApproach: true);
        if (attack == null)
            return false;

        canCounter = false;
        IsCounterAttack = false;
        attackContext = EnemyAttackContext.AfterDeflectFollowup;
        combatStep = CombatStep.Engage;
        attackTimer = Time.time;
        SetApproachAttack(attack);
        intent = GetApproachIntent(perception);
        return true;
    }

    //============ 공격 후보와 가중치 선택 ============

    /// <summary>
    /// 공격하기로 선택
    /// </summary>
    private EnemyAttackData ChooseAttack(
        float distance,
        EnemyAttackContext context,
        bool isCounter = false,
        bool allowApproach = false,
        bool preferInRange = false,
        bool preferSingleAttack = false,
        bool allowObservation = true)
    {
        validAttacks.Clear();
        validAttackWeights.Clear();
        bool hasInRangeAttack = false;

        foreach (EnemyAttackData attack in data.pattern)
        {
            if (attack == null)
                continue;
            if (attack.SelectionWeight <= 0f)
                continue;
            if (!SupportsSelectionContext(attack, context))
                continue;
            if (!UsesAttackPolicy && context == EnemyAttackContext.AfterDeflectFollowup)
            {
                if (attack.PatternRole is not (EnemyAttackRole.Combo
                    or EnemyAttackRole.Rush
                    or EnemyAttackRole.Perilous
                    or EnemyAttackRole.Delayed
                    or EnemyAttackRole.Retreat))
                    continue;
                if (distance < data.minimumApproachDistance && attack.PatternRole != EnemyAttackRole.Retreat)
                    continue;
            }
            if (failedPreparations.TryGetValue(attack, out float retryTime) && retryTime > Time.time)
                continue;
            if (attack == data.retreatAttack && !CanRetreat())
                continue;
            bool canApproach = allowApproach
                && attack.MaxDistance >= data.minimumApproachDistance
                && (attack.HasHitWindow
                || attack.NextComboAttack != null);
            float distanceError = attack.GetStartDistanceError(distance);
            canApproach &= distanceError >= -data.maxPreparationRetreatDistance
                && distanceError <= data.chaseSpeed * data.attackPreparationTimeout;
            bool isInStartRange = IsInAttackStartRange(attack, distance);
            if (!isInStartRange && !canApproach)
                continue;

            // 반격은 쿨다운 무시
            if (!UsesAttackPolicy && !isCounter && coolTimes.TryGetValue(attack, out float cool) && cool > Time.time)
                continue;

            validAttacks.Add(attack);
            hasInRangeAttack |= isInStartRange;
        }

        if (validAttacks.Count == 0 && !UsesAttackPolicy)
            return null;

        if (preferInRange && hasInRangeAttack)
        {
            for (int i = validAttacks.Count - 1; i >= 0; i--)
            {
                if (IsInAttackStartRange(validAttacks[i], distance))
                    continue;

                validAttacks.RemoveAt(i);
            }
        }

        if (!UsesAttackPolicy && preferSingleAttack && validAttacks.Exists(IsSingleAttack))
        {
            for (int i = validAttacks.Count - 1; i >= 0; i--)
            {
                if (IsSingleAttack(validAttacks[i]))
                    continue;

                validAttacks.RemoveAt(i);
            }
        }

        if (!UsesAttackPolicy
            && !isCounter
            && (context == EnemyAttackContext.Neutral
            || context == EnemyAttackContext.AfterGuard))
        {
            // 멀어진 상대는 추격이 우선이고, 근접 공방의 대응 기술은 선택한 뒤 준비 거리를 맞춥니다.
            if (distance >= data.distantAttackDistance)
                PreferDistanceResponse(distance);
            PreferExchangeResponse();
            if (distance < data.distantAttackDistance)
                PreferDistanceResponse(distance);
        }

        return PickWeightedAttack(distance, allowObservation);
    }

    private bool SupportsSelectionContext(EnemyAttackData attack, EnemyAttackContext context) => attack.SupportsContext(context)
        || UsesAttackPolicy && isReboundSelection && context == EnemyAttackContext.Neutral
            && attack.SupportsContext(EnemyAttackContext.AfterDeflect);

    private EnemyAttackData ChooseWhiffAttack(float distance, float remaining)
    {
        validAttacks.Clear();
        validAttackWeights.Clear();
        foreach (EnemyAttackData attack in data.pattern)
        {
            if (attack == null || !attack.HasHitWindow || attack.SelectionWeight <= 0f
                || attack.PatternRole is not (EnemyAttackRole.Single or EnemyAttackRole.Counter or EnemyAttackRole.Rush)
                || !attack.IsInStartRange(distance)
                || !(attack.SupportsContext(EnemyAttackContext.Neutral)
                    || attack.PatternRole == EnemyAttackRole.Counter && attack.SupportsContext(EnemyAttackContext.AfterDeflect))
                || coolTimes.TryGetValue(attack, out float cooldown) && cooldown > Time.time
                || failedPreparations.TryGetValue(attack, out float retryTime) && retryTime > Time.time
                || EstimateAttackStartup(attack) + data.whiffStartupMargin > remaining)
                continue;

            validAttacks.Add(attack);
        }
        return validAttacks.Count > 0 ? PickWeightedAttack(distance, allowObservation: false) : null;
    }

    private static float EstimateAttackStartup(EnemyAttackData attack)
    {
        float firstHitFrame = attack.ActiveStartFrame;
        if (attack.HasSeparateHitWindows)
        {
            firstHitFrame = float.PositiveInfinity;
            foreach (AttackHitWindow window in attack.hitWindows)
            {
                if (window == null)
                    continue;
                float start = Mathf.Max(attack.ActiveStartFrame, window.startFrame);
                if (start < Mathf.Min(window.endFrame, attack.RecoveryStartFrame))
                    firstHitFrame = Mathf.Min(firstHitFrame, start);
            }
            if (float.IsPositiveInfinity(firstHitFrame))
                return firstHitFrame;
        }
        float slowestSpeed = float.PositiveInfinity;
        for (int i = 0; i <= 8; i++)
            slowestSpeed = Mathf.Min(
                slowestSpeed,
                attack.GetPlaybackSpeed(firstHitFrame * i / (8f * Mathf.Max(1f, attack.totalFrames))));
        return firstHitFrame / (attack.FramesPerSecond * slowestSpeed);
    }

    private void PreferDistanceResponse(float distance)
    {
        if (NeedsPostureRecovery)
            return;

        if (distance <= data.closePressureDistance)
        {
            float nearestStart = float.PositiveInfinity;
            foreach (EnemyAttackData attack in validAttacks)
                if (IsPressureAttack(attack))
                    nearestStart = Mathf.Min(nearestStart, Mathf.Abs(attack.GetStartDistanceError(distance)));
            if (float.IsPositiveInfinity(nearestStart))
                return;

            for (int i = validAttacks.Count - 1; i >= 0; i--)
            {
                EnemyAttackData attack = validAttacks[i];
                bool canPressure = nearestStart == 0f ? attack.IsInStartRange(distance)
                    : Mathf.Abs(attack.GetStartDistanceError(distance)) <= nearestStart + data.distanceTolerance;
                if (!IsPressureAttack(attack) || !canPressure)
                    validAttacks.RemoveAt(i);
            }
        }
        else if (distance >= data.distantAttackDistance)
        {
            int bestPriority = int.MaxValue;
            foreach (EnemyAttackData attack in validAttacks)
                bestPriority = Mathf.Min(bestPriority, GetClosingPriority(attack, distance));
            for (int i = validAttacks.Count - 1; i >= 0; i--)
                if (GetClosingPriority(validAttacks[i], distance) != bestPriority)
                    validAttacks.RemoveAt(i);
        }
    }

    private static bool IsGapClosingAttack(EnemyAttackData attack) => attack.PatternRole is EnemyAttackRole.Rush
        or EnemyAttackRole.Approach;

    private void PreferExchangeResponse()
    {
        if (!UsesExchange || NeedsPostureRecovery)
            return;
        int bestPriority = int.MaxValue;
        foreach (EnemyAttackData attack in validAttacks)
            bestPriority = Mathf.Min(
                bestPriority,
                exchange.SelectionPriority(attack.PatternRole, attackObserver.IsPlayerGuarding));
        for (int i = validAttacks.Count - 1; i >= 0; i--)
            if (exchange.SelectionPriority(validAttacks[i].PatternRole, attackObserver.IsPlayerGuarding) != bestPriority)
                validAttacks.RemoveAt(i);
    }

    private static bool IsPressureAttack(EnemyAttackData attack) => attack.PatternRole != EnemyAttackRole.Retreat
        && (attack.HasHitWindow || attack.NextComboAttack != null);

    private static int GetClosingPriority(EnemyAttackData attack, float distance)
    {
        if (distance >= attack.StartMinDistance)
        {
            if (attack.PatternRole == EnemyAttackRole.Rush)
                return 0;
            if (attack.PatternRole == EnemyAttackRole.Approach)
                return 1;
        }
        return IsPressureAttack(attack) ? 2 : 3;
    }

    private static bool IsSingleAttack(EnemyAttackData attack)
    {
        return attack.HasHitWindow && attack.Type == AttackType.Normal && attack.NextComboAttack == null
            && attack.GuardComboAttack == null && attack.ParryComboAttack == null;
    }

    private EnemyAttackData PickWeightedAttack(float distance, bool allowObservation = true)
    {
        if (UsesAttackPolicy)
            return PickReferenceAttack(distance, allowObservation);
        float totalWeight = 0f;
        foreach (EnemyAttackData attack in validAttacks)
        {
            float weight = GetAttackSelectionWeight(attack, distance);
            validAttackWeights.Add(weight);
            totalWeight += weight;
        }
        if (totalWeight <= 0f)
            return null;

        ///사용 가능한 공격들의 가중치를 뽑아서 합한 후 랜덤한 값을 설정.
        ///랜덤한 값이 포함되어 있는 범위를 공격으로 설정
        float randomValue = Random.Range(0f, totalWeight);
        for (int i = 0; i < validAttacks.Count; i++)
        {
            randomValue -= validAttackWeights[i];
            if (validAttackWeights[i] <= 0f || randomValue > 0f)
                continue;

            return validAttacks[i];
        }

        // 부동소수점 경계때문에 선택되지 않은 경우
        return validAttacks[validAttacks.Count - 1];
    }

    private EnemyAttackData PickReferenceAttack(float distance, bool allowObservation)
    {
        float scale = Mathf.Max(0.1f, data.policyDistanceScale);
        referenceInput.Distance = distance / scale;
        referenceInput.HealthRatio = stats.CurrentHp / Mathf.Max(1f, stats.MaxHp.GetValue());
        referenceInput.PostureRatio = stats.CurrentPosture / Mathf.Max(1f, stats.MaxPosture.GetValue());
        referenceInput.Lives = Mathf.Max(1, stats.CurrentLife);
        referenceInput.Now = Time.time;
        referenceInput.ActionTimes = policyActionTimes;
        Vector3 direction = Player.LocalPlayer != null
            ? Player.LocalPlayer.transform.position - transform.position
            : transform.forward;
        direction.y = 0f;
        referenceInput.TargetAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        referenceInput.SpaceQuery ??= QueryReferenceSpace;
        referenceInput.BeginSpaceQueries();

        LastReferenceChoice = EnemyReferenceDecision.Choose(referenceInput, referenceWeights, ReferenceRoll);
        // 접촉 하나를 여러 프레임에 걸쳐 다시 세지 않습니다.
        referenceInput.Event = 0;
        selectedReferenceAttack = null;
        selectedReferenceBinding = data.attackPolicy.FindBinding(LastReferenceChoice);
        LastReferenceMapping = EnemyReferenceMapping.None;
        LastReferenceAdaptation = selectedReferenceBinding?.adaptation;
        if (!LastReferenceChoice.IsValid)
            return null;

        if (selectedReferenceBinding?.execution == EnemyReferenceExecution.Observe
            && allowObservation
            && UsesExchange
            && !hasChosenObservation)
        {
            LastReferenceMapping = EnemyReferenceMapping.Observation;
            hasChosenObservation = true;
            EnemyAttackPolicy policy = data.attackPolicy;
            exchange.BeginObservation(
                distance,
                Time.time,
                Random.Range(Mathf.Min(policy.minimumObservationTime, policy.maximumObservationTime),
                Mathf.Max(policy.minimumObservationTime, policy.maximumObservationTime)));
            selectedReferenceBinding = null;
            return null;
        }

        EnemyAttackData selected = PickReferenceRole(selectedReferenceBinding?.role ?? EnemyAttackRole.None,
            selectedReferenceBinding?.alternativeRole ?? EnemyAttackRole.None);
        bool substitute = selectedReferenceBinding == null
            || selectedReferenceBinding.execution != EnemyReferenceExecution.Pattern;
        if (selected == null || selectedReferenceBinding?.execution == EnemyReferenceExecution.Observe)
        {
            // 없는 활/번개/턴 동작과 준비 불가능한 기술은 원본 추첨을 다시 하지 않고 실행만 대체합니다.
            selected = PickReferenceFallback(distance);
            substitute = true;
            LastReferenceAdaptation = selectedReferenceBinding?.execution == EnemyReferenceExecution.Observe
                ? "Immediate response or repeated observation; existing pressure motion"
                : string.IsNullOrEmpty(LastReferenceAdaptation)
                ? "No available counterpart in this context or distance"
                : LastReferenceAdaptation;
        }
        LastReferenceMapping = selected == null ? EnemyReferenceMapping.Unavailable
            : substitute ? EnemyReferenceMapping.Substitute : EnemyReferenceMapping.Pattern;
        if (selected != null && IsPressureAttack(selected) && HasReachedMotionRepeatLimit(selected))
        {
            EnemyAttackData replacement = PickReferenceRole(selectedReferenceBinding?.role ?? EnemyAttackRole.None,
                selectedReferenceBinding?.alternativeRole ?? EnemyAttackRole.None, selected);
            if (!CanReplaceReferenceAttack(selected, replacement, distance))
                replacement = PickReferenceFallback(distance, selected);
            if (CanReplaceReferenceAttack(selected, replacement, distance))
            {
                selected = replacement;
                LastReferenceMapping = EnemyReferenceMapping.Substitute;
                LastReferenceAdaptation = string.IsNullOrEmpty(LastReferenceAdaptation)
                    ? "Played motion repeat limit; available alternative attack"
                    : LastReferenceAdaptation + "; played motion repeat limit";
            }
        }
        if (selected != null && CanStartReferenceInPlace(selected, distance))
        {
            // 실측 타격권에 이미 들어온 일반 공격은 기존 근접 대체 기준으로 제자리에서 시작합니다.
            closeRangeFallbackAttack = selected;
            LastReferenceAdaptation = string.IsNullOrEmpty(LastReferenceAdaptation)
                ? "Measured close contact; preferred start-distance preparation skipped"
                : LastReferenceAdaptation + "; measured close contact";
        }
        if (selected == null)
            attackTimer = Time.time + Mathf.Max(0.1f, data.minActionDelay);
        selectedReferenceAttack = selected;
        return selected;
    }

    private static int ReferenceRoll(int minimum, int maximum) => Random.Range(minimum, maximum + 1);

    private bool QueryReferenceSpace(float angle, float distance) => motor != null
        && motor.HasDecisionSpace(angle, distance * Mathf.Max(0.1f, data.policyDistanceScale));

    private bool IsInAttackStartRange(EnemyAttackData attack, float distance) => attack.IsInStartRange(distance)
        || CanStartReferenceInPlace(attack, distance);

    private bool CanStartReferenceInPlace(EnemyAttackData attack, float distance) => UsesAttackPolicy
        && distance <= data.closePressureDistance && attack.HasHitWindow && attack.Type == AttackType.Normal
        && attack.PatternRole is EnemyAttackRole.Single or EnemyAttackRole.Combo or EnemyAttackRole.Delayed
        && distance < attack.StartMinDistance && distance <= attack.StartMaxDistance
        && combatDistance.CanHitAtDistance(attack, distance);

    private EnemyAttackData PickReferenceRole(
        EnemyAttackRole role,
        EnemyAttackRole alternative,
        EnemyAttackData avoidMotion = null)
    {
        float total = 0f;
        validAttackWeights.Clear();
        referenceCandidateIndices.Clear();
        for (int i = 0; i < validAttacks.Count; i++)
        {
            EnemyAttackData attack = validAttacks[i];
            float weight = 0f;
            if (attack.PatternRole != EnemyAttackRole.None && (attack.PatternRole == role || attack.PatternRole == alternative)
                && !HasSameAttackMotion(attack, avoidMotion))
            {
                weight = GetReferenceMotionWeight(attack);
                referenceCandidateIndices.Add(i);
            }
            validAttackWeights.Add(weight);
            total += weight;
        }
        if (total <= 0f)
            return null;
        return PickReferenceCandidate(total);
    }

    private EnemyAttackData PickReferenceCandidate(float totalWeight)
    {
        float roll = Random.Range(0f, totalWeight);
        EnemyAttackData last = null;
        foreach (int index in referenceCandidateIndices)
        {
            last = validAttacks[index];
            roll -= validAttackWeights[index];
            if (roll <= 0f)
                return last;
        }
        return last;
    }

    private static bool HasSameAttackMotion(EnemyAttackData first, EnemyAttackData second) => first != null && second != null
        && (first == second || (first.animationClip != null && second.animationClip != null
            ? first.animationClip == second.animationClip : first.AnimationHash == second.AnimationHash));

    private bool HasReachedMotionRepeatLimit(EnemyAttackData attack) => data.avoidRepeatedPatterns
        && HasSameAttackMotion(attack, LastStartedPattern) && consecutiveMotionUses >= Mathf.Max(
            1,
            data.maximumConsecutivePatternUses);

    private bool CanReplaceReferenceAttack(EnemyAttackData selected, EnemyAttackData replacement, float distance) => replacement != null
        && (!IsInAttackStartRange(selected, distance) || IsInAttackStartRange(replacement, distance))
        && (distance < data.distantAttackDistance || !IsGapClosingAttack(selected) || IsGapClosingAttack(replacement));

    private float GetReferenceMotionWeight(EnemyAttackData attack)
    {
        float weight = attack.SelectionWeight;
        if (!data.avoidRepeatedPatterns)
            return weight;
        if (HasSameAttackMotion(attack, LastStartedPattern))
            weight *= Mathf.Clamp(data.repeatedPatternWeight, 0.01f, 1f);
        foreach (RecentAttack recent in recentAttacks)
            if (HasSameAttackMotion(attack, recent.Attack))
                weight *= Mathf.Clamp(data.recentPatternWeight, 0.1f, 1f);
        return weight;
    }

    private float GetReferenceFallbackScore(EnemyAttackData attack, float distance)
    {
        float score = Mathf.Abs(attack.GetStartDistanceError(distance));
        if (!IsInAttackStartRange(attack, distance))
            score += 100f;
        if (distance >= data.distantAttackDistance && !IsGapClosingAttack(attack))
            score += 10f;
        return score;
    }

    private EnemyAttackData PickReferenceFallback(float distance, EnemyAttackData avoidMotion = null)
    {
        float bestScore = float.PositiveInfinity;
        referenceFallbackScores.Clear();
        referenceCandidateIndices.Clear();
        foreach (EnemyAttackData attack in validAttacks)
        {
            float score = IsPressureAttack(attack) && !HasSameAttackMotion(attack, avoidMotion)
                ? GetReferenceFallbackScore(attack, distance) : float.PositiveInfinity;
            referenceFallbackScores.Add(score);
            bestScore = Mathf.Min(bestScore, score);
        }
        if (float.IsPositiveInfinity(bestScore))
            return null;
        float total = 0f;
        validAttackWeights.Clear();
        for (int i = 0; i < validAttacks.Count; i++)
        {
            float weight = 0f;
            if (referenceFallbackScores[i] <= bestScore + data.distanceTolerance)
            {
                weight = GetReferenceMotionWeight(validAttacks[i]);
                referenceCandidateIndices.Add(i);
            }
            validAttackWeights.Add(weight);
            total += weight;
        }
        return PickReferenceCandidate(total);
    }

    private void RecordReferenceAttackStarted(EnemyAttackData attack)
    {
        if (!UsesAttackPolicy || attack != selectedReferenceAttack || selectedReferenceBinding == null)
            return;
        EnemyAttackPolicy.ReferenceBinding binding = selectedReferenceBinding;
        if (binding.historyKey > 0)
            policyActionTimes[binding.historyKey] = Time.time;
        // 실행한 대응 모션만 원본 상태 이력에 반영합니다. 활/번개 대체 동작은 해당 원본 상태를 위조하지 않습니다.
        if (LastReferenceMapping == EnemyReferenceMapping.Pattern)
        {
            if (binding.scope == EnemyReferenceScope.Exchange)
            {
                if (binding.action == 1)
                    referenceInput.Numbers[3] = 1;
                else if (binding.action == 4)
                    referenceInput.Numbers[3] = 0;
                else if (binding.action == 45)
                    referenceInput.Numbers[0] = 0;
                if (binding.action == 38)
                    referenceInput.Numbers[4] = 1;
            }
            else
            {
                if (binding.action == 3)
                    referenceInput.Timers[0] = Time.time + 7f;
                if (binding.action == 6)
                    referenceInput.Timers[0] = Time.time + 5f;
                if (binding.action == 10)
                    referenceInput.Timers[3] = Time.time + 10f;
            }
            bool retreat = binding.scope == EnemyReferenceScope.Neutral && binding.action == 24
                || binding.scope == EnemyReferenceScope.Exchange && binding.action is 2 or 43 or 44;
            if (retreat)
            {
                referenceInput.Numbers[2] = 1;
                referenceRetreatInProgress = binding.action is 2 or 24;
            }
        }
        selectedReferenceAttack = null;
        selectedReferenceBinding = null;
    }

    private float GetAttackSelectionWeight(EnemyAttackData attack, float distance)
    {
        float weight = data.avoidRepeatedPatterns && validAttacks.Count > 1 && attack == lastAttack
            ? attack.SelectionWeight * data.repeatedPatternWeight : attack.SelectionWeight;
        if (data.avoidRepeatedPatterns && validAttacks.Count > 1 && attack.PatternRole != EnemyAttackRole.None)
        {
            foreach (RecentAttack recent in recentAttacks)
                if (recent.Role == attack.PatternRole)
                    weight *= data.recentPatternWeight;
        }
        if (attackObserver.IsPlayerGuarding)
        {
            if (attack.PatternRole == EnemyAttackRole.Perilous)
                weight *= data.guardedPerilousWeight;
            if (attack.PatternRole == EnemyAttackRole.Delayed)
                weight *= data.guardedDelayedWeight;
        }
        if (attack.PatternRole == EnemyAttackRole.Retreat && IsUnderPlayerPressure())
            weight *= data.pressureRetreatWeight;
        if (attack.PatternRole == EnemyAttackRole.Rush && distance >= attack.StartMinDistance)
            weight *= data.distantRushWeight;
        if (UsesExchange)
            weight *= exchange.SelectionMultiplier(data.exchange, attack.PatternRole);
        return weight / (1f + Mathf.Abs(attack.GetStartDistanceError(distance)));
    }

    //============ 공격 준비와 거리 접근 ============

    private void SetApproachAttack(EnemyAttackData attack)
    {
        approachAttack = attack;
        if (attack != closeRangeFallbackAttack)
            closeRangeFallbackAttack = null;
        approachDistanceError = float.PositiveInfinity;
        approachProgressTime = Time.time;
        approachDeadline = Time.time + data.attackPreparationTimeout;
        IsPreparingRetreat = false;
        preparedRetreatDistance = 0f;
        isRetreatReentryApproach = false;
    }

    private float GetApproachTargetDistance(EnemyAttackData attack) => Mathf.Clamp(
        Mathf.Max(data.minimumApproachDistance, attack.PreferredStartDistance),
        attack.StartMinDistance,
        attack.StartMaxDistance);

    private EnemyIntent GetApproachIntent(in EnemyTargetInfo perception)
    {
        if (Time.time >= approachDeadline || approachAttack == data.retreatAttack && !CanRetreat())
            return RetryApproach(perception);

        if (approachAttack.IsInStartRange(perception.Distance)
            || approachAttack == closeRangeFallbackAttack
            && perception.Distance <= data.closePressureDistance
                && perception.Distance <= approachAttack.StartMaxDistance
            && combatDistance.CanHitAtDistance(
                    approachAttack,
                    perception.Distance))
            return BeginAttack(approachAttack);

        float distanceError = approachAttack.GetStartDistanceError(perception.Distance);
        if (distanceError * approachDistanceError < 0f
            || Mathf.Abs(approachDistanceError) - Mathf.Abs(distanceError) > data.distanceTolerance)
        {
            approachDistanceError = distanceError;
            approachProgressTime = Time.time;
        }

        if (Time.time - approachProgressTime >= data.approachRetryDelay)
            return RetryApproach(perception);

        if (perception.Distance < approachAttack.StartMinDistance)
        {
            if (TryPrepareRetreat(perception.Distance))
                return EnemyIntent.Attack(data.retreatAttack);
            return EnemyIntent.Space(perception.TargetPosition);
        }

        return EnemyIntent.Chase(perception.TargetPosition);
    }

    private EnemyIntent RetryApproach(in EnemyTargetInfo perception)
    {
        if (approachAttack != null)
            failedPreparations[approachAttack] = Time.time + data.failedPreparationRetryDelay;
        SetApproachAttack(null);
        if (perception.HasTarget && perception.CanSeeTarget)
        {
            SetApproachAttack(ChoosePreparationFallback(perception.Distance));
            if (approachAttack != null)
                return BeginAttack(approachAttack);
            BeginObservation(perception.TargetPosition);
        }
        canCounter = false;
        attackContext = EnemyAttackContext.Neutral;
        ResetActionTimer();
        return EnemyIntent.Hold();
    }

    private EnemyAttackData ChoosePreparationFallback(float distance)
    {
        EnemyAttackData attack = ChooseAttack(
            distance,
            attackContext,
            isCounter: canCounter,
            preferInRange: true,
            allowObservation: false);
        if (attack == null
            && (attackContext == EnemyAttackContext.AfterDeflectFollowup
            || attackContext == EnemyAttackContext.AfterDeflectApproach))
        {
            attackContext = EnemyAttackContext.Neutral;
            attack = ChooseAttack(distance, attackContext, preferInRange: true, allowObservation: false);
        }
        return attack != null ? attack : ChooseCloseRangeFallback(distance);
    }

    private EnemyAttackData ChooseCloseRangeFallback(float distance)
    {
        if (distance > data.closePressureDistance
            || attackContext is not (EnemyAttackContext.Neutral
            or EnemyAttackContext.AfterGuard))
            return null;

        validAttacks.Clear();
        validAttackWeights.Clear();
        foreach (EnemyAttackData attack in data.pattern)
        {
            if (attack == null || !attack.HasHitWindow || attack.Type != AttackType.Normal
                || attack.PatternRole is not (EnemyAttackRole.Single
                    or EnemyAttackRole.Combo
                    or EnemyAttackRole.Delayed)
                || attack.SelectionWeight <= 0f || !attack.SupportsContext(attackContext)
                || distance >= attack.StartMinDistance || distance > attack.StartMaxDistance
                || !UsesAttackPolicy && coolTimes.TryGetValue(attack, out float cooldown) && cooldown > Time.time
                || !combatDistance.CanHitAtDistance(attack, distance))
                continue;
            // 준비 거리 확보에 실패했어도, 실측 타격권 안에 있는 일반 공격은 제자리에서 시작할 수 있습니다.
            validAttacks.Add(attack);
        }
        if (validAttacks.Count == 0)
            return null;
        if (validAttacks.Exists(attack => attack.PatternRole == EnemyAttackRole.Single))
            validAttacks.RemoveAll(attack => attack.PatternRole != EnemyAttackRole.Single);
        closeRangeFallbackAttack = PickWeightedAttack(distance, allowObservation: false);
        return closeRangeFallbackAttack;
    }

    public void RecordChaseFailure(Vector3 targetPosition)
    {
        ResetCombatContext();
        failedNavigationTarget = targetPosition;
        navigationRetryTime = Time.time + Mathf.Max(0.1f, data.failedPreparationRetryDelay);
        ResetActionTimer();
    }

    //============ 후퇴 준비와 재진입 ============

    private bool TryPrepareRetreat(float distance)
    {
        EnemyAttackData retreat = data.retreatAttack;
        if (IsPreparingRetreat || canCounter || retreat == null || retreat == approachAttack
            || retreat.HasHitWindow || retreat.RetreatDistance <= 0f || retreat.RetreatMotionDistance <= 0f
            || !retreat.SupportsContext(attackContext)
            || coolTimes.TryGetValue(retreat, out float cooldown) && cooldown > Time.time)
            return false;

        float retreatDistance = motor.GetAvailableRetreatDistance(Mathf.Min(
            retreat.RetreatDistance,
            GetApproachTargetDistance(approachAttack) - distance));
        if (retreatDistance < data.minimumRetreatJumpDistance
            || !approachAttack.IsInStartRange(distance + retreatDistance))
            return false;

        IsPreparingRetreat = true;
        preparedRetreatDistance = retreatDistance;
        coolTimes[retreat] = Time.time + retreat.Cooldown;
        return true;
    }

    private bool CanRetreat() => data.retreatAttack != null && data.retreatAttack.RetreatMotionDistance > 0f
        && motor.GetAvailableRetreatDistance(data.retreatAttack.RetreatDistance) >= data.minimumRetreatJumpDistance;

    public EnemyAttackData GetPreparedRetreatAttack(EnemyAttackData retreat) => IsPreparingRetreat
        && retreat == data.retreatAttack
        ? approachAttack
        : null;

    public float GetRetreatDistance(EnemyAttackData retreat) => IsPreparingRetreat
        && retreat == data.retreatAttack
        ? preparedRetreatDistance
        : retreat.RetreatDistance;

    public bool CanStartPreparedAttack(EnemyAttackData attack) => !IsPreparingRetreat
        || attack == approachAttack
        && Time.time < approachDeadline;

    public void BeginPreparedAttack(EnemyAttackData attack)
    {
        if (IsPreparingRetreat)
            BeginAttack(attack);
    }

    public bool TryQueueRetreatReentry(EnemyAttackData retreat, EnemyAttackData reentry, in EnemyTargetInfo perception)
    {
        if (IsPreparingRetreat || retreat != data.retreatAttack || retreat.RetreatObservationDuration <= 0f
            || reentry == null || !perception.HasTarget || !perception.CanSeeTarget)
            return false;
        queuedRetreatReentry = reentry;
        retreatObservationDuration = Random.Range(retreat.RetreatObservationDuration,
            Mathf.Max(retreat.RetreatObservationDuration, retreat.RetreatObservationMaximumDuration));
        return true;
    }

    //============ 공격 시작과 접촉 기록 ============

    private EnemyIntent BeginAttack(EnemyAttackData attack)
    {
        policyCounterStartTime = float.NegativeInfinity;
        queuedRetreatReentry = null;
        hasRespondedToCloseRange = false;
        ResetGuardPressure();
        combatStep = CombatStep.Engage;
        coolTimes[attack] = Time.time + attack.Cooldown;
        lastAttack = attack;
        IsCounterAttack = canCounter || attackContext is EnemyAttackContext.AfterDeflectApproach
            or EnemyAttackContext.AfterDeflectFollowup
            || UsesAttackPolicy && attackContext == EnemyAttackContext.AfterDeflect;
        canCounter = false;
        attackContext = EnemyAttackContext.Neutral;
        SetApproachAttack(null);
        return EnemyIntent.Attack(attack);
    }

    public void RecordAttackStarted(EnemyAttackData attack)
    {
        responseOpportunityEndTime = float.NegativeInfinity;
        passiveParryCount = 0;
        if (UsesExchange)
            exchange.BeginAttack(attack != null ? attack.PatternRole : EnemyAttackRole.None);
        if (attack == null
            || attack.PatternRole == EnemyAttackRole.None
            || IsPreparingRetreat
            && attack == data.retreatAttack)
            return;

        RecordReferenceAttackStarted(attack);
        hasChosenObservation = false;
        consecutiveMotionUses = HasSameAttackMotion(attack, LastStartedPattern) ? consecutiveMotionUses + 1 : 1;
        LastStartedPattern = attack;
        recentAttacks.Add(new RecentAttack(attack));
        while (recentAttacks.Count > Mathf.Max(0, data.recentPatternMemory))
            recentAttacks.RemoveAt(0);
        executedPatternCounts.TryGetValue(attack.PatternRole, out int count);
        executedPatternCounts[attack.PatternRole] = count + 1;
    }

    public int GetExecutedPatternCount(EnemyAttackRole role) => executedPatternCounts.TryGetValue(role, out int count)
        ? count
        : 0;

    /// <summary>기존 접촉 판정이 확정된 뒤에만 공방 결과를 기억합니다.</summary>
    public void RecordAttackContact(DefenseType defense, float contactSide = 0f)
    {
        if (UsesExchange)
            exchange.RecordContact(defense);
        if (UsesAttackPolicy && defense == DefenseType.Parry)
            reboundReferenceEvent = contactSide > 0f ? 200201 : 200200;
    }

    private void TrimPlayerAttackMemory()
    {
        while (recentPlayerAttacks.Count > 0
            && Time.time - recentPlayerAttacks.Peek() > data.playerPressureMemoryDuration)
            recentPlayerAttacks.Dequeue();
    }

    private bool IsUnderPlayerPressure()
    {
        TrimPlayerAttackMemory();
        return attackObserver.IsPlayerAttacking || recentPlayerAttacks.Count >= data.playerPressureAttackCount;
    }

    //============ 공격 종료와 후속 공방 ============

    /// <summary>마지막 회수와 상대의 실제 회복 시간에 맞춰 공격권을 돌려줍니다.</summary>
    public void BeginAttackRecovery(
        EnemyAttackData attack,
        int attackCount,
        bool hasAcceptedContact,
        DefenseType lastContactDefense,
        float recoveryStartTime,
        float lastContactTime)
    {
        responseOpportunityEndTime = float.NegativeInfinity;
        if (attack == null || !attack.HasHitWindow)
            return;

        EnemyAttackContact contact = !hasAcceptedContact ? EnemyAttackContact.Miss
            : lastContactDefense == DefenseType.Parry ? EnemyAttackContact.Deflected
            : lastContactDefense == DefenseType.NormalGuard ? EnemyAttackContact.Guarded : EnemyAttackContact.Hit;
        EnemyAttackEndResponse response = attack.GetEndResponse(contact);
        if (response != EnemyAttackEndResponse.Automatic)
        {
            if (response == EnemyAttackEndResponse.Yield)
            {
                responseOpportunityEndTime = recoveryStartTime + Mathf.Max(0f, attack.ResponseOpportunityDuration);
                hasPendingDefense = false;
                lastAttackVersion = -1;
            }
            return;
        }

        bool defendedLargeFinish = lastContactDefense != DefenseType.None
            && (attack.ImpactLevel == KnockBackLevel.Heavy || attack.Type != AttackType.Normal
                || attackCount >= LongComboHitCount || attack.Followup == EnemyAttackFollowup.Recover);
        if (hasAcceptedContact && !defendedLargeFinish)
            return;

        float responseReadyTime = recoveryStartTime;
        if (hasAcceptedContact)
        {
            float playerRecovery = lastContactDefense == DefenseType.Parry
                ? CombatSettings.Current.PlayerParryRecoveryDuration : CombatSettings.Current.PlayerGuardRecoveryDuration;
            responseReadyTime = Mathf.Max(responseReadyTime, lastContactTime + playerRecovery);
        }
        if (Player.LocalPlayer?.StateMachine?.CurrentState is PlayerDodgeState dodge)
            responseReadyTime = Mathf.Max(responseReadyTime, Time.time + dodge.RemainingDuration);

        PlayerAttackData responseAttack = Player.LocalPlayer?.Combat?.FirstAttackData;
        float responseHitWindowEnd = responseAttack != null && responseAttack.HasHitWindow
            ? Mathf.Max(responseAttack.ActiveStartFrame, responseAttack.RecoveryStartFrame) / responseAttack.FramesPerSecond : 0f;
        responseOpportunityEndTime = Mathf.Max(recoveryStartTime + Mathf.Max(0f, data.defenseRecoveryDuration),
            responseReadyTime + responseHitWindowEnd + AnimationController.AttackTransitionDuration);
        hasPendingDefense = false;
        lastAttackVersion = -1;
    }

    public void FinishAttack(
        bool completedPattern,
        EnemyAttackData attack,
        int attackCount,
        bool wasRebounded,
        in EnemyTargetInfo perception,
        EnemyAttackContact? finalContact = null)
    {
        if (UsesAttackPolicy)
        {
            if (referenceRetreatInProgress && completedPattern)
                referenceInput.Timers[3] = Time.time + 30f;
            referenceRetreatInProgress = false;
            if (wasRebounded)
                referenceInput.Event = reboundReferenceEvent;
            else if (finalContact == EnemyAttackContact.Hit)
                referenceInput.Numbers[0] = 0;
        }
        if (queuedRetreatReentry != null)
        {
            if (completedPattern && attack == data.retreatAttack && perception.HasTarget && perception.CanSeeTarget)
            {
                IsCounterAttack = false;
                BeginObservation(perception.TargetPosition);
                attackTimer = Time.time + retreatObservationDuration;
                retreatReentryDeadline = attackTimer + data.attackPreparationTimeout;
                return;
            }
            queuedRetreatReentry = null;
        }
        if (IsPreparingRetreat)
        {
            if (completedPattern)
            {
                if (approachAttack != null)
                    failedPreparations[approachAttack] = Time.time + data.failedPreparationRetryDelay;
                SetApproachAttack(null);
                combatStep = CombatStep.Engage;
                if (perception.HasTarget && perception.CanSeeTarget)
                    SetApproachAttack(ChoosePreparationFallback(perception.Distance));
                attackTimer = Time.time;
            }
            else
            {
                SetApproachAttack(null);
                ResetActionTimer();
            }
            return;
        }
        if (UsesExchange)
        {
            if (completedPattern
                && attack != null
                && perception.HasTarget
                && perception.CanSeeTarget
                && !canCounter
                && approachAttack == null)
            {
                IsCounterAttack = false;
                combatStep = CombatStep.Engage;
                EnemyAttackContact contact = finalContact ?? exchange.Contact;
                isReboundSelection = wasRebounded;
                EnemyAttackEndResponse response = attack.GetEndResponse(contact);
                bool mustRecover = response == EnemyAttackEndResponse.Recover
                    || response == EnemyAttackEndResponse.Automatic
                        && (NeedsPostureRecovery
                    || wasRebounded
                    || attackCount >= LongComboHitCount
                    || attack.Followup == EnemyAttackFollowup.Recover);
                exchange.FinishAttack(data, mustRecover, perception.Distance, Time.time, Random.value, Random.value,
                    GetSpacingDuration(perception.Distance, data.exchange.spacingTimeout), response, contact);
                ResetActionTimer();
                return;
            }
            exchange.Reset();
        }
        bool completedLongCombo = attackCount >= LongComboHitCount;
        bool finishedHeavyAttack = attack != null && attack.HasHitWindow
            && (attack.ImpactLevel == KnockBackLevel.Heavy || attack.Type != AttackType.Normal);
        EnemyAttackFollowup followup = attack != null ? attack.Followup : EnemyAttackFollowup.Automatic;
        IsCounterAttack = false;
        combatStep = CombatStep.Engage;
        if (completedPattern && perception.HasTarget && perception.CanSeeTarget && !canCounter && approachAttack == null)
        {
            if (followup == EnemyAttackFollowup.Recover
                || followup == EnemyAttackFollowup.Observe
                && NeedsPostureRecovery)
                BeginRecovery(perception);
            else if (followup == EnemyAttackFollowup.Observe)
                BeginObservation(perception.TargetPosition);
            else if (completedLongCombo || finishedHeavyAttack || wasRebounded)
            {
                ReviewFollowup(perception, allowApproach: !wasRebounded);
                if (approachAttack != null)
                    BeginObservation(perception.TargetPosition);
            }
        }
        ResetActionTimer();
    }

    private bool TryGetExchangeIntent(in EnemyTargetInfo perception, bool canMove, out EnemyIntent intent)
    {
        intent = default;
        if (!UsesExchange || !exchange.HasPlan)
            return false;

        EnemyExchangeStep step = exchange.Update(
            data,
            perception.Distance,
            Time.time,
            NeedsPostureRecovery,
            ObservationDistance);
        if (step is EnemyExchangeStep.Space or EnemyExchangeStep.Observe)
        {
            intent = !canMove ? EnemyIntent.Hold() : step == EnemyExchangeStep.Space
                ? EnemyIntent.Space(perception.TargetPosition) : EnemyIntent.Strafe(perception.TargetPosition);
            return true;
        }
        if (!IsAttackReady)
        {
            intent = !canMove ? EnemyIntent.Hold() : perception.Distance >= data.distantAttackDistance
                ? EnemyIntent.Chase(perception.TargetPosition) : EnemyIntent.Strafe(perception.TargetPosition);
            return true;
        }

        combatStep = CombatStep.Engage;
        SetApproachAttack(ChooseAttack(perception.Distance, attackContext, allowApproach: true));
        if (approachAttack != null)
        {
            exchange.CompletePlan();
            intent = GetApproachIntent(perception);
            return true;
        }
        // 재사용 대기 중에도 현재 공방 결과를 보존하고, 일정 간격으로 후보를 다시 확인합니다.
        attackTimer = Time.time + Mathf.Max(0.1f, data.minActionDelay);
        intent = !canMove
            ? EnemyIntent.Hold()
            : exchange.Step != EnemyExchangeStep.Observe
            && perception.Distance >= data.distantAttackDistance
            ? EnemyIntent.Chase(perception.TargetPosition)
            : EnemyIntent.Strafe(perception.TargetPosition);
        return true;
    }

    private void ReviewFollowup(in EnemyTargetInfo perception, bool allowApproach)
    {
        if (!perception.HasTarget || !perception.CanSeeTarget)
            return;

        if (!NeedsPostureRecovery)
            SetApproachAttack(ChooseAttack(perception.Distance, attackContext, allowApproach: allowApproach));
        if (approachAttack == null && !exchange.HasPlan)
            BeginRecovery(perception);
    }

    private void BeginRecovery(in EnemyTargetInfo perception)
    {
        BeginObservation(perception.TargetPosition);
    }

    private void BeginObservation(Vector3 targetPosition)
    {
        Vector3 gap = transform.position - targetPosition;
        gap.y = 0f;
        observationSpacingDeadline = Time.time + GetSpacingDuration(gap.magnitude, data.maxStrafeTime);
        observationTargetPosition = targetPosition;
        observationDuration = Random.Range(Mathf.Min(data.minStrafeTime, data.maxStrafeTime),
            Mathf.Max(data.minStrafeTime, data.maxStrafeTime));
        StartObservation(targetPosition);
        if (observationDirection.magnitude < ObservationDistance - data.observationDistanceTolerance)
            combatStep = CombatStep.Space;
    }

    private void StartObservation(Vector3 targetPosition)
    {
        combatStep = CombatStep.Observe;
        combatStepStartTime = Time.time;
        observationDirection = transform.position - targetPosition;
        observationDirection.y = 0f;
    }

    private float GetSpacingDuration(float distance, float minimumDuration)
    {
        float travel = Mathf.Max(0f, ObservationDistance - distance);
        return Mathf.Max(minimumDuration, motor.EstimateSpacingTime(travel));
    }

    //============ 행동 대기 시간 ============

    /// <summary>
    /// 행동 한번한번의 쿨타임
    /// </summary>
    public void ResetActionTimer()
    {
        float min = Mathf.Min(data.minActionDelay, data.maxActionDelay);
        float max = Mathf.Max(data.minActionDelay, data.maxActionDelay);
        attackTimer = Time.time + Random.Range(min, max);
    }
}
