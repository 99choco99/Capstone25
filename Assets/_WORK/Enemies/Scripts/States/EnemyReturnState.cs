using UnityEngine;

/// <summary>추격을 마친 일반 적이 경로를 따라 돌아온 후 스폰 당시 방향으로 대기합니다.</summary>
public class EnemyReturnState : EnemyState
{
    private EnemyTerritory territory;
    private float returnDeadline;

    public EnemyReturnState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        territory = enemy.GetComponent<EnemyTerritory>();
        enemy.AIController.ResetCombatContext();
        enemy.Combat.ClearDefense();
        enemy.Motor.StopKnockback();
        enemy.Motor.ClearAttackReach();
        enemy.Motor.StopAttackPressure();
        enemy.AnimationController.PlayAction(AnimHash.Locomotion);
        if (territory != null)
            returnDeadline = Time.time + territory.ReturnStallTimeout
                + Mathf.Max(5f, enemy.Motor.EstimateChaseTime(territory.SpawnPosition) * 3f);
    }

    public override void Update()
    {
        if (territory == null || !territory.IsReturning)
        {
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
            return;
        }

        if ((enemy.transform.position - territory.SpawnPosition).sqrMagnitude
            <= territory.ArrivalDistance * territory.ArrivalDistance)
        {
            enemy.Motor.Stop();
            enemy.transform.rotation = territory.SpawnRotation;
            territory.CompleteReturn();
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
            return;
        }

        if (Time.time >= returnDeadline
            || !enemy.Motor.Chase(
                territory.SpawnPosition,
                territory.ArrivalDistance * 0.5f,
                stallTimeout: territory.ReturnStallTimeout))
        {
            enemy.Motor.ResetPosition(territory.SpawnPosition, territory.SpawnRotation);
            territory.CompleteReturn();
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
            return;
        }
        Vector2 velocity = enemy.Motor.GetNormalizedVelocity();
        enemy.AnimationController.UpdateLocomotion(velocity.x, velocity.y);
    }

    public override void Exit()
    {
        enemy.Motor.Stop();
        enemy.AnimationController.ForceStopLocomotion();
        if (territory != null)
            territory.CancelReturn();
    }

    public override void OnHit(in DamageResult result)
    {
        territory?.CancelReturn();
        if (result.Request.Attacker != null)
            enemy.Sense.Alert(result.Request.Attacker.transform.position);
        base.OnHit(result);
    }
}
