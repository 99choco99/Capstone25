using UnityEngine;

public abstract class PlayerState : State
{
    protected Player player;
    protected PlayerStateMachine stateMachine;

    public PlayerState(Player player, PlayerStateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    public virtual void HandleDamage(DamageResult result)
    {
        if (result.DefenseType == DefenseType.None && result.Request.HitReaction == HitReactionType.Knockdown)
        {
            if (stateMachine.CurrentState == stateMachine.PlayerKnockdownState)
                stateMachine.PlayerKnockdownState.RestartHit();
            else
                stateMachine.TransitionTo(stateMachine.PlayerKnockdownState);
            return;
        }

        if (stateMachine.CurrentState == stateMachine.PlayerHitState)
        {
            stateMachine.PlayerHitState.RestartHit(result);
            return;
        }

        stateMachine.PlayerHitState.SetHitData(result);
        stateMachine.TransitionTo(stateMachine.PlayerHitState);
    }

    public virtual void HandleInput()
    {
        ActionCommand cmd = player.InputBuffer.PeekValidCommand();
        if (cmd == ActionCommand.None)
            return;
        if (cmd == ActionCommand.Attack && !player.Combat.CanStartAttack)
        {
            if (RequestDeathblow())
                player.InputBuffer.ConsumeCurrentCommand();
            return;
        }
        player.InputBuffer.ConsumeCurrentCommand();
        switch (cmd)
        {
            case ActionCommand.Attack:
                OnAttackCommand();
                break;
            case ActionCommand.Dodge:
                OnDodgeCommand();
                break;
            case ActionCommand.Jump:
                OnJumpCommand();
                break;
            case ActionCommand.Guard:
                OnGuardCommand();
                break;
        }
    }

    protected virtual void OnAttackCommand()
    {
    }

    protected virtual void OnDodgeCommand()
    {
        stateMachine.TransitionTo(stateMachine.PlayerDodgeState);
    }

    protected virtual void OnJumpCommand()
    {
        if (player.Motor.IsGrounded)
        {
            stateMachine.TransitionTo(stateMachine.PlayerJumpState);
        }
    }

    protected virtual void OnGuardCommand()
    {
    }

    protected Vector2 GetLocomotionInput(Vector3 moveDir)
    {
        if (player.IsLockOn)
        {
            Vector3 localMove = player.transform.InverseTransformDirection(moveDir);
            return new Vector2(localMove.x, localMove.z);
        }

        return new Vector2(0f, player.InputHandler.MoveAmount);
    }

    protected void RotateToTargetOrMove(Vector3 moveDir)
    {
        Vector3 direction;
        if (player.IsLockOn)
            direction = player.TargetingSystem.CurrentTarget.TargetTransform.position - player.transform.position;
        else
            direction = moveDir;

        player.Motor.RotateToDirection(direction);
    }

    /// <summary>
    /// 인살이 가능한 상태일 때 인살로 전환할 수 있도록
    /// </summary>
    protected bool RequestDeathblow()
    {
        if (player.TargetingSystem == null)
            return false;
        if (!player.TargetingSystem.GetDeathblowPlan(out DeathblowPlan plan))
            return false;

        stateMachine.RequestedDeathblowPlan = plan;
        stateMachine.TransitionTo(stateMachine.PlayerExecuteState);
        return true;
    }
}
