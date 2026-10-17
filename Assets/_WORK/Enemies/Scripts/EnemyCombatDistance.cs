using UnityEngine;

/// <summary>실측한 양쪽 기본 공격의 위협권으로 대치 거리를 정합니다. 공격/패링 판정은 바꾸지 않습니다.</summary>
public sealed class EnemyCombatDistance
{
    private CombatReachDatabase database;
    private Player target;
    private EnemyData data;
    private CombatReachDatabase.Actor self;
    private CombatReachDatabase.Actor opponent;
    private bool resolved;
    public float ObservationDistance { get; private set; }
    public bool UsesMeasuredReach { get; private set; }

    public void Refresh(EnemyData enemyData, Transform enemy, Player player)
    {
        if (resolved && target == player && data == enemyData)
            return;
        resolved = player != null && player.Combat != null;
        target = player;
        data = enemyData;
        ObservationDistance = data.observationDistance;
        UsesMeasuredReach = false;
        self = opponent = null;
        if (player == null || player.Combat == null)
            return;

        if (database == null)
            database = Resources.Load<CombatReachDatabase>("CombatReachDatabase");
        if (database == null || database.version != CombatReachDatabase.FormatVersion)
            return;
        self = database.Find(data, enemy);
        opponent = database.Find(null, player.transform, player.Combat.FirstAttackData);
        if (self == null || opponent == null)
            return;

        float playerReach = MaximumReach(opponent.Find(player.Combat.FirstAttackData, self.body));
        float enemyReach = 0f;
        foreach (EnemyAttackData attack in data.pattern)
        {
            if (attack == null || attack.SelectionWeight <= 0f
                || attack.PatternRole is not (EnemyAttackRole.Single or EnemyAttackRole.Combo))
                continue;
            enemyReach = Mathf.Max(enemyReach, MaximumReach(self.Find(attack, opponent.body)));
        }
        if (playerReach <= 0f || enemyReach <= 0f)
            return;

        // bands are already centre-to-centre distances including the target capsule.
        // Leave half a combined body width beyond the ordinary threat range, plus
        // the existing band tolerance so its inner edge remains outside that range.
        float clearance = (self.body.radius + opponent.body.radius) * 0.5f;
        ObservationDistance = Mathf.Max(playerReach, enemyReach) + clearance + data.observationDistanceTolerance;
        UsesMeasuredReach = true;
    }

    public bool CanHitAtDistance(EnemyAttackData attack, float distance)
    {
        var measured = self != null && opponent != null ? self.Find(attack, opponent.body) : null;
        if (measured?.bands == null)
            return false;
        foreach (var band in measured.bands)
            if (band.max > band.min && distance >= band.min && distance <= band.max)
                return true;
        return false;
    }

    private static float MaximumReach(CombatReachDatabase.Attack attack)
    {
        float reach = 0f;
        if (attack == null || attack.bands == null)
            return reach;
        foreach (var band in attack.bands)
            if (!float.IsNaN(band.max) && !float.IsInfinity(band.max) && band.max > band.min)
                reach = Mathf.Max(reach, band.max);
        return reach;
    }
}
