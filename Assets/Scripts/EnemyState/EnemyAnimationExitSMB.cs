using UnityEngine;

public class EnemyAnimationExitSMB : StateMachineBehaviour
{
    Enemy enemy;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (enemy == null)
        {
            enemy = animator.GetComponent<Enemy>();
        }
        else
        {
            enemy.AnimationManager.IsPerformAction = false;
        }
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (enemy == null)
        {
            enemy = animator.GetComponent<Enemy>();
        }
        else
        {
            enemy.AnimationManager.IsPerformAction = false;
        }
    }
}
