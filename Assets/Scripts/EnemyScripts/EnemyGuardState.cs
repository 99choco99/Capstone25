using UnityEngine;

public class EnemyGuardState : StateMachineBehaviour
{

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Guard");
        animator.ResetTrigger("AttackSign");
    }
}
