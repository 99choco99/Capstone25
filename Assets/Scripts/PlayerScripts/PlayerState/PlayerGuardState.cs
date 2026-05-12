using System.Collections;
using UnityEngine;

public class PlayerGuardState : State
{

    private float guardTimer;

    public PlayerGuardState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }


    public override void Enter()
    {
        guardTimer = 0f;
        player.AnimatorManager.PlayAction(AnimHash.Guard, false);

        player.Motor.StopMovement();
    }


    public override void Update()
    {
        guardTimer += Time.deltaTime;

        if (!player.InputHandler.GuardInput || player.Stats.isStunned)
        {
            if (player.InputHandler.MoveInput == Vector3.zero)
            {
                stateMachine.TransitionTo(stateMachine.PlayerIdleState);
            }
            else
            {
                stateMachine.TransitionTo(stateMachine.PlayerMoveState);
            }
            return;
        }
        if (player.InputHandler.JumpInput)
        {
            player.InputHandler.UseJumpInput();
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
            return;
        }
        if (player.InputHandler.AttackInput)
        {
            player.InputHandler.UseAttackInput(); // 입력 소비
            stateMachine.TransitionTo(stateMachine.PlayerAttackState);
            return;
        }
        player.Motor.SetTargetVelocity(player.Motor.GuardSpeed);
        player.Motor.HandleRotation();
    }




    public bool IsParryWindowActive()
    {
        return guardTimer <= player.Combat.parryDuration;
    }

}
