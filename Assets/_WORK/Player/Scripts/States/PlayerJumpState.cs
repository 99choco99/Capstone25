using UnityEngine;
using SoundEffectManager;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        player.AnimatorController.PlayAction(AnimHash.Jump);
        SoundManager.Instance.Play(SfxKeys.Jump);
        player.Motor.Jump();
    }

    public override void Update()
    {
        Vector3 moveDir = player.GetDesiredMoveDirection();
        Vector3 calculatedInputMove = moveDir * player.Motor.MoveSpeed;
        player.Motor.SetMovement(calculatedInputMove);

        RotateToTargetOrMove(moveDir);

        if (player.Motor.IsGrounded && player.Motor.CurrentVerticalVelocity < 0f)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }
}
