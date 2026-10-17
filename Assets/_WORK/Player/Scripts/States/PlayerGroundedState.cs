using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.Normal;

    private bool IsSprinting => player.InputHandler.SprintInput
        && player.InputHandler.MoveInput != Vector3.zero
        && !player.IsLockOn;
    private bool isKeepingMotion;

    public override void Enter()
    {
        bool isPlayingReaction = player.AnimatorController.IsPlayingReaction;
        bool isPlayingGuardRelease = player.AnimatorController.IsPlayingGuardRelease;
        isKeepingMotion = isPlayingReaction || isPlayingGuardRelease;
        if (!isKeepingMotion)
            player.AnimatorController.PlayAction(AnimHash.Locomotion);
    }

    public override void Update()
    {
        base.HandleInput();
        if (stateMachine.CurrentState != this)
            return;
        if (player.InputHandler.GuardInput)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGuardState);
            return;
        } //dodge, jump 동안 선입력된 가드 처리

        Vector3 moveDir = player.GetDesiredMoveDirection();
        if (isKeepingMotion && (moveDir != Vector3.zero
            || !(player.AnimatorController.IsPlayingReaction || player.AnimatorController.IsPlayingGuardRelease)))
        {
            isKeepingMotion = false;
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
        else
        {
            Vector2 input = GetLocomotionInput(moveDir);
            player.AnimatorController.UpdateLocomotion(input.x, input.y);
        }
    }

    protected override void OnAttackCommand()
    {
        if (RequestDeathblow())
            return;

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
