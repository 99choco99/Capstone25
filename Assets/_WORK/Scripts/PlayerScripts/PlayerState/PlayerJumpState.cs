using UnityEngine;

public class PlayerJumpState : PlayerState
{
    public override bool UseRootMotion => false;
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.AnimatorController.PlayAction(AnimHash.Jump);
        player.Motor.Jump();
    }

    public override void Update()
    {
        Vector3 moveDir = player.GetDesiredMoveDirection();
        Vector3 calculatedInputMove = moveDir * player.Motor.MoveSpeed;
        player.Motor.SetMovement(calculatedInputMove);

        if (moveDir != Vector3.zero) player.Motor.RotateToDirection(moveDir);

        if (player.Motor.IsGrounded && player.Motor.CurrentVerticalVelocity < 0f)
        {
            stateMachine.TransitionTo(stateMachine.PlayerLandState);
        }
    }

    public override void Exit()
    {

    }

}
