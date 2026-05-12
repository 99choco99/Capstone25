using Unity.VisualScripting;
using UnityEngine;

public class PlayerMoveState : State
{
    public PlayerMoveState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }
    public override bool UseRootMotion => true;

    public override void Enter()
    {
        SoundManager.Instance.PlayLoopingSFX("Walking");
        player.AnimatorManager.PlayAction(AnimHash.Locomotion, false);
    }

    public override void Exit()
    {
        SoundManager.Instance.StopLoopingSFX("Walking");
    }


    public override void Update()
    {
        if (player.Stats.isStunned) { return; }
        if (player.InputHandler.AttackInput && player.TargetingSystem.IsCurrentTargetExecutable())
        {
            player.InputHandler.UseAttackInput();
            stateMachine.TransitionTo(stateMachine.PlayerExecuteState);
            return;
        }

        if (player.InputHandler.AttackInput)
        {
            player.InputHandler.UseAttackInput();
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
            return;
        }

        if (player.InputHandler.GuardInput && !player.AnimatorManager.IsActionLocked)
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


        if (player.InputHandler.SprintInput && player.TargetingSystem.CurrentTarget == null)
        {
            stateMachine.TransitionTo(stateMachine.PlayerSprintState);
            return;
        }

        if (player.InputHandler.DodgeInput)
        {
            player.InputHandler.UseDodgeInput();
            stateMachine.TransitionTo(stateMachine.PlayerRollingState);
            return;
        }

        // 이동 입력을 멈추면 IdleState로 돌아갑니다.
        if (player.InputHandler.MoveInput == Vector3.zero)
        {
            stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            return;
        }

        player.Motor.SetTargetVelocity(player.Motor.MoveSpeed);
        player.Motor.HandleRotation();
    }

}
