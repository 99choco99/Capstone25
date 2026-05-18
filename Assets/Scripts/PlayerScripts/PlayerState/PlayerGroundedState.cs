using UnityEngine;

public class PlayerGroundedState : State
{
    public PlayerGroundedState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }
    public override bool UseRootMotion => false;
    public override void Enter()
    {
        player.AnimatorController.PlayAction(AnimHash.Locomotion);
    }

    public override void Update()
    {
        if (player.Stats.IsStunned) { return; }


        if (player.InputHandler.AttackInput && player.TargetingSystem.IsCurrentTargetExecutable())
        {
            player.InputHandler.UseAttackInput();
            stateMachine.TransitionTo(stateMachine.PlayerExecuteState);
            return;
        }


        if (player.InputHandler.AttackInput)
        {
            player.InputHandler.UseAttackInput(); // 입력 소비
            stateMachine.RequestedAttack = AttackType.Normal;
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
            return;
        }

        if (player.InputHandler.DodgeInput)
        {
            player.InputHandler.UseDodgeInput();
            stateMachine.TransitionTo(stateMachine.PlayerRollingState);
            return;
        }


        if (player.InputHandler.GuardInput)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
            return;
        }


        if (player.InputHandler.JumpInput && player.Motor.IsGrounded)   
        {
            player.InputHandler.UseJumpInput(); // 입력 소비
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
            return;
        }

        if (player.InputHandler.MoveInput == Vector3.zero)
        {
            player.Motor.SetTargetVelocity(0);
            player.AnimatorController.UpdateLocomotion(0, 0);
        }
        else
        {
            if (player.InputHandler.SprintInput && player.TargetingSystem.CurrentTarget == null)
            {
                stateMachine.TransitionTo(stateMachine.PlayerSprintState);
                return;
            }

            player.Motor.SetTargetVelocity(player.Motor.MoveSpeed);
            player.Motor.HandleRotation();

            if (player.IsLockOn)
                player.AnimatorController.UpdateLocomotion(player.InputHandler.MoveInput.x, player.InputHandler.MoveInput.z);
            else
                player.AnimatorController.UpdateLocomotion(0, player.InputHandler.MoveAmount);
        }

    }

}