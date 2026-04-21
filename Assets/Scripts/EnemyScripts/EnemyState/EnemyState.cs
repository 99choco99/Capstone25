using UnityEngine;

public abstract class EnemyState
{
    protected Enemy enemy;
    public EnemyState(Enemy enemy) { this.enemy = enemy; }

    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
}
