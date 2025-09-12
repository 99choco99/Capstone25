using UnityEngine;

public interface IDamageable
{
    bool dead { get; }
    GameObject gameObject { get; }
    Transform transform { get; }
    public void OnDamage(DamageInfo Info);
}
