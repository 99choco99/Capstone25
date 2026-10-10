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
    Patrol,
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

    private EnemyIntent(EnemyIntentType type, Vector3 targetPosition = default, EnemyAttackData attack = null, DefenseType defense = DefenseType.None)
    {
        Type = type;
        TargetPosition = targetPosition;
        AttackData = attack;
        Defense = defense;
    }

    public static EnemyIntent Hold() => new(EnemyIntentType.HoldPosition);
    public static EnemyIntent Patrol() => new(EnemyIntentType.Patrol);
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
    private enum CombatStep { Engage, Space, Observe }

    private EnemyAttackObserver attackObserver;

    private EnemyData data;

    public EnemyTacticalMode TacticalMode { get; private set; } = EnemyTacticalMode.Idle;
    public float StrafeDistance => approachAttack != null
        ? Mathf.Min(approachAttack.MaxDistance, approachAttack.MinDistance + data.distanceTolerance)
        : data.strafeDistance;
    public float ChaseStoppingDistance => approachAttack != null
        ? Mathf.Max(data.minimumApproachDistance, approachAttack.MaxDistance - data.distanceTolerance) : StrafeDistance;

    /// <summary>
    /// 현재 가능한 공격들
    /// </summary>
    private readonly List<EnemyAttackData> validAttacks = new();
    private readonly Dictionary<EnemyAttackData, float> coolTimes = new();
    private EnemyAttackData lastAttack;
    private EnemyAttackData approachAttack;
    private EnemyAttackContext attackContext = EnemyAttackContext.Neutral;
    private CombatStep combatStep;
    private float combatStepStartTime;
    private Vector3 observationDirection;
    private Vector3 observationStartPosition;

    private float attackTimer;
    private bool IsAttackReady => Time.time >= attackTimer;

    // 같은 공격을 중복 판단하지 않기 위한 식별자
    private int lastAttackVersion = -1;
    private DefenseType pendingDefense;
    private float pendingDefenseTime;
    private bool hasPendingDefense;

    private bool counterReserved;
    public bool canCounter
    {
        get => counterReserved;
        set
        {
            counterReserved = value;
            if (value)
            {
                attackContext = EnemyAttackContext.AfterDeflect;
                approachAttack = null;
                combatStep = CombatStep.Engage;
                return;
            }
            if (attackContext != EnemyAttackContext.AfterDeflect) return;

            attackContext = EnemyAttackContext.Neutral;
            approachAttack = null;
        }
    }
    private float counterDelayTime;
    public bool IsCounterAttack { get; private set; }

    private void Awake()
    {
        attackObserver = GetComponent<EnemyAttackObserver>();
        data = GetComponent<EnemyStats>().Data;
    }

    private void OnDisable()
    {
        ResetCombatContext();
    }

    //========================= 의도 변환 및 선택 ====================

    /// <summary>
    /// 현재 감지 값을 읽고 이번 프레임의 행동 의도를 반환
    /// </summary>
    public EnemyIntent SelectIntent(in EnemyTargetInfo perception, bool canPatrol, bool canStrafeWhileWaiting = true)
    {
        if (!perception.HasTarget)
        {
            ResetCombatContext();
            SetTacticalMode(EnemyTacticalMode.Idle);

            return canPatrol ? EnemyIntent.Patrol() : EnemyIntent.Hold();
        }

        if (!perception.CanSeeTarget)
        {
            ResetCombatContext();
            SetTacticalMode(EnemyTacticalMode.Chase);
            return EnemyIntent.Chase(perception.TargetPosition);
        }

        UpdateTacticalModeByDistance(perception.Distance);

        if (approachAttack != null && !approachAttack.SupportsContext(attackContext))
            approachAttack = null;

        //반격
        if (canCounter)
        {
            if (Time.time < counterDelayTime)
                return EnemyIntent.Hold();

            if (Time.time <= counterDelayTime + Mathf.Max(data.minActionDelay, data.maxActionDelay))
            {
                if (approachAttack != null && !IsInCounterApproachRange(approachAttack, perception.Distance))
                    approachAttack = null;
                if (approachAttack == null)
                    approachAttack = ChooseAttack(perception.Distance, attackContext, isCounter: true, allowApproach: true);

                if (approachAttack != null)
                    return GetApproachIntent(perception);
                return EnemyIntent.Hold();
            }

            canCounter = false;
            combatStep = CombatStep.Engage;
            ResetActionTimer();
        }

        // 방어판단 우선
        if (GetDefenseIntent(out EnemyIntent defenseIntent))
        {
            combatStep = CombatStep.Engage;
            approachAttack = null;
            return defenseIntent;
        }

        bool finishedObservation = false;
        if (combatStep != CombatStep.Engage)
        {
            bool timedOut = Time.time - combatStepStartTime >= data.maxStrafeTime;
            if (combatStep == CombatStep.Space)
            {
                if (Mathf.Abs(perception.Distance - data.strafeDistance) <= data.distanceTolerance || timedOut)
                    BeginObservation(perception.TargetPosition);
                else
                {
                    EnemyAttackData retreat = data.retreatAttack;
                    if (perception.Distance < data.strafeDistance - data.distanceTolerance && retreat != null
                        && retreat.SupportsContext(attackContext) && retreat.IsInStartRange(perception.Distance)
                        && (!coolTimes.TryGetValue(retreat, out float cooldown) || cooldown <= Time.time))
                        return BeginAttack(retreat);

                    return EnemyIntent.Space(perception.TargetPosition);
                }
            }

            Vector3 movement = transform.position - observationStartPosition;
            movement.y = 0f;
            float lateralDistance = Vector3.ProjectOnPlane(movement, observationDirection).magnitude;
            float observationDistance = StrafeDistance * Mathf.Sin(data.strafeArcAngle * Mathf.Deg2Rad);
            timedOut = Time.time - combatStepStartTime >= data.maxStrafeTime;
            if (!IsAttackReady || Time.time - combatStepStartTime < data.minStrafeTime
                || !timedOut && lateralDistance < observationDistance)
                return EnemyIntent.Strafe(perception.TargetPosition);

            combatStep = CombatStep.Engage;
            finishedObservation = true;
        }

        //공격
        // 추적 중에도 사거리가 긴 공격이 등록되어 있으면 사용 가능
        if (IsAttackReady)
        {
            if (approachAttack == null)
                approachAttack = ChooseAttack(perception.Distance, attackContext, allowApproach: TacticalMode == EnemyTacticalMode.Engage, preferInRange: !finishedObservation);

            if (approachAttack != null)
                return GetApproachIntent(perception);
        }

        bool canWaitInRange = approachAttack != null ? approachAttack.IsInStartRange(perception.Distance)
            : perception.Distance <= data.strafeDistance + data.distanceTolerance;
        if (!IsAttackReady && TacticalMode == EnemyTacticalMode.Engage && canWaitInRange)
            return canStrafeWhileWaiting ? EnemyIntent.Strafe(perception.TargetPosition) : EnemyIntent.Hold();

        //추격
        if (TacticalMode == EnemyTacticalMode.Chase || perception.Distance > StrafeDistance + data.distanceTolerance)
            return EnemyIntent.Chase(perception.TargetPosition);

        return EnemyIntent.Hold();
    }

    private EnemyIntent GetApproachIntent(in EnemyTargetInfo perception)
    {
        if (approachAttack.IsInStartRange(perception.Distance))
            return BeginAttack(approachAttack);
        if (perception.Distance < approachAttack.MinDistance)
            return EnemyIntent.Space(perception.TargetPosition);

        return EnemyIntent.Chase(perception.TargetPosition);
    }

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
        if (TacticalMode == nextMode) return;

        TacticalMode = nextMode;
        if (combatStep == CombatStep.Engage) ResetActionTimer();
    }

    //================= 수비 의도 함수들 ============================

    /// <summary>
    /// 방어의도를 가져오기
    /// </summary>
    private bool GetDefenseIntent(out EnemyIntent intent)
    {
        intent = default;

        if (!attackObserver.IsPlayerAttacking)
        {
            hasPendingDefense = false;
            return false;
        }

        if (!attackObserver.IsAttackInRange()) return false;

        int version = attackObserver.curAttackVersion;
        if (version != lastAttackVersion)
        {
            pendingDefense = GetDefenseDecision(version);
            pendingDefenseTime = attackObserver.DefenseStartTime;
            hasPendingDefense = pendingDefense != DefenseType.None;
        }

        if (!hasPendingDefense) return false;

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
        if (attackVersion == lastAttackVersion)
            return pendingDefense;

        lastAttackVersion = attackVersion;
        pendingDefense = RollDefenseType();
        return pendingDefense;
    }

    /// <summary>
    /// 공격 하나에 대한 방어 결과를 확률적으로 판정
    /// </summary>
    private DefenseType RollDefenseType()
    {
        float roll = Random.value;

        if (roll < data.parryChance)
            return DefenseType.Parry;

        if (roll < data.parryChance + data.blockChance)
            return DefenseType.NormalGuard;

        return DefenseType.None;
    }

    public void RecordDefenseResult(DefenseType defense)
    {
        canCounter = false;
        approachAttack = null;
        attackContext = defense == DefenseType.NormalGuard ? EnemyAttackContext.AfterGuard : EnemyAttackContext.Neutral;
    }

    public void ResetCombatContext()
    {
        combatStep = CombatStep.Engage;
        canCounter = false;
        IsCounterAttack = false;
        attackContext = EnemyAttackContext.Neutral;
        approachAttack = null;
        hasPendingDefense = false;
        lastAttackVersion = -1;
    }

    public void FinishHitReaction()
    {
        ResetCombatContext();
        ResetActionTimer();
    }

    //============================공격 의도 함수들 ================================

    /// <summary>GuardState가 패링에 성공했을 때 호출. 반격 여부 결정</summary>
    public void DecideCounterAttack()
    {
        canCounter = false;
        attackContext = EnemyAttackContext.Neutral;
        approachAttack = null;
        canCounter = Random.value <= data.counterChance;
        if (canCounter)
        {
            counterDelayTime = Time.time + Mathf.Max(data.counterDelay, CombatSettings.Current.ParriedRecoveryDuration);
        }
    }

    public void FinishAttack(bool completedPattern, bool reviewDistance, bool wasRebounded, in EnemyTargetInfo perception)
    {
        IsCounterAttack = false;
        combatStep = CombatStep.Engage;
        if (completedPattern && (reviewDistance || wasRebounded) && perception.HasTarget && perception.CanSeeTarget && !canCounter && approachAttack == null)
        {
            approachAttack = ChooseAttack(perception.Distance, attackContext, allowApproach: !wasRebounded);
            if (approachAttack == null)
                BeginSpacing();
        }
        ResetActionTimer();
    }

    private void BeginSpacing()
    {
        combatStep = CombatStep.Space;
        combatStepStartTime = Time.time;
    }

    private void BeginObservation(Vector3 targetPosition)
    {
        combatStep = CombatStep.Observe;
        observationDirection = transform.position - targetPosition;
        observationDirection.y = 0f;
        observationStartPosition = transform.position;
    }

    /// <summary>
    /// 공격하기로 선택
    /// </summary>
    private EnemyAttackData ChooseAttack(float distance, EnemyAttackContext context, bool isCounter = false, bool allowApproach = false, bool preferInRange = true)
    {
        validAttacks.Clear();

        foreach (EnemyAttackData attack in data.pattern)
        {
            if (attack == null) continue;
            if (attack.SelectionWeight <= 0f) continue;
            if (!attack.SupportsContext(context)) continue;
            bool canApproach = allowApproach && attack.MaxDistance >= data.minimumApproachDistance && (attack.HasHitWindow || attack.NextComboAttack != null)
                && (!isCounter || IsInCounterApproachRange(attack, distance));
            if (!attack.IsInStartRange(distance) && !canApproach) continue;

            // 반격은 쿨다운 무시
            if (!isCounter && coolTimes.TryGetValue(attack, out float cool) && cool > Time.time)
                continue;

            validAttacks.Add(attack);
        }

        if (validAttacks.Count == 0) return null;

        if (preferInRange && validAttacks.Exists(attack => attack.IsInStartRange(distance)))
        {
            for (int i = validAttacks.Count - 1; i >= 0; i--)
            {
                if (validAttacks[i].IsInStartRange(distance)) continue;

                validAttacks.RemoveAt(i);
            }
        }

        float totalWeight = 0f;
        foreach (EnemyAttackData attack in validAttacks)
            totalWeight += GetAttackSelectionWeight(attack);

        ///사용 가능한 공격들의 가중치를 뽑아서 합한 후 랜덤한 값을 설정.
        ///랜덤한 값이 포함되어 있는 범위를 공격으로 설정
        float randomValue = Random.Range(0f, totalWeight);
        foreach (EnemyAttackData attack in validAttacks)
        {
            randomValue -= GetAttackSelectionWeight(attack);
            if (randomValue > 0f) continue;

            return attack;
        }

        // 부동소수점 경계때문에 선택되지 않은 경우
        return validAttacks[validAttacks.Count - 1];
    }

    private float GetAttackSelectionWeight(EnemyAttackData attack)
    {
        return data.avoidRepeatedPatterns && validAttacks.Count > 1 && attack == lastAttack
            ? attack.SelectionWeight * data.repeatedPatternWeight : attack.SelectionWeight;
    }

    private bool IsInCounterApproachRange(EnemyAttackData attack, float distance)
    {
        float tolerance = CombatSettings.Current.ReactionCounterApproachDistance;
        return distance >= attack.MinDistance - tolerance && distance <= attack.MaxDistance + tolerance;
    }

    private EnemyIntent BeginAttack(EnemyAttackData attack)
    {
        combatStep = CombatStep.Engage;
        coolTimes[attack] = Time.time + attack.Cooldown;
        lastAttack = attack;
        IsCounterAttack = canCounter;
        canCounter = false;
        attackContext = EnemyAttackContext.Neutral;
        approachAttack = null;
        return EnemyIntent.Attack(attack);
    }

    //============ 행동 쿨타임 ====================

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
