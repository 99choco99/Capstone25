using UnityEngine;

public class PlayerSprintState : PlayerState
{
    public PlayerSprintState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override bool UseRootMotion => false;
    public override void Enter()
    {
        SoundManager.Instance.PlayLoopingSFX("Walking");
    }

    public override void Exit()
    {
        SoundManager.Instance.StopLoopingSFX("Walking");
    }


    // 일단은 sprint시 이동속도만 빨라지게 해서 lockon상태도 대응해야됨. 미완성
    public override void Update()
    {
        base.HandleInput();
        if (!player.InputHandler.SprintInput || player.InputHandler.MoveInput == Vector3.zero)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            return;
        }

        Vector3 moveDir = player.GetDesiredMoveDirection();
        Vector3 sprintVelocity = moveDir * player.Motor.SprintSpeed;
        player.Motor.SetMovement(sprintVelocity);

        if (moveDir != Vector3.zero)
            player.Motor.RotateToDirection(moveDir);

        player.AnimatorController.UpdateLocomotion(0, 2f, true);

    }

    protected override void OnAttackCommand()
    {
        stateMachine.TransitionTo(stateMachine.PlayerAttackState);
    }

    protected override void OnJumpCommand()
    {
        if (player.Motor.IsGrounded)
        {
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
        }
    }

    protected override void OnDodgeCommand()
    {
        stateMachine.TransitionTo(stateMachine.PlayerDodgeState);
    }

}
