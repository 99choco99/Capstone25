using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDamagedState : IState
{
    private PlayerController player;
    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if(player == null)
        {
<<<<<<< HEAD
            player = animator.GetComponent<PlayerController>();
=======
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerMoveState);
>>>>>>> parent of c1af48d (250701)
        }
        player.currentState = PlayerState.Damaged;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
        }
        player.currentState = PlayerState.Damaged;
        animator.SetTrigger("Hit");

    }

}
