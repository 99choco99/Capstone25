using UnityEngine;

public interface IWeaponOwner
{
    void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon);
}
