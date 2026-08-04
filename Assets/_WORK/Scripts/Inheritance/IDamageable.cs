using UnityEngine;

public interface IDamageable
{
    Faction TargetFaction { get; }

    bool IsDead { get; }
    GameObject gameObject { get; }
    Transform transform { get; }
    public void TakeDamage(ref DamageEvent Info);
    public void Die();
}
