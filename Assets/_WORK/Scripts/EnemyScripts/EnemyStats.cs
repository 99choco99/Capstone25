using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 적 전용 기본 스탯과 목숨을 관리
/// </summary>
public class EnemyStats : LivingEntity
{
    private Enemy enemy;
    public override Faction TargetFaction => Faction.EnemyTeam;
    [SerializeField] private EnemyData enemyData;

    public int ExpReward => enemyData.exp;

    public event Action<int, int> OnLifeChanged;

    [field: Header("목숨")]
    [field: SerializeField, Min(1)] public int MaxLife { get; private set; }
    public int CurrentLife { get; private set; }


    protected override IDefenser Defenser => enemy.Combat;
    protected override PostureRecoveryMode CurrentPostureRecoveryMode =>
        enemy.StateMachine?.CurrentState?.PostureRecoveryMode ?? PostureRecoveryMode.Disabled;



    protected override void Awake()
    {
        //스탯 초기화
        base.Awake();

        enemy = GetComponent<Enemy>();
        Initialize(enemyData);
    }


    /// <summary>EnemyData로 기본 데이터 초기화</summary>
    public void Initialize(EnemyData data)
    {
        if (data == null)
        {
            Debug.LogError("EnemyStats: 초기화할 EnemyData가 없음", this);
            return;
        }

        MaxHp.AddBaseValue(data.hp - MaxHp.GetBaseValue());
        MaxPosture.AddBaseValue(data.posture - MaxPosture.GetBaseValue());
        CurrentHp = MaxHp.GetValue();

        ResetPosture();

        MaxLife = Mathf.Max(1, MaxLife);
        CurrentLife = MaxLife;
    }

    protected override void HandleHealthDepleted() { }

    /// <summary>
    /// 인살 한 번의 결과를 반영
    /// </summary>
    /// <returns>이번 인살로 적이 최종 사망했거나 이미 사망 상태라면 true.</returns>
    public bool ProcessDeathblow()
    {
        if (IsDead) return true;

        CurrentLife = Mathf.Max(0, CurrentLife - 1);
        OnLifeChanged?.Invoke(CurrentLife, MaxLife);

        if (CurrentLife == 0)
        {
            Die();
            return true;
        }

        RestoreHealth(MaxHp.GetValue());
        ResetPosture();
        return false;
    }



    /// <summary>
    /// 체력 구간별 체간 회복 속도 조절
    /// </summary>
    protected override float GetPostureRecoveryHealthMultiplier(float hpPercentage)
    {
        if (hpPercentage > 0.80f) return 1f;
        if (hpPercentage > 0.60f) return 0.50f;
        if (hpPercentage > 0.40f) return 0.167f;
        return 0.083f;
    }
}
