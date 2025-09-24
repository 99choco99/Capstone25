using UnityEngine;

public class PlayerJumpState : State
{
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        if (!player.Motor.IsGrounded) { return; }
        player.Anim.SetBool("Jump", true);
        player.Motor.Jump(player.Stats.JumpPower);

    }

    public override void Update()
    {

        if (player.Motor.IsGrounded)
        {
            if (player.InputHandler.MoveInput == Vector3.zero)
            {
                stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            }
            else
            {
                stateMachine.TransitionTo(stateMachine.PlayerMoveState);
            }
        }
        player.Motor.Move();
    }

    public override void Exit()
    {
        if (player.Motor.IsGrounded)
        {
            player.Anim.SetBool("Jump", false);
        }
    }

}
