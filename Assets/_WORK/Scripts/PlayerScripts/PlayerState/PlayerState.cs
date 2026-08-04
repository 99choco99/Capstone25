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
        if (cmd == ActionCommand.None) return;
        player.InputBuffer.ConsumeCurrentCommand();
        switch (cmd)
        {
            case ActionCommand.Attack: OnAttackCommand(); break;
            case ActionCommand.Dodge: OnDodgeCommand(); break;
            case ActionCommand.Jump: OnJumpCommand(); break;
            case ActionCommand.Guard: OnGuardCommand(); break;
        }
    }
    protected virtual void OnAttackCommand() { }
    protected virtual void OnDodgeCommand() { }
    protected virtual void OnJumpCommand() { }
    protected virtual void OnGuardCommand() { }

    /// <summary>
    /// 인살이 가능한 상태일 때 인살로 전환할 수 있도록
    /// </summary>
    protected bool RequestDeathblow()
    {
        if (player.TargetingSystem == null) return false;
        if (!player.TargetingSystem.GetDeathblowPlan(out DeathblowPlan plan))
            return false;

        stateMachine.RequestedDeathblowPlan = plan;
        stateMachine.TransitionTo(stateMachine.PlayerExecuteState);
        return true;
    }
}
