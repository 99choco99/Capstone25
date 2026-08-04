using UnityEngine;


/// <summary>
/// 무기를 지닌 객체들
/// </summary>
public interface IWeaponOwner
{
    Faction OwnerFaction { get; }
    void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon, Vector3 hitPoint);
}
