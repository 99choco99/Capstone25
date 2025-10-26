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
        player.Motor.canMove = true;
        player.Motor.canRotate = true;
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (player == null)
        {
            player = animator.GetComponent<Player>();
        }
        player.Motor.canMove = true;
        player.Motor.canRotate = true;
        if (player.InputHandler.MoveInput == Vector3.zero)
        {
            player.StateMachine.TransitionTo(player.StateMachine.PlayerIdleState);
        }
        else
        {
            player.StateMachine.TransitionTo(player.StateMachine.PlayerMoveState);
        }
    }
}
