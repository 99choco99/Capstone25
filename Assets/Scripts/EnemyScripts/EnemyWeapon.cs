using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    [SerializeField] LivingEntity Enemy;
    Animator anim;
    Collider col;

    private void Awake()
    {
        anim = GetComponentInParent<Animator>();
        col = GetComponent<Collider>();
    }

    private void Update()
    {
        if (anim.GetBool("Attack")) { col.enabled = true; }
        else { col.enabled = false; }
    }
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
