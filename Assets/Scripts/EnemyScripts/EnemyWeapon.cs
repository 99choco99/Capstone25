using System.Collections;
using UnityEngine;

public class EnemyWeapon : Weapon
{
    [SerializeField] LivingEntity Enemy;
    Animator anim;
    Collider col;

    private void Awake()
    {
        anim = GetComponentInParent<Animator>();
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canTrigger) { return; }
        if (other.CompareTag("Player"))
        {
            Player player = other.transform.parent.GetComponent<Player>();

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitnormal = transform.position - other.transform.position;
            player.OnDamage(Enemy.damage, hitPoint, hitnormal);
            canTrigger = false;
            StartCoroutine(ResetTrigger());
        }
        else if (other.CompareTag("GuardState"))
        {
            Player player = other.transform.parent.GetComponent<Player>();
            player.Ishit = true;
            canTrigger = false;
            StartCoroutine(ResetTrigger());
        }
    }

}
