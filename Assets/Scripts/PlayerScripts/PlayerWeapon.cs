using System.Collections;
using UnityEngine;

public class PlayerWeapon : Weapon
{
    [SerializeField] LayerMask layerMask;
    PlayerCombat owner;

    private void Awake()
    {
        owner = GetComponentInParent<PlayerCombat>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (owner != null) { return; }
        if ((1 << other.gameObject.layer) == layerMask)
        {
            if (other.TryGetComponent<IDamageable>(out var target))
            {
                owner.OnWeaponHit(target, other);
            }
        }
    }


}
