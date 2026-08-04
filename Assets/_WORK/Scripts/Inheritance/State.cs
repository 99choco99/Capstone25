using UnityEngine;

public enum PostureRecoveryMode
{
    Disabled,
    Normal,
    GuardBoosted
}

public abstract class State
{
    public virtual bool UseRootMotion => false;
    public virtual PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.Disabled;

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }

    public virtual void OnAnimationEnd() { }

    /// <summary>공격이 상대에게 닿아 결과가 확정됐을 때</summary>
    public virtual void HandleAttackAccepted(in DamageResult result) { }
}
