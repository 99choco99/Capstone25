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
            player.anim.SetTrigger("Hit");
            player.playerSetting.Ishit = false;
        }

        if (!player.guard)
        {
            player.anim.SetBool("Guard", false);
            player.currentState = PlayerState.Move;
        }
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        player.currentState = PlayerState.Move;
        player.guard = false;
    }
}
