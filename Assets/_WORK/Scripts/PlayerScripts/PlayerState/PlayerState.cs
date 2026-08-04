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
    public virtual bool CanBeInterruptedByHit => true;

    public virtual void HandleDamage(DamageEvent damageEvent)
    {
        if (!CanBeInterruptedByHit) return;
        stateMachine.PlayerHitState.SetHitData(damageEvent);
        stateMachine.TransitionTo(stateMachine.PlayerHitState);
    }
    public virtual void OnPostureBroken()
    {
        stateMachine.PlayerStunState.SetStunData(AnimHash.Stun);
        stateMachine.TransitionTo(stateMachine.PlayerStunState);
    }

    public virtual void HandleInput()
    {
        ActionCommand cmd = player.InputBuffer.PeekValidCommand();
        if (cmd == ActionCommand.None) return;
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
        }
    }
    protected virtual void OnAttackCommand() { }
    protected virtual void OnDodgeCommand() { }
    protected virtual void OnJumpCommand() { }
}