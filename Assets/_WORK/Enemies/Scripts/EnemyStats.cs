using System;
using UnityEngine;

/// <summary>
/// 적 전용 기본 스탯과 목숨을 관리
/// </summary>
public class EnemyStats : LivingEntity
{
    private Enemy enemy;
    public override Faction TargetFaction => Faction.EnemyTeam;
    [SerializeField] private EnemyData enemyData;
    public EnemyData Data => enemyData;
    public float DamageMultiplier { get; private set; } = 1f;
    public int ScaledPlayerLevel { get; private set; }
    private bool levelScalingCaptured;

    public int EnemyId => enemyData.id;
    public int ExpReward => enemyData.exp;

    public event Action<int, int> OnLifeChanged;

    [field: Header("목숨")]
    [field: SerializeField, Min(1)] public int MaxLife { get; private set; }
    public int CurrentLife { get; private set; }

    protected override IDefenser Defenser => enemy.Combat;
    protected override PostureRecoveryMode CurrentPostureRecoveryMode => enemy.StateMachine?.CurrentState?.PostureRecoveryMode
        ?? PostureRecoveryMode.Disabled;

    protected override void Awake()
    {
        //스탯 초기화
        base.Awake();

        enemy = GetComponent<Enemy>();
        Initialize(enemyData);
    }

    /// <summary>EnemyData로 기본 데이터 초기화</summary>
    private void Initialize(EnemyData data)
    {
        if (data == null)
        {
            Debug.LogError("EnemyStats: 초기화할 EnemyData가 없음", this);
            return;
        }

        MaxHp.AddBaseValue(data.hp - MaxHp.GetValue());
        MaxPosture.AddBaseValue(data.posture - MaxPosture.GetValue());
        CurrentHp = MaxHp.GetValue();

        ResetPosture();

        MaxLife = Mathf.Max(1, MaxLife);
        CurrentLife = MaxLife;
        ApplyPlayerLevelScaling(DataManager.Instance.PlayerData?.level ?? PlayerGrowthSettings.Current.InitialLevel);
    }

    public void CapturePlayerLevel()
    {
        if (levelScalingCaptured)
            return;
        int playerLevel = Player.LocalPlayer?.Stats?.Level
            ?? DataManager.Instance.PlayerData?.level
            ?? PlayerGrowthSettings.Current.InitialLevel;
        ApplyPlayerLevelScaling(Mathf.Max(1, playerLevel));
        levelScalingCaptured = true;
    }

    public void ReleasePlayerLevel() => levelScalingCaptured = false;

    public void ApplyPlayerLevelScaling(int playerLevel)
    {
        if (enemyData == null || ScaledPlayerLevel == playerLevel)
            return;
        EnemyData.LevelScaling scaling = enemyData.levelScaling;
        float healthRatio = MaxHp.GetValue() > 0f ? CurrentHp / MaxHp.GetValue() : 1f;
        float health = enemyData.hp * scaling.GetMultiplier(
            playerLevel,
            scaling.InitialHealthMultiplier,
            scaling.MaximumHealthMultiplier);
        float posture = enemyData.posture * scaling.GetMultiplier(
            playerLevel,
            scaling.InitialPostureMultiplier,
            scaling.MaximumPostureMultiplier);
        MaxHp.AddBaseValue(Mathf.Max(1f, health) - MaxHp.GetValue());
        MaxPosture.AddBaseValue(Mathf.Max(1f, posture) - MaxPosture.GetValue());
        CurrentHp = MaxHp.GetValue() * Mathf.Clamp01(healthRatio);
        CurrentPosture = Mathf.Min(CurrentPosture, MaxPosture.GetValue());
        DamageMultiplier = scaling.GetMultiplier(
            playerLevel,
            scaling.InitialDamageMultiplier,
            scaling.MaximumDamageMultiplier);
        ScaledPlayerLevel = playerLevel;
        RestoreHealth(0f);
    }

    protected override void HandleHealthDepleted() { }

    /// <summary>
    /// 인살 한 번의 결과를 반영
    /// </summary>
    /// <returns>이번 인살로 적이 최종 사망했거나 이미 사망 상태라면 true.</returns>
    public bool ProcessDeathblow()
    {
        if (IsDead)
            return true;

        CurrentLife = Mathf.Max(0, CurrentLife - 1);
        OnLifeChanged?.Invoke(CurrentLife, MaxLife);

        if (CurrentLife == 0)
        {
            Die();
            return true;
        }

        RestoreHealthAndPosture();
        return false;
    }

    /// <summary>현재 목숨은 유지하고 체력을 전부 회복한 뒤 체간을 초기화합니다.</summary>
    public void RestoreHealthAndPosture()
    {
        RestoreHealth(MaxHp.GetValue());
        ResetPosture();
    }

    /// <summary>
    /// 체력 구간별 체간 회복 속도 조절
    /// </summary>
    protected override CombatSettings.PostureRecovery PostureRecovery => CombatSettings.Current.EnemyPostureRecovery;
}
