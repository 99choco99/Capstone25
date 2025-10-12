using UnityEngine;

public class PlayerJumpState : State
{
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        if (!player.Motor.IsGrounded) { return; }
        player.Anim.SetBool("Jump", true);
        player.Motor.Jump(player.Stats.JumpPower);
        SoundManager.Instance.PlaySFX("Jump");
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
            return; // 상태가 변경되었으면 아래 로직을 실행할 필요 없음
        }
        player.Motor.Move();
    }

    public override void Exit()
    {
        player.Anim.SetBool("Jump", false);
    }

}
