using UnityEngine;

public interface IDamageable
{
    Faction TargetFaction { get; }

    bool IsDead { get; }
    GameObject gameObject { get; }
    Transform transform { get; }
    public void TakeDamage(DamageInfo Info);
    public void Die();
}
