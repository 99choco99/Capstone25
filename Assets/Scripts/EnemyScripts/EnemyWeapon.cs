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
            PlayerData player = other.transform.parent.GetComponent<PlayerData>();

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitnormal = transform.position - other.transform.position;
            player.OnDamage(Enemy.damage, hitPoint, hitnormal);
            canTrigger = false;
            StartCoroutine(ResetTrigger());
        }
        else if (other.CompareTag("GuardState"))
        {
            PlayerData player = other.transform.parent.GetComponent<PlayerData>();
            player.Ishit = true;
            canTrigger = false;
            StartCoroutine(ResetTrigger());
        }
    }

}
