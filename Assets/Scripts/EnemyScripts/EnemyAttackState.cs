using UnityEngine;

public class EnemyAttackState : StateMachineBehaviour
{
    EnemyAttack self;
    int currentPatternIndex;
    int currentAnimationIndex;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(self == null)
        {
            self = animator.GetComponent<EnemyAttack>();
        }
        if (!stateInfo.IsName("Start"))
        {
            currentAnimationIndex = animator.GetInteger("index");
            currentPatternIndex = animator.GetInteger("pattern") - 1;
            animator.SetInteger("index", ++currentAnimationIndex);
            self.currentPattern = self.attacks[currentPatternIndex];
        }
        else
        {
            animator.SetInteger("pattern", 0);
            animator.SetInteger("index", -1);

        }
    }

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {

    }
    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {

    }

}
