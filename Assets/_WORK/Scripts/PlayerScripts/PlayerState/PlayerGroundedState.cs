using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }
    public override bool UseRootMotion => false;
    public override PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.Normal;


    private bool IsSprinting => player.InputHandler.SprintInput && player.InputHandler.MoveInput != Vector3.zero && !player.IsLockOn;

    public override void Enter()
    {
        player.AnimatorController.PlayAction(AnimHash.Locomotion);
    }

    public override void Update()
    {
        base.HandleInput();
        if (stateMachine.CurrentState != this) return;
        if (player.InputHandler.GuardInput) { stateMachine.TransitionTo(stateMachine.PlayerGuardState); } //dodge, jump 동안 선입력된 가드 처리


        Vector3 moveDir = player.GetDesiredMoveDirection();
        float speed = IsSprinting ? player.Motor.SprintSpeed : player.Motor.MoveSpeed;

        player.Motor.SetMovement(moveDir * speed);

        if (player.IsLockOn)
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

    /// <summary>
    /// 움직임 애니메이션 동기화
    /// </summary>
    /// <param name="moveDir"></param>
    private void UpdateLocomotionAnimation(Vector3 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.0001f)
        {
            player.AnimatorController.UpdateLocomotion(0f, 0f);
        }
        else if (IsSprinting)
        {
            player.AnimatorController.UpdateLocomotion(0, player.InputHandler.MoveAmount, true);
        }
        else if(player.IsLockOn)
        {
            Vector3 localMove = player.transform.InverseTransformDirection(moveDir);
            player.AnimatorController.UpdateLocomotion(localMove.x, localMove.z);
        }
        else
        {
            player.AnimatorController.UpdateLocomotion(0, player.InputHandler.MoveAmount);
        }
    }

    protected override void OnAttackCommand()
    {
        if (RequestDeathblow()) return;

        stateMachine.RequestedAttackData = IsSprinting && player.Combat.SprintAttackData != null
            ? player.Combat.SprintAttackData : player.Combat.FirstAttackData;

        stateMachine.TransitionTo(stateMachine.PlayerAttackState);
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

    protected override void OnGuardCommand()
    {
        player.Motor.SetMovement(Vector3.zero);
        stateMachine.TransitionTo(stateMachine.PlayerGuardState);
    }
}
