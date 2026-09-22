using UnityEngine;

/// <summary>
/// 피격시 연출과 넉백 담당.
/// </summary>
public class EnemyHitState : EnemyState
{
    private float hitRecoveryDuration;

    private DamageResult currentHitData;

    private float stateTimer;

    public EnemyHitState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override bool UseRootMotion => false;


    /// <summary>피격 정보를 HitState에 전달</summary>
    public void SetHitData(in DamageResult result)
    {
        currentHitData = result;
    }

    /// <summary>
    /// Hit 애니메이션 및 넉백 시작
    /// </summary>
    private void BeginHitReaction()
    {
        stateTimer = 0f;
        // 적 종류가 아니라 이번에 받은 공격의 강도로 피격 경직을 정합니다.
        CombatSettings settings = CombatSettings.Current;
        hitRecoveryDuration = Mathf.Max(settings.GetReaction(currentHitData.Request.KnockBackLevel).HitRecoveryDuration, settings.DirectHitMoveDuration);
        enemy.Motor.Stop();
        enemy.Combat.CancelAttack();

        KnockbackSpec knockback = KnockBackPolicy.DefenderKnockBack(currentHitData);

        enemy.Motor.StartKnockback(currentHitData.HitDirection, knockback);

        int animHash = DecideHitReaction(currentHitData);
        if (animHash != 0)
            enemy.AnimationController.PlayReaction(animHash);
    }

    /// <summary>피해 결과에 맞는 피격 애니메이션 선택</summary>
    public int DecideHitReaction(in DamageResult result)
    {
        if (result.DefenseType == DefenseType.Parry) return AnimHash.Parry;
        if (result.DefenseType == DefenseType.NormalGuard) return AnimHash.GuardHit;

        float hitAngle = Vector3.SignedAngle(enemy.transform.forward, result.HitDirection, Vector3.up);

        if (Mathf.Abs(hitAngle) <= 45f)
            return AnimHash.BackHit;
        if (hitAngle > 45f && hitAngle <= 135f)
            return AnimHash.HitLeft;
        if (hitAngle >= -135f && hitAngle < -45f)
            return AnimHash.HitRight;

        return AnimHash.HitFront;
    }


    /// <summary>
    /// Hit중에 또 Hit당하면
    /// </summary>
    public void RestartHit(in DamageResult result)
    {
        SetHitData(result);
        BeginHitReaction();
    }

    public override void Enter()
    {
        BeginHitReaction();
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= hitRecoveryDuration)
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
    }

    public override void Exit()
    {
        stateTimer = 0f;
        enemy.Motor.StopKnockback();
    }
}
