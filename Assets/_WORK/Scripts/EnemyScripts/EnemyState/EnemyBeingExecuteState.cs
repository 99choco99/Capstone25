
public class EnemyBeingExecuteState : EnemyState
{
    private bool isExecuted;

    public EnemyBeingExecuteState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override bool CanInterrupted => false;

    public override void Enter()
    {
        isExecuted = false;
        enemy.Motor.Stop();
        enemy.Motor.StopKnockback();
        enemy.Combat.CancelAttack();
        enemy.Combat.ClearDefense();
        enemy.Stats.IsInvincible = true;
    }

    public override void Exit()
    {
        enemy.Stats.IsInvincible = false;
    }

    /// <summary>
    /// 인살 종료 시 PlayerExecution이 호출
    /// </summary>
    public void ExecutionFinished()
    {
        if (isExecuted) return;
        isExecuted = true;

        bool isLive = enemy.Stats.ProcessDeathblow();

        if (!isLive && stateMachine.CurrentState == this)
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
    }
}
