using System.Collections;
using UnityEngine;

public class EnemyWeapon : Weapon
{
    LayerMask playerLayer = 1 << 6;
    [SerializeField] Enemy self;
    PlayerController target;

    Collider col;
    Vector3 hitPoint;
    Vector3 hitDirection;

    private void Awake()
    {
        self = GetComponentInParent<Enemy>();
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if((1 << other.gameObject.layer) == playerLayer && target == null)
        {
           target = other.transform.parent.GetComponent<PlayerController>();
        }
        if ((1 << other.gameObject.layer) == playerLayer)
        {
            hitPoint = other.ClosestPoint(transform.position);
            hitDirection = (target.transform.position - self.transform.position);
            hitDirection.y = 0;
            target.playerSetting.hitDirection = hitDirection.normalized;

            target.playerSetting.OnDamage(
                self.enemyAttack.currentPattern, 
                self.enemyAttack.currentAnimationIndex, 
                hitDirection);
        }
    }

}
