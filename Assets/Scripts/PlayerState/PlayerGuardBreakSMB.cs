using UnityEngine;

public class PlayerGuardBreakSMB : StateMachineBehaviour
{
    Player player;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(player == null)
        {
            player = animator.GetComponent<Player>();
        }
        player.Stats.isGroggy = true;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<Player>();
        }
        player.Stats.isGroggy = false;
    }
}
