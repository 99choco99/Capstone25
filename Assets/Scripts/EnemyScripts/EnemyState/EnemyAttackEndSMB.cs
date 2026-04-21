using UnityEngine;

public class EnemyAttackEndSMB : StateMachineBehaviour
{
    Enemy enemy;
    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (enemy == null)
        {
            enemy = animator.GetComponent<Enemy>();
        }
        enemy.AnimationManager.IsPerformAction = false;
        enemy.Combat.EnemyAttackEnd();
    }
}
