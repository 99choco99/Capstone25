using UnityEngine;

public class PlayerAttackSMB : StateMachineBehaviour
{
    Player player;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<Player>();
        }
        player.Motor.CanMove = false;
        player.Motor.CanRotate = false;
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (player == null)
        {
            player = animator.GetComponent<Player>();
        }
    }
}
