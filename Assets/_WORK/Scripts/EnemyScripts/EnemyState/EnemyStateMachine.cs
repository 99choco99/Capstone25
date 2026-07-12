// EnemyStateMachine.cs
using Unity.Behavior;
using UnityEngine;

public class EnemyStateMachine
{
    private Enemy enemy;
    public EnemyState CurrentState { get; private set; }


    public EnemyAttackData RequestedAttackData;

    public EnemyAttackState EnemyAttackState { get; private set; }  
    public EnemyGroundedState EnemyGroundedState { get; private set; }
    public EnemyHitState EnemyHitState { get; private set; }
    public EnemyStunState EnemyStunState { get; private set; }
    public EnemyExecuteState EnemyExecuteState { get; private set; }
    public EnemyBeingExecuteState EnemyBeingExecuteState { get;private set; }
    public EnemyDeadState EnemyDeadState { get; private set; }

    public EnemyStateMachine(Enemy enemy)
    {
        this.enemy = enemy;

        EnemyAttackState = new EnemyAttackState(enemy, this);
        EnemyGroundedState = new EnemyGroundedState(enemy, this);
        EnemyHitState = new EnemyHitState(enemy, this);
        EnemyStunState = new EnemyStunState(enemy, this);
        EnemyExecuteState = new EnemyExecuteState(enemy, this);
        EnemyDeadState = new EnemyDeadState(enemy, this);


        TransitionTo(EnemyGroundedState);
    }


    public void Update() => CurrentState?.Update();


    public void TransitionTo(EnemyState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
        Debug.Log($"Enemy : {CurrentState}");
    }
}