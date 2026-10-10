using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.Normal;

    private bool IsSprinting => player.InputHandler.SprintInput && player.InputHandler.MoveInput != Vector3.zero && !player.IsLockOn;
    private bool isKeepingReaction;

    public override void Enter()
    {
        isKeepingReaction = player.AnimatorController.IsPlayingReaction;
        if (!isKeepingReaction)
            player.AnimatorController.PlayAction(AnimHash.Locomotion);
    }

    public override void Update()
    {
        base.HandleInput();
        if (stateMachine.CurrentState != this) return;
        if (player.InputHandler.GuardInput)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
            return;
        } //dodge, jump 동안 선입력된 가드 처리

        Vector3 moveDir = player.GetDesiredMoveDirection();
        if (isKeepingReaction && (moveDir != Vector3.zero || !player.AnimatorController.IsPlayingReaction))
        {
            isKeepingReaction = false;
            player.AnimatorController.PlayAction(AnimHash.Locomotion);
        }
        float speed = IsSprinting ? player.Motor.SprintSpeed : player.Motor.MoveSpeed;

        player.Motor.SetMovement(moveDir * speed);

        RotateToTargetOrMove(moveDir);

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
        else if (player.IsLockOn)
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

    protected override void OnGuardCommand()
    {
        player.Motor.SetMovement(Vector3.zero);
        stateMachine.TransitionTo(stateMachine.PlayerGuardState);
    }
}
