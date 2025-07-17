using UnityEngine;

public class PlayerGuardState : StateMachineBehaviour
{
    PlayerController player;

    public float guardDuration;


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
        if (player.guard)
        {
            guardDuration = Time.deltaTime;
        }

        if (guardDuration <= 0.5f && player.playerSetting.Ishit)
        {
            player.anim.SetTrigger("Parry");
        }
        else
        {
            player.anim.SetTrigger("GuardHit");
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
