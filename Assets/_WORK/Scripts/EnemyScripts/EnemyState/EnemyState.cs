using UnityEngine;

public abstract class EnemyState : State
{
    protected Enemy enemy;
    protected EnemyStateMachine stateMachine;

    public EnemyState(Enemy enemy, EnemyStateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public virtual bool CanBeInterruptedByHit => true;

    public virtual void HandleDamage(DamageEvent damageEvent)
    {
        if (!CanBeInterruptedByHit) return;

        // 적 전용 피격 상태로 전환
        stateMachine.EnemyHitState.SetHitData(damageEvent);
        stateMachine.TransitionTo(stateMachine.EnemyHitState);
    }

    public virtual void OnPostureBroken()
    {
        // 적 전용 스턴/인살(Execution) 가능 상태로 전환
        stateMachine.TransitionTo(stateMachine.EnemyExecuteState);
    }
}
