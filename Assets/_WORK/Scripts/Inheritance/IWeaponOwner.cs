using UnityEngine;

public interface IWeaponOwner
{
    Faction OwnerFaction { get; }
    void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon);
}
