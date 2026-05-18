using UnityEngine;

public class PlayerJumpState : State
{
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.AnimatorController.PlayAction(AnimHash.Jump);

        player.Motor.Jump();
        SoundManager.Instance.PlaySFX("Jump");

    }

    public override void Update()
    {
        player.Motor.SetTargetVelocity(player.Motor.MoveSpeed);
        player.Motor.HandleRotation();

        if (player.Motor.controller.isGrounded && player.Motor.controller.velocity.y <= 0f)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            return;
        }

    }

    public override void Exit()
    {

    }

}
