using UnityEngine;

/// <summary>
/// 피격시 연출과 넉백 담당.
/// </summary>
public class EnemyHitState : EnemyState
{
    private const float HitRecoveryDuration = 0.5f;

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
        enemy.Motor.Stop();

        KnockbackSpec knockback = KnockBackPolicy.DefenderKnockBack(currentHitData);

        enemy.Motor.StartKnockback(currentHitData.HitDirection, knockback);

        int animHash = enemy.Combat.DecideHitReaction(currentHitData);
        if (animHash != 0)
            enemy.AnimationController.PlayReaction(animHash);
    }


    /// <summary>
    /// Hit중에 또 Hit당하면
    /// </summary>
    /// <param name="result"></param>
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

        if (stateTimer >= HitRecoveryDuration)
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
    }

    public override void Exit()
    {
        stateTimer = 0f;
        enemy.Motor.StopKnockback();
    }
}
