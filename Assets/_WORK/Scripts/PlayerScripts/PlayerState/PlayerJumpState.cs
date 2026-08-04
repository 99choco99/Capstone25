using UnityEngine;

public class PlayerJumpState : PlayerState
{
    public override bool UseRootMotion => false;
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.AnimatorController.PlayAction(AnimHash.Jump);
        SoundManager.Instance.PlaySFX(SfxKeys.Jump);
        player.Motor.Jump();
    }

    public override void Update()
    {
        Vector3 moveDir = player.GetDesiredMoveDirection();
        Vector3 calculatedInputMove = moveDir * player.Motor.MoveSpeed;
        player.Motor.SetMovement(calculatedInputMove);

        if (player.IsLockOn && player.TargetingSystem.CurrentTarget != null)
        {
            Vector3 directionToTarget = player.TargetingSystem.CurrentTarget.TargetTransform.position - player.transform.position;
            player.Motor.RotateToDirection(directionToTarget);
        }
        else if (moveDir.sqrMagnitude > 0.0001f)
        {
            player.Motor.RotateToDirection(moveDir);
        }

        if (player.Motor.IsGrounded && player.Motor.CurrentVerticalVelocity < 0f)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    public override void Exit()
    {

    }

}
