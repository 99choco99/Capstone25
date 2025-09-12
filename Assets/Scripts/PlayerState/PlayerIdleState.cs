using UnityEngine;

public class PlayerIdleState : State
{
    public PlayerIdleState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }


    public override void Enter()
    {
        player.Anim.SetBool("Jump", false);
        player.Combat.ResetCombo();
    }

    public override void Update()
    {
        // 1. 공격 입력이 들어오면 AttackState로 전환
        if (player.InputHandler.AttackInput)
        {
            player.InputHandler.UseAttackInput(); // 입력 소비
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
            return;
        }

        // 2. 가드 입력이 들어오면 GuardState로 전환
        if (player.InputHandler.GuardInput)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
            return;
        }

        // 3. 점프 입력이 들어오면 JumpState로 전환
        if (player.InputHandler.JumpInput)
        {
            player.InputHandler.UseJumpInput(); // 입력 소비
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
            return;
        }

        // 4. 이동 입력이 들어오면 MoveState로 전환
        if (player.InputHandler.MoveInput != Vector3.zero)
        {
            stateMachine.TransitionTo(stateMachine.PlayerMoveState);
            return;
        }
    }
}
