using UnityEngine;

public class PlayerIdleState : State
{
    public PlayerIdleState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }
    public override bool UseRootMotion => true;
    public override void Enter()
    {
        player.Anim.SetBool("Jump", false);
        player.Combat.ResetCombo();
    }

    public override void Update()
    {
        if (player.Stats.isGroggy) { return; }
        if (player.InputHandler.AttackInput && player.TargetingSystem.IsCurrentTargetExecutable())
        {
            player.InputHandler.UseAttackInput();
            stateMachine.TransitionTo(stateMachine.PlayerExecuteState);
            return;
        }


        if (player.InputHandler.AttackInput)
        {
            player.InputHandler.UseAttackInput(); // 입력 소비
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
            return;
        }

        if (player.InputHandler.DodgeInput)
        {
            player.InputHandler.UseDodgeInput();
            stateMachine.TransitionTo(stateMachine.PlayerRollingState);
            return;
        }


        if (player.InputHandler.GuardInput && !player.animatorManager.isPerformingAction)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
            return;
        }


        if (player.InputHandler.JumpInput)
        {
            player.InputHandler.UseJumpInput(); // 입력 소비
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
            return;
        }


        if (player.InputHandler.MoveInput != Vector3.zero)
        {
            stateMachine.TransitionTo(stateMachine.PlayerMoveState);
            return;
        }
        player.Motor.Move();
    }

}
