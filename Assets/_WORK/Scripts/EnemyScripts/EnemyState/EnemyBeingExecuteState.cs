public class EnemyBeingExecuteState : EnemyState
{
    public EnemyBeingExecuteState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }


    public override bool CanInterrupted => false;

    public override void Enter()
    {
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
    public void ExecutionFinished(bool completed)
    {
        if (completed && enemy.Stats.ProcessDeathblow()) return;
        bool isFinalDeathblow = enemy.Stats.CurrentLife == 1;

        enemy.Sense.Alert(Player.LocalPlayer.transform.position);

        if (enemy.Stats.IsHealthDepleted || enemy.Stats.IsPostureBroken)
            stateMachine.TransitionTo(stateMachine.EnemyGroggyState);
        else
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
    }
}
