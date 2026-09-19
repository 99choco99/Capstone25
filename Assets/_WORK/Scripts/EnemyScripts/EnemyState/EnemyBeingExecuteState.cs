
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
        // 정상 완료에만 목숨을 차감합니다. 마지막 목숨이면 사망 처리에서 상태가 바뀝니다.
        if (completed && enemy.Stats.ProcessDeathblow()) return;

        // 비활성화로 정리할 때는 체력·체간을 복구하지 않고 원래 상태로 되돌립니다.
        if (enemy.Stats.IsHealthDepleted || enemy.Stats.IsPostureBroken)
            stateMachine.TransitionTo(stateMachine.EnemyGroggyState);
        else
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
    }
}
