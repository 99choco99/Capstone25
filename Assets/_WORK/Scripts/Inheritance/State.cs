using UnityEngine;

public abstract class State
{
    public virtual bool UseRootMotion => false;

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }

    public virtual void OnAnimationEnd() { }
}
