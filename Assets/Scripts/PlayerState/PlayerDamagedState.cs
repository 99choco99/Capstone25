using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDamagedState : StateMachineBehaviour
{
    private PlayerController player;



    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if(player == null)
        {
            player = animator.GetComponent<PlayerController>();
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
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        animator.SetBool("AirBornState", false);
        player.currentState = PlayerState.Move;
    }

}
