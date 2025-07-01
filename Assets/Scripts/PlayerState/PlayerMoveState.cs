using Unity.AppUI.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMoveState : StateMachineBehaviour
{
    PlayerController player;


    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
        }
        player.anim.SetBool("isMove", false);
        player.currentState = PlayerState.Move;
    }
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
        }
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player.playerBehaviour.isInRange && player.attack && player.canExecute)
        {
            player.anim.SetTrigger("Execute");
        }
        else if (player.attack && player.isGround)
        {
            player.anim.SetTrigger("Attack");
        }else if (player.guard)
        {

        }
        if (player.isGround)
        {
            player.anim.SetBool("Jump", false);
        }
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    { 
        player.anim.SetBool("isMove", false);
    }
}
