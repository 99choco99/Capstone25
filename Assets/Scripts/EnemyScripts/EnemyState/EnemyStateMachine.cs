// EnemyStateMachine.cs
using Unity.Behavior;
using UnityEngine;

public class EnemyStateMachine : MonoBehaviour
{
    private Enemy enemy;
    public EnemyState CurrentState { get; private set; }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        ChangeState(new EnemyIdleState(enemy));
    }

    private void Update()
    {
        CurrentState?.OnUpdate();
    }

    public void ChangeState(EnemyState newState)
    {
        CurrentState?.OnExit();
        CurrentState = newState;
        CurrentState.OnEnter();
    }
}