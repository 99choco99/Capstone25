using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    [SerializeField] LivingEntity Enemy;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.transform.parent.GetComponent<Player>();

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitnormal = transform.position - other.transform.position;
            player.OnDamage(Enemy.damage, hitPoint, hitnormal);
        }
    }
}
