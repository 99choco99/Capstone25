using System.Collections;
using UnityEngine;

public class PlayerGuardState : PlayerState
{

    [Header("패링 시스템")]
    private float guardTimer;
    private float parryWindowDuration = 0.2f;

    public PlayerGuardState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override bool UseRootMotion => false;


    public override void Enter()
    {
        guardTimer = 0f;
        player.Motor.SetMovement(Vector3.zero);
        player.AnimatorController.ForceStopLocomotion();
        player.AnimatorController.PlayAction(AnimHash.Guard);

        player.SetGuardState(true);
    }


    public override void Update()
    {
        base.HandleInput();
        if (stateMachine.CurrentState != this) return;

        guardTimer += Time.unscaledDeltaTime;
        if (guardTimer > parryWindowDuration && player.Combat.IsParryWindowOpen)
        {
            player.Combat.SetParryWindow(false);
        }

        if (!player.InputHandler.GuardInput)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            return;
        }

        HandleGuardMovement();
    }

    private void HandleGuardMovement()
    {
        Vector3 moveDir = player.GetDesiredMoveDirection();
        player.Motor.SetMovement(moveDir * player.Motor.GuardSpeed);

        if (player.IsLockOn && player.TargetingSystem.CurrentTarget != null)
        {
            Vector3 directionToTarget = player.TargetingSystem.CurrentTarget.TargetTransform.position - player.transform.position;
            directionToTarget.y = 0;
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



    public override void OnPostureBroken()
    {
        stateMachine.PlayerStunState.SetStunData(AnimHash.GuardBreak);
        stateMachine.TransitionTo(stateMachine.PlayerStunState);
    }


    protected override void OnAttackCommand()
    {
        if (player.TargetingSystem.IsCurrentTargetExecutable())
        {
            stateMachine.TransitionTo(stateMachine.PlayerExecuteState);
        }
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

    public override void Exit()
    {
        player.SetGuardState(false);
    }

}
