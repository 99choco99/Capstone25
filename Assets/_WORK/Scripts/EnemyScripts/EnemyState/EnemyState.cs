using UnityEngine;

public abstract class EnemyState : State
{
    protected readonly Enemy enemy;
    protected readonly EnemyStateMachine stateMachine;

    public EnemyState(Enemy enemy, EnemyStateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }


    /// <summary>
    /// Hit 으로 끊길 수 있는 상태인가
    /// </summary>
    public virtual bool CanInterrupted => true;


    /// <summary>
    /// 피격시 상태 처리
    /// </summary>
    public virtual void OnHit(in DamageResult result)
    {
        if (!CanInterrupted) return;

        if (stateMachine.CurrentState == stateMachine.EnemyHitState)
        {
            stateMachine.EnemyHitState.RestartHit(result);
            return;
        }

        stateMachine.EnemyHitState.SetHitData(result);
        stateMachine.TransitionTo(stateMachine.EnemyHitState);
    }
}
