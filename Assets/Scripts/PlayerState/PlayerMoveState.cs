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
        player.anim.SetBool("Attack", false);
        player.anim.ResetTrigger("Attack");
        player.anim.ResetTrigger("HeavyAttack");
        player.currentState = PlayerState.Move;
    }
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
        }
        player.currentState = PlayerState.Move;
        if (player.sprint)
        {
            player.currentSpeed = player.sprintSpeed;
        }
        else
        {
            player.currentSpeed = player.moveSpeed;
        }
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player.playerBehaviour.isInRange && player.attack && player.canExecute)
        {
            player.anim.SetTrigger("Execute");
        }
        else if (player.attack && player.sprint)
        {
            player.currentState = PlayerState.Attack;
            player.anim.SetTrigger("SprintAttack");
        }
        else if (!player.sprint && player.attack && player.isGround)
        {
            player.anim.SetTrigger("Attack");
            player.currentState = PlayerState.Attack;
            player.attack = false;
        }

        if (player.guard)
        {
            player.currentState = PlayerState.Guard;
            player.anim.SetBool("Guard", true);
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
