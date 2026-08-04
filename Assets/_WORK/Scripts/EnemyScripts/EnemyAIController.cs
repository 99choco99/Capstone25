using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    public static EnemyIntent Chase(Vector3 position) => new(EnemyIntentType.Chase, position);
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
    [SerializeField] private EnemyAttackObserver attackObserver;

    [Header("거리 판단")]
    [Tooltip("교전 거리")]
    [SerializeField, Min(0.1f)] private float combatRange = 3f;
    [Tooltip("이 배수만큼 멀어지면 추적으로 전환. 전투 경계선에서 떨림 방지")]
    [SerializeField, Min(1f)] private float combatExitRangeMultiplier = 1.2f;
    [Tooltip("strafe 거리")]
    [SerializeField, Min(0.1f)] private float strafeDistance = 2f;

    [Header("행동 대기")]
    [SerializeField, Min(0f)] private float minActionDelay = 0.5f;
    [SerializeField, Min(0f)] private float maxActionDelay = 1.2f;

    [Header("공격 패턴")]
    [SerializeField] private List<EnemyAttackData> pattern = new();

    [Header("방어")]
    [Tooltip("첫 패링 확률")]
    [SerializeField, Range(0f, 1f)] private float parryChance = 0.1f;
    [Tooltip("일반 가드 확률")]
    [SerializeField, Range(0f, 1f)] private float blockChance = 0.75f;
    [Tooltip("예상 타격 보다 빠르게 가드를 올리는 시간")]
    [SerializeField, Min(0f)] private float guardLeadTime = 0.11f;

    [Header("반격")]
    [Tooltip("패링 후 반격 확률")]
    [SerializeField, Range(0f, 1f)] private float counterChance = 0.45f;
    [Tooltip("패링 접촉 후 반격 전 최소 반응 시간")]
    [SerializeField, Min(0f)] private float counterDelay = 0.2f;

    public EnemyTacticalMode TacticalMode { get; private set; } = EnemyTacticalMode.Idle;
    public float StrafeDistance => strafeDistance;
    public float GuardLeadTime => guardLeadTime;

    /// <summary>
    /// 현재 가능한 공격들
    /// </summary>
    private readonly List<EnemyAttackData> validAttacks = new();
    private readonly Dictionary<EnemyAttackData, float> coolTimes = new();

    private float attackTimer;
    private bool IsAttackReady => Time.time >= attackTimer;

    // 같은 공격을 중복 판단하지 않기 위한 식별자
    private int lastAttackVersion = -1;
    private DefenseType pendingDefense;
    private float pendingDefenseTime;
    private bool hasPendingDefense;

    public bool canCounter;
    private float counterDelayTime;

    private void Awake()
    {
        attackObserver = GetComponent<EnemyAttackObserver>();
    }

    //========================= 의도 변환 및 선택 ====================


    /// <summary>
    /// 현재 감지 값을 읽고 이번 프레임의 행동 의도를 반환
    /// </summary>
    public EnemyIntent SelectIntent(in EnemyTargetInfo perception)
    {
        if (!perception.HasTarget)
        {
            hasPendingDefense = false;
            canCounter = false;
            SetTacticalMode(EnemyTacticalMode.Idle);
            return EnemyIntent.Hold();
        }

        if (!perception.CanSeeTarget)
        {
            hasPendingDefense = false;
            canCounter = false;
            SetTacticalMode(EnemyTacticalMode.Chase);
            return EnemyIntent.Chase(perception.TargetPosition);
        }

        UpdateTacticalModeByDistance(perception.Distance);


        //반격
        if (canCounter && Time.time >= counterDelayTime && ChooseAttack(perception.Distance, out EnemyAttackData counter, ignoreCooldown: true))
        {
            canCounter = false;
            return EnemyIntent.Attack(counter);
        }


        // 방어판단 우선
        if (GetDefenseIntent(out EnemyIntent defenseIntent))
            return defenseIntent;



        //추격
        if (TacticalMode == EnemyTacticalMode.Chase)
        {
            // 추적 중에도 사거리가 긴 공격이 등록되어 있으면 사용 가능
            if (IsAttackReady && ChooseAttack(perception.Distance, out EnemyAttackData chaseAttack))
                return EnemyIntent.Attack(chaseAttack);

            return EnemyIntent.Chase(perception.TargetPosition);
        }

        //공격
        if (IsAttackReady)
        {
            if (ChooseAttack(perception.Distance, out EnemyAttackData attack))
                return EnemyIntent.Attack(attack);

            ResetActionTimer();
        }

        return EnemyIntent.Strafe(perception.TargetPosition);
    }


    /// <summary>
    /// 거리에 따라 전투 모드 전환
    /// </summary>
    private void UpdateTacticalModeByDistance(float distance)
    {
        if (TacticalMode == EnemyTacticalMode.Idle)
        {
            SetTacticalMode(distance <= combatRange ? EnemyTacticalMode.Engage : EnemyTacticalMode.Chase);
            return;
        }

        if (TacticalMode == EnemyTacticalMode.Chase && distance <= combatRange)
        {
            SetTacticalMode(EnemyTacticalMode.Engage);
            return;
        }

        if (TacticalMode == EnemyTacticalMode.Engage
            && distance > combatRange * combatExitRangeMultiplier)
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
        ResetActionTimer();
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
            pendingDefenseTime = Mathf.Max(Time.time, attackObserver.ExpectedActiveTime - guardLeadTime);
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

        if (roll < parryChance)
            return DefenseType.Parry;

        if (roll < parryChance + blockChance)
            return DefenseType.NormalGuard;

        return DefenseType.None;
    }

    //============================공격 의도 함수들 ================================

    /// <summary>GuardState가 패링에 성공했을 때 호출. 반격 여부 결정</summary>
    public void DecideCounterAttack()
    {
        canCounter = Random.value <= counterChance;
        if (canCounter)
            counterDelayTime = Time.time + counterDelay;
    }



    /// <summary>
    /// 공격하기로 선택
    /// </summary>
    private bool ChooseAttack(float distance, out EnemyAttackData chosenAttack, bool ignoreCooldown = false)
    {
        chosenAttack = null;
        validAttacks.Clear();

        float totalWeight = 0f;
        foreach (EnemyAttackData attack in pattern)
        {
            if (attack == null) continue;
            if (attack.SelectionWeight <= 0f) continue;
            if (!attack.IsInRange(distance)) continue;

            // 반격은 쿨다운 무시
            if (!ignoreCooldown && coolTimes.TryGetValue(attack, out float cool) && cool > Time.time)
                continue;

            validAttacks.Add(attack);
            totalWeight += attack.SelectionWeight;
        }

        if (validAttacks.Count == 0 || totalWeight <= 0f) return false;


        ///사용 가능한 공격들의 가중치를 뽑아서 합한 후 랜덤한 값을 설정.
        ///랜덤한 값이 포함되어 있는 범위를 공격으로 설정
        float randomValue = Random.Range(0f, totalWeight);
        foreach (EnemyAttackData attack in validAttacks)
        {
            randomValue -= attack.SelectionWeight;
            if (randomValue > 0f) continue;

            chosenAttack = attack;
            break;
        }

        // 부동소수점 경계때문에 선택되지 않은 경우
        if (chosenAttack == null)
        {
            chosenAttack = validAttacks[validAttacks.Count - 1];
        }
        coolTimes[chosenAttack] = Time.time + chosenAttack.GetRandomCooldown();
        return true;
    }





    //============ 행동 쿨타임 ====================


    /// <summary>
    /// 행동 한번한번의 쿨타임
    /// </summary>
    private void ResetActionTimer()
    {
        float min = Mathf.Min(minActionDelay, maxActionDelay);
        float max = Mathf.Max(minActionDelay, maxActionDelay);
        attackTimer = Time.time + Random.Range(min, max);
    }


    /// <summary>
    /// 행동 후 호출. 다음 의사결정의 쿨타임 주입
    /// </summary>
    public void NotifyActtackCompleted()
    {
        ResetActionTimer();

    }
    private void OnValidate()
    {
        if(parryChance + blockChance >= 1f)
        {
            blockChance = 1f - parryChance;
        }
    }
}
