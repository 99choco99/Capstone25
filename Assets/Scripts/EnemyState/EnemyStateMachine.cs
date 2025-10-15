// EnemyStateMachine.cs
using Unity.Behavior;
using UnityEngine;

public class EnemyStateMachine : MonoBehaviour
{
    private Enemy enemy;
    public EnemyState CurrentState { get; private set; }

    [Header("Behavior Trees")]
    [SerializeField] private BehaviorGraph nonCombatBT;
    [SerializeField] private BehaviorGraph combatBT;

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

        // [핵심] 상태에 맞는 비헤이비어 트리(두뇌)로 교체합니다!
        if (newState is EnemyCombatState)
        {
            enemy.BehaviourTree.Graph = combatBT;
        }
        else // Idle, Patrol 등 비전투 상태일 때
        {
            enemy.BehaviourTree.Graph = nonCombatBT;
        }

        CurrentState.OnEnter();
    }
}