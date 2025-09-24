using UnityEngine;

public class PlayerSprintState : State
{
    public PlayerSprintState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
    }

    public override void Exit()
    {

    }


    public override void Update()
    {
        // 이동 중에도 다른 행동으로 전환이 가능해야 합니다.
        if (player.InputHandler.AttackInput)
        {
            player.InputHandler.UseAttackInput();
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
            return;
        }

        if (player.InputHandler.GuardInput)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
            return;
        }

        if (player.InputHandler.JumpInput)
        {
            player.InputHandler.UseJumpInput();
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
            return;
        }
        if (!player.InputHandler.SprintInput)
        {
            stateMachine.TransitionTo(stateMachine.PlayerMoveState);
            return;
        }

        // 이동 입력을 멈추면 IdleState로 돌아갑니다.
        if (player.InputHandler.MoveInput == Vector3.zero)
        {
            stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            return;
        }
        player.Motor.Move();
    }


}
