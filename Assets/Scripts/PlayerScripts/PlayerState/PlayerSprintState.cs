using UnityEngine;

public class PlayerSprintState : State
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


    public override void Update()
    {
        // 이동 중에도 다른 행동으로 전환이 가능해야 합니다.
        if (player.InputHandler.AttackInput)
        {
            player.InputHandler.UseAttackInput();
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
            player.AnimatorManager.PlayTargetActionAnimation("SprintAttack", true);
            return;
        }

        if (player.InputHandler.GuardInput && !player.AnimatorManager.isPerformingAction)
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
        if (!player.InputHandler.SprintInput || player.TargetingSystem.CurrentTarget != null)
        {
            stateMachine.TransitionTo(stateMachine.PlayerMoveState);
            return;
        }

        if (player.InputHandler.MoveInput == Vector3.zero)
        {
            stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            return;
        }
        player.Motor.Move();
    }


}
