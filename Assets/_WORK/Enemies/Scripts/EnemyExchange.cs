using UnityEngine;

/// <summary>공격 판정과 분리된, 공방 이후 행동 설정. 적별로 활성화합니다.</summary>
[System.Serializable]
public sealed class EnemyExchangeSettings
{
    public bool enabled;
    [Tooltip("명중한 짧은 패턴 뒤 압박을 이어갈 확률")]
    [Range(0f, 1f)] public float hitPressureChance = 0.7f;
    [Tooltip("상대 가드에 막힌 짧은 패턴 뒤 압박을 이어갈 확률")]
    [Range(0f, 1f)] public float guardedPressureChance = 0.85f;
    [Tooltip("가드 접촉 후 다음 공격을 고를 때 위협/엇박자 공격에 곱할 가중치")]
    [Min(1f)] public float guardedMixupWeight = 2f;
    [Min(0f)] public float minObservationTime = 0.5f;
    [Min(0f)] public float maxObservationTime = 0.85f;
    [Tooltip("공방 후 간격 확보 제한 시간의 최솟값. 필요한 거리/속도로 늘어나며, 시간이 지나면 재진입합니다.")]
    [Min(0.1f)] public float spacingTimeout = 1.25f;
    [Tooltip("간격 확보 중 상대가 이만큼 다시 붙어 근접권에 들어오면 압박으로 전환합니다.")]
    [Min(0.1f)] public float closingInterruptDistance = 0.55f;
}

public enum EnemyExchangeStep
{
    None,
    Pressure,
    Space,
    Observe,
    Reenter
}
public enum EnemyAttackContact
{
    Miss,
    Hit,
    Guarded,
    Deflected
}

/// <summary>확정된 접촉 결과와 시간/거리만으로 다음 공방 단계를 관리합니다.</summary>
public sealed class EnemyExchange
{
    public EnemyExchangeStep Step { get; private set; }
    public EnemyAttackContact Contact { get; private set; }
    public bool HasPlan => Step != EnemyExchangeStep.None;

    private EnemyAttackContact selectionContact;
    private EnemyAttackRole patternRole;
    private bool hasSelectionContext;
    private bool needsSpacing;
    private float waitingDistance;
    private float recoverySpacingDuration;
    private float spacingDeadline;
    private float observationDeadline;
    private float planDeadline;
    private float observationDuration;
    private float maximumGap;

    public void Reset()
    {
        Step = EnemyExchangeStep.None;
        Contact = selectionContact = EnemyAttackContact.Miss;
        patternRole = EnemyAttackRole.None;
        hasSelectionContext = needsSpacing = false;
    }

    public void BeginAttack(EnemyAttackRole role = EnemyAttackRole.None)
    {
        Step = EnemyExchangeStep.None;
        Contact = selectionContact = EnemyAttackContact.Miss;
        patternRole = role;
        hasSelectionContext = false;
    }

    public void RecordContact(DefenseType defense)
    {
        // 한 패턴 안에서 패링된 사실은 뒤의 가드/명중으로 지우지 않습니다.
        if (Contact == EnemyAttackContact.Deflected)
            return;
        Contact = defense == DefenseType.Parry ? EnemyAttackContact.Deflected
            : defense == DefenseType.NormalGuard ? EnemyAttackContact.Guarded : EnemyAttackContact.Hit;
    }

    public void FinishAttack(
        EnemyData data,
        bool mustRecover,
        float distance,
        float now,
        float pressureRoll,
        float observationRoll,
        float spacingDuration,
        EnemyAttackEndResponse response = EnemyAttackEndResponse.Automatic,
        EnemyAttackContact? finalContact = null)
    {
        EnemyExchangeSettings settings = data.exchange;
        selectionContact = finalContact ?? Contact;
        hasSelectionContext = true;
        needsSpacing = false;
        waitingDistance = maximumGap = distance;
        recoverySpacingDuration = Mathf.Max(0.1f, spacingDuration);
        observationDuration = Mathf.Lerp(Mathf.Min(settings.minObservationTime, settings.maxObservationTime),
            Mathf.Max(settings.minObservationTime, settings.maxObservationTime), Mathf.Clamp01(observationRoll));
        if (mustRecover)
        {
            BeginSpacing(distance, now);
            return;
        }
        if (response is EnemyAttackEndResponse.Resume or EnemyAttackEndResponse.Yield)
        {
            Step = EnemyExchangeStep.Reenter;
            return;
        }

        float chance = response == EnemyAttackEndResponse.Pressure ? 1f
            : selectionContact == EnemyAttackContact.Hit ? settings.hitPressureChance
            : selectionContact == EnemyAttackContact.Guarded ? settings.guardedPressureChance : 0f;
        bool keptContact = selectionContact is EnemyAttackContact.Hit or EnemyAttackContact.Guarded;
        if (keptContact && distance <= data.closePressureDistance + data.distanceTolerance && pressureRoll < chance)
        {
            Step = EnemyExchangeStep.Pressure;
            return;
        }

        // 상대가 빠졌다면 다시 접근하고, 근접권에서 쉬는 경우에는 현재 간격을 유지합니다.
        Step = distance > data.closePressureDistance + data.distanceTolerance
            ? EnemyExchangeStep.Reenter : EnemyExchangeStep.Observe;
        observationDeadline = planDeadline = now + observationDuration;
    }

    private void BeginSpacing(float distance, float now)
    {
        needsSpacing = true;
        maximumGap = distance;
        spacingDeadline = now + recoverySpacingDuration;
        planDeadline = spacingDeadline + observationDuration;
        observationDeadline = float.PositiveInfinity;
        Step = EnemyExchangeStep.Space;
    }

    public EnemyExchangeStep Update(
        EnemyData data,
        float distance,
        float now,
        bool needsPostureRecovery,
        float observationDistance)
    {
        if (Step == EnemyExchangeStep.None)
            return Step;
        if (Step is EnemyExchangeStep.Pressure or EnemyExchangeStep.Reenter)
        {
            waitingDistance = distance;
            // 기술이 정한 압박/재진입을 체간 회복으로 다시 덮지 않습니다.
            if (distance > data.closePressureDistance + data.distanceTolerance)
                Step = EnemyExchangeStep.Reenter;
            return Step;
        }
        if (needsPostureRecovery && !needsSpacing)
            BeginSpacing(distance, now);

        float targetDistance = GetWaitingDistance(observationDistance);

        maximumGap = Mathf.Max(maximumGap, distance);
        bool playerClosed = distance <= data.closePressureDistance
            && maximumGap - distance >= data.exchange.closingInterruptDistance;
        bool playerLeft = distance > targetDistance + data.observationDistanceTolerance;
        if (now >= planDeadline || playerClosed || playerLeft)
            return Step = EnemyExchangeStep.Reenter;

        float arrivalGap = targetDistance - Mathf.Max(data.arrivalTolerance, data.observationDistanceTolerance * 0.5f);
        if (needsSpacing
            && Step == EnemyExchangeStep.Observe
            && distance < targetDistance - data.observationDistanceTolerance)
            Step = EnemyExchangeStep.Space;
        if (Step == EnemyExchangeStep.Space)
        {
            if (distance >= arrivalGap)
            {
                Step = EnemyExchangeStep.Observe;
                // 간격 경계를 오가도 관찰 시간을 다시 받지 않습니다.
                observationDeadline = Mathf.Min(observationDeadline, Mathf.Min(now + observationDuration, planDeadline));
            }
            else if (now >= spacingDeadline)
                return Step = EnemyExchangeStep.Reenter;
        }
        if (Step == EnemyExchangeStep.Observe && now >= observationDeadline)
            Step = EnemyExchangeStep.Reenter;
        return Step;
    }

    public void CompletePlan() => Step = EnemyExchangeStep.None;

    public void BeginObservation(float distance, float now, float duration)
    {
        needsSpacing = false;
        waitingDistance = maximumGap = distance;
        observationDeadline = planDeadline = now + Mathf.Max(0f, duration);
        Step = EnemyExchangeStep.Observe;
    }

    public float GetWaitingDistance(float recoveryDistance) => needsSpacing && Step is EnemyExchangeStep.Space
        or EnemyExchangeStep.Observe
        ? recoveryDistance : waitingDistance;

    public int SelectionPriority(EnemyAttackRole role, bool playerGuarding)
    {
        if (!hasSelectionContext)
            return 1;
        if (selectionContact == EnemyAttackContact.Guarded && playerGuarding)
            return patternRole is EnemyAttackRole.Perilous or EnemyAttackRole.Delayed
                ? role is EnemyAttackRole.Single or EnemyAttackRole.Combo ? 0 : 1
                : role is EnemyAttackRole.Perilous or EnemyAttackRole.Delayed ? 0 : 1;
        if (selectionContact == EnemyAttackContact.Miss
            && patternRole is not (EnemyAttackRole.Single
            or EnemyAttackRole.Counter))
            return role is EnemyAttackRole.Single or EnemyAttackRole.Counter ? 0 : 1;
        return 1;
    }

    public float SelectionMultiplier(EnemyExchangeSettings settings, EnemyAttackRole role)
    {
        return hasSelectionContext
            && selectionContact == EnemyAttackContact.Guarded
            && role is EnemyAttackRole.Perilous
            or EnemyAttackRole.Delayed
            ? settings.guardedMixupWeight
            : 1f;
    }
}
