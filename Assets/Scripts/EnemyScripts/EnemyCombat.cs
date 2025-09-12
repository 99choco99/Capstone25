using Unity.Netcode;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    private Enemy enemy;
    [SerializeField] EnemyWeapon weapon;
    [SerializeField] Attack[] attacks;
    public int currentAttackIndex;

    // 한 번의 공격에 여러 번 피격되는 것을 방지
    private bool IsWeanponHit;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    public void OnWeaponHit(IDamageable target, Collider targetCollider)
    {
        if (IsWeanponHit) { return; }
        IsWeanponHit = true;

        Attack currentAttackData = attacks[currentAttackIndex];

        Vector3 hitPoint = targetCollider.ClosestPoint(transform.position);
        Vector3 hitDirection = (targetCollider.transform.position - transform.position).normalized;
        hitDirection.y = 0;

        DamageInfo damageInfo = new DamageInfo
        {
            finalDamage = currentAttackData.damage,
            knockbackForce = currentAttackData.knockbackPower,
            knockbackDuration = currentAttackData.knockbackDuration,
            hitPoint = hitPoint,
            hitDirection = hitDirection,
            wasGuarded = false,
            wasParried = false,
        };
        target.OnDamage(damageInfo);
    }



    public void AE_EnemyAttackStart()
    {
        weapon.enabled = true;
    }

    public void AE_EnemyAttackEnd()
    {
        weapon.enabled = false;
    }
}
