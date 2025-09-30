using UnityEngine;

public class DodgeSMB : StateMachineBehaviour
{
    Player player;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(player == null)
        {
            player = animator.GetComponent<Player>();
        }

        player.animatorManager.isPerformingAction = false;
    }
}
