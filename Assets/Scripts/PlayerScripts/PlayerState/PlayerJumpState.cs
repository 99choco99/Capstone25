using UnityEngine;

public class PlayerJumpState : State
{
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        if (!player.Motor.IsGrounded) { return; }

        player.AnimatorManager.PlayAction(AnimHash.Jump, true);

        player.Motor.Jump(player.Motor.JumpPower);
        SoundManager.Instance.PlaySFX("Jump");
        player.Motor.CanMove = true;
        player.Motor.CanRotate = true;
    }

    public override void Update()
    {

        if (player.Motor.IsGrounded && player.Motor.verticalVelocity.y <= 0f)
        {
            if (player.InputHandler.MoveInput == Vector3.zero)
            {
                stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            }
            else
            {
                stateMachine.TransitionTo(stateMachine.PlayerMoveState);
            }
            return;
        }

    }

    public override void Exit()
    {

    }

}
