using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }
    public override bool UseRootMotion => false;
    public override void Enter()
    {
        player.AnimatorController.PlayAction(AnimHash.Locomotion);
    }

    public override void Update()
    {
        base.HandleInput();

        if (player.InputHandler.GuardInput)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
            return;
        }

        if (player.InputHandler.SprintInput && player.InputHandler.MoveInput != Vector3.zero && !player.IsLockOn)
        {
            stateMachine.TransitionTo(stateMachine.PlayerSprintState);
            return;
        }

        HandleMovement();
    }
    private void HandleMovement()
    {
        if (player.InputHandler.GuardInput)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
        }


        Vector3 moveDir = player.GetDesiredMoveDirection();
        float speed = player.InputHandler.SprintInput && !player.IsLockOn ? player.Motor.SprintSpeed : player.Motor.MoveSpeed;

        player.Motor.SetMovement(moveDir * speed);

        if (player.IsLockOn && player.TargetingSystem.CurrentTarget != null)
        {
            Vector3 directionToTarget = player.TargetingSystem.CurrentTarget.TargetTransform.position - player.transform.position;
            player.Motor.RotateToDirection(directionToTarget);
        }
        else if (moveDir != Vector3.zero)
        {
            player.Motor.RotateToDirection(moveDir);
        }

        UpdateLocomotionAnimation(moveDir);
    }

    private void UpdateLocomotionAnimation(Vector3 moveDir)
    {
        if (moveDir == Vector3.zero)
        {
            player.AnimatorController.UpdateLocomotion(0, 0);
        }
        else
        {
            if (player.IsLockOn)
                player.AnimatorController.UpdateLocomotion(player.InputHandler.MoveInput.x, player.InputHandler.MoveInput.z);
            else
                player.AnimatorController.UpdateLocomotion(0, player.InputHandler.MoveAmount);
        }
    }

    protected override void OnAttackCommand()
    {
        if (player.TargetingSystem.IsCurrentTargetExecutable())
            stateMachine.TransitionTo(stateMachine.PlayerExecuteState);
        else
        {
            stateMachine.RequestedAttackData = player.Combat.FirstAttackData;
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
        }
    }

    protected override void OnDodgeCommand()
    {
        stateMachine.TransitionTo(stateMachine.PlayerDodgeState);
    }

    protected override void OnJumpCommand()
    {
        if (player.Motor.IsGrounded)
        {
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
        }
    }

}