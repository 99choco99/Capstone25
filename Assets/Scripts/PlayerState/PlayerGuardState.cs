using System.Collections;
using UnityEngine;

public class PlayerGuardState : State
{

    private float guardTimer;

    public PlayerGuardState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }


    public override void Enter()
    {
        guardTimer = 0f;
        player.Anim.SetBool("Guard", true);
        player.Motor.StopMovement();

        player.Motor.LockMovementFor(0.45f);
    }


    public override void Exit()
    {
        player.Anim.SetBool("Guard", false);
    }

    public override void Update()
    {
        guardTimer += Time.deltaTime;


        if (!player.InputHandler.GuardInput)
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
    }

    public override void FixedUpdate()
    {
        player.Motor.Move(player.InputHandler.MoveInput, player.Stats.MoveSpeed);
    }


    public bool IsParryWindowActive()
    {
        return guardTimer <= player.Combat.parryDuration;
    }

}
