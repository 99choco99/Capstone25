using System.Collections;
using UnityEngine;

public class EnemyWeapon : Weapon
{
    EnemyCombat EnemyCombat;
    [SerializeField] LayerMask playerLayer = 1 << 6;

    private void Awake()
    {
        EnemyCombat = GetComponentInParent<EnemyCombat>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if((1 << other.gameObject.layer) == playerLayer)
        {
            var target = other.GetComponentInParent<IDamageable>();
            if(target != null)
            {
                EnemyCombat.OnWeaponHit(target, other);
            }
        }

    }

}
