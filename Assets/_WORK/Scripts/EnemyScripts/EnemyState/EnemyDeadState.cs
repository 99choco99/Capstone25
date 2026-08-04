using UnityEngine;

public class EnemyDeadState : EnemyState
{
    public EnemyDeadState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }


    public override void Enter()
    {
        //Á×Àº°Å Ã³¸®
        enemy.AnimationController.PlayAction(AnimHash.Death);

        enemy.Motor.Stop();
        enemy.Combat.ForceResetAttackState();
    }
}
