using UnityEngine;

public class PlayerJumpState : State
{
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        if (!player.Motor.IsGrounded) { return; }
        player.Anim.SetBool("Jump", true);
        player.AnimatorManager.PlayTargetActionAnimation("Jump", true);
        player.Motor.Jump(player.Motor.JumpPower);
        SoundManager.Instance.PlaySFX("Jump");
        player.Motor.canMove = true;
        player.Motor.canRotate = true;
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
        player.Motor.Move();
    }

    public override void Exit()
    {
        player.Anim.SetBool("Jump", false);
    }

}
