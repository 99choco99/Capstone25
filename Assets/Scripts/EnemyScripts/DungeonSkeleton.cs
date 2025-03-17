using UnityEngine;
using UnityEngine.AI;

public class DungeonSkeleton : LivingEntity
{
    NavMeshAgent agent;
    Animator anim;
    [SerializeField] EnemyData enemyData;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        SetUp(enemyData);
    }

    public void SetUp(EnemyData enemyData)
    {
        maxHp = enemyData.hp;
        damage = enemyData.damage;
        agent.speed = enemyData.speed;
    }

    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        base.OnDamage(damage, hitPoint, hitNormal);
        anim.SetTrigger("Hit");
    }
}
