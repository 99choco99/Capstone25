using UnityEngine;

public abstract class State
{
    protected Player player;
    protected PlayerStateMachine stateMachine;

    public virtual bool UseRootMotion => false;

    public State(Player player, PlayerStateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }

    public virtual void Exit() { }

    public virtual void Update() { }

    public virtual void FixedUpdate() { }

}