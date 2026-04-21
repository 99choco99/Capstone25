using UnityEngine;

public class EnemyCombatState : EnemyState
{
    public EnemyCombatState(Enemy enemy) : base(enemy) { }

    public override void OnUpdate()
    {
        // 전투 BT (방어, 공격, 추격 등)를 실행합니다.


        // 만약 플레이어를 놓치면, 비전투 상태로 전환합니다.
        if (!enemy.Senses.IsTargetDetected)
        {
            enemy.StateMachine.ChangeState(new EnemyIdleState(enemy));
        }
    }
}
