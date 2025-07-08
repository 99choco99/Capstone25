using UnityEngine;

public class PlayerGuardState : IState
{
    PlayerController player;

    public float guardDuration;


    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if (player == null)
        {
<<<<<<< HEAD
            player = animator.GetComponent<PlayerController>();
=======
            if (player.anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.8f)
            {
                player.anim.SetTrigger("Parry");
                player.playerStateMachine.TransitionTo(player.playerStateMachine.playerMoveState);
            }
            else
            {
                player.anim.SetTrigger("GuardHit");
                player.player.Ishit = false;
            }
>>>>>>> parent of c1af48d (250701)
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

        if (guardDuration <= 0.5f && player.player.Ishit)
        {
            player.anim.SetTrigger("Parry");
        }
        else
        {
            player.anim.SetTrigger("GuardHit");
            player.player.Ishit = false;
        }

        if (!player.guard)
        {
<<<<<<< HEAD
            player.anim.SetBool("Guard", false);
            player.currentState = PlayerState.Move;
=======
            player.playerStateMachine.TransitionTo(player.playerStateMachine.playerMoveState);
            return;
>>>>>>> parent of c1af48d (250701)
        }
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        player.currentState = PlayerState.Move;
        player.guard = false;
    }
}
