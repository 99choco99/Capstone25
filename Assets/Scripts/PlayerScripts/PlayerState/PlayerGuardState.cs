using System.Collections;
using UnityEngine;

public class PlayerGuardState : State
{

    [Header("패링 시스템")]
    private float guardTimer;
    private float parryWindowDuration = 0.2f;

    public PlayerGuardState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }


    public override void Enter()
    {
        guardTimer = 0f;
        player.AnimatorController.PlayAction(AnimHash.Guard);
        player.Motor.StopMovement();

        player.Combat.IsGuarding = true;
        player.Combat.SetParryWindow(true);
    }


    public override void Update()
    {
        guardTimer += Time.deltaTime;

        if (guardTimer > parryWindowDuration && player.Combat.IsParryWindowOpen)
        {
            player.Combat.SetParryWindow(false);
        }

        if (!player.InputHandler.GuardInput || player.Stats.IsStunned)
        {
            if (player.InputHandler.MoveInput == Vector3.zero)
            {
                stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            }
            else
            {
                stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            }
            return;
        }
        if (player.InputHandler.JumpInput && player.Motor.IsGrounded)
        {
            player.InputHandler.UseJumpInput();
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
            return;
        }
        if (player.InputHandler.AttackInput)
        {
            player.InputHandler.UseAttackInput(); // 입력 소비
            stateMachine.RequestedAttack = AttackType.Normal;
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
            return;
        }
        player.Motor.SetTargetVelocity(player.Motor.GuardSpeed);
        player.Motor.HandleRotation();
    }



    public override void Exit()
    {
        player.Combat.IsGuarding = false;
        player.Combat.SetParryWindow(false);
    }

}
