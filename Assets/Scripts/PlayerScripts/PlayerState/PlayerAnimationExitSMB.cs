using UnityEngine;

public class PlayerAnimationExitSMB : StateMachineBehaviour
{
    Player player;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(player == null)
        {
            player = animator.GetComponent<Player>();
        }
        player.AnimatorManager.isPerformingAction = false;
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (player == null)
        {
            player = animator.GetComponent<Player>();
        }
        player.AnimatorManager.isPerformingAction = false;
    }
}
