using UnityEngine;

public class EnemyBeingExecuteState : EnemyState
{
    public EnemyBeingExecuteState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    // 인살 도중에는 일반 데미지를 무시해야 함
    public override bool CanBeInterruptedByHit => false;

    public override void Enter()
    {
        enemy.Motor.Stop();
        enemy.Combat.ForceResetAttackState();
    }

    public void OnExecutionFinished()
    {
        //목숨 깎기
        enemy.Stats.LoseLife();

        if (enemy.Stats.CurrentLives > 0)
        {
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
        }
        else
        {
            stateMachine.TransitionTo(stateMachine.EnemyDeadState);
        }
    }
}
