/// <summary>
/// 적의 현재 행동 State와 상태 전이 관리
/// </summary>
public class EnemyStateMachine
{
    public EnemyState CurrentState { get; private set; }

    public EnemyAttackState EnemyAttackState { get; }
    public EnemyGroundedState EnemyGroundedState { get; }
    public EnemyHitState EnemyHitState { get; }
    public EnemyGuardState EnemyGuardState { get; }
    public EnemyGroggyState EnemyGroggyState { get; }
    public EnemyBeingExecuteState EnemyBeingExecuteState { get; }
    public EnemyDeadState EnemyDeadState { get; }

    /// <summary>
    /// 현재 요청받은 공격정보
    /// </summary>
    private EnemyAttackData requestedAttack;

    public EnemyStateMachine(Enemy enemy)
    {
        EnemyAttackState = new EnemyAttackState(enemy, this);
        EnemyGroundedState = new EnemyGroundedState(enemy, this);
        EnemyHitState = new EnemyHitState(enemy, this);
        EnemyGuardState = new EnemyGuardState(enemy, this);
        EnemyGroggyState = new EnemyGroggyState(enemy, this);
        EnemyBeingExecuteState = new EnemyBeingExecuteState(enemy, this);
        EnemyDeadState = new EnemyDeadState(enemy, this);

        TransitionTo(EnemyGroundedState);
    }

    public void Tick()
    {
        CurrentState?.Update();
    }

    public void RequestAttack(EnemyAttackData attack)
    {
        requestedAttack = attack;
    }

    public EnemyAttackData GetRequestedAttack()
    {
        EnemyAttackData result = requestedAttack;
        requestedAttack = null;
        return result;
    }

    public void TransitionTo(EnemyState nextState)
    {
        if (nextState == null || CurrentState == nextState) return;

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }
}
