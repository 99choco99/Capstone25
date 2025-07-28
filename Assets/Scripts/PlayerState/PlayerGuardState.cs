using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerGuardState : StateMachineBehaviour
{
    PlayerController player;

    public float canParryTime;

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
        }
        player.currentState = PlayerState.Guard;
        player.currentSpeed = player.guardMoveSpeed;
        player.playerBehaviour.canMove = false;
        player.anim.SetBool("isMove", false);
    }
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
        }
        player.currentState = PlayerState.Guard;
    }


    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player.playerBehaviour.guardDuration <= canParryTime && player.playerSetting.Ishit)
        {
            player.anim.SetTrigger("Parry");
        }
        else if(player.playerSetting.Ishit)
        {
            player.anim.SetTrigger("GuardHit");
        }

        if (!player.guard)
        {
            player.anim.SetBool("Guard", false);
        }
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        player.currentState = PlayerState.Move;
        player.guard = false;
    }
}
