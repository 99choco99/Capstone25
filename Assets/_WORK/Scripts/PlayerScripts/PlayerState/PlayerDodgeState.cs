using UnityEngine;

public class PlayerDodgeState : PlayerState
{
    public PlayerDodgeState(Player player,PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override bool UseRootMotion => true;

    private const float iframeDuration = 0.4f;
    private const float dodgeDuration = 1f;
    private float stateTimer = 0f;


    public override void Enter()
    {
        Vector3 inputDir = player.GetDesiredMoveDirection();
        Vector3 dodgeDirection = inputDir.sqrMagnitude > 0 ? inputDir : -player.transform.forward;

        player.Motor.SetMovement(Vector3.zero);

        if (dodgeDirection != Vector3.zero)
        {
            dodgeDirection.y = 0;
            player.transform.rotation = Quaternion.LookRotation(dodgeDirection.normalized);
        }


        stateTimer = 0f;
        player.AnimatorController.PlayAction(AnimHash.Roll);
        player.SetInvincible(true);
    }


    public override void Update()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= iframeDuration && player.Stats.IsInvincible)
        {
            player.SetInvincible(false);
        }

        if (stateTimer >= dodgeDuration)
        {
            if (player.InputHandler.MoveInput != Vector3.zero && player.InputHandler.SprintInput)
            {
                stateMachine.TransitionTo(stateMachine.PlayerSprintState);
            }
            else
            {
                stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            }
        }
    }

    public override void Exit()
    {
        stateTimer = 0f;
        player.SetInvincible(false);
        player.Motor.SetMovement(Vector3.zero);
    }
}
