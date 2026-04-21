using UnityEngine;

public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(Enemy enemy) : base(enemy) { }

    public override void OnUpdate()
    {
        // 비전투 BT (순찰, 주변 둘러보기)를 실행합니다.


        // 만약 플레이어를 발견하면, 전투 상태로 전환합니다.
        if (enemy.Senses.IsTargetDetected)
        {
            enemy.StateMachine.ChangeState(new EnemyCombatState(enemy));
        }
    }
}
