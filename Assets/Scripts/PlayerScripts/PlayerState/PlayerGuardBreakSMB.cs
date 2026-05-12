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
        player.Stats.isStunned = true;
        player.Motor.CanRotate = false;
        player.Motor.CanMove = false;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<Player>();
        }
        player.Stats.isStunned = false;
        player.Motor.CanRotate = true;
        player.Motor.CanMove = true;
    }
}
