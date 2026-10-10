using UnityEngine;

/// <summary>
/// 피격시 연출과 경직 담당.
/// </summary>
public class EnemyHitState : EnemyState
{
    private float hitRecoveryDuration;

    private DamageResult currentHitData;

    private float stateTimer;

    public EnemyHitState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    /// <summary>피격 정보를 HitState에 전달</summary>
    public void SetHitData(in DamageResult result)
    {
        currentHitData = result;
    }

    /// <summary>
    /// Hit 애니메이션 및 경직 시작
    /// </summary>
    private void BeginHitReaction()
    {
        stateTimer = 0f;
        // 적 종류가 아니라 이번에 받은 공격의 강도로 피격 경직을 정합니다.
        hitRecoveryDuration = CombatSettings.Current.GetHitRecoveryTime(currentHitData.Request.KnockBackLevel);
        enemy.Motor.Stop();
        enemy.Combat.CancelAttack();
        enemy.AIController.canCounter = false;

        int animHash = DecideHitReaction(currentHitData);
        if (animHash != 0)
            enemy.AnimationController.PlayReaction(animHash);
    }

    /// <summary>피해 결과에 맞는 피격 애니메이션 선택</summary>
    private int DecideHitReaction(in DamageResult result)
    {
        return AnimationController.GetHitReactionHash(enemy.transform.forward, result);
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
        {
            enemy.AIController.FinishHitReaction();
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
        }
    }

    public override void Exit()
    {
        stateTimer = 0f;
    }
}
