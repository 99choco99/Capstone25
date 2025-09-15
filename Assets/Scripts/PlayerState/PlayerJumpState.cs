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

        if (player.Motor.IsGrounded && player.Motor.rb.linearVelocity.y < 0.1f)
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
    }

    public override void FixedUpdate()
    {
        // 공중에서도 어느 정도 좌우 이동이 가능하도록 Motor를 호출
        player.Motor.Move(player.InputHandler.MoveInput, player.Stats.MoveSpeed * 0.8f);
    }

    public override void Exit()
    {
        if (player.Motor.IsGrounded)
        {
            player.Anim.SetBool("Jump", false);
        }
    }

}
