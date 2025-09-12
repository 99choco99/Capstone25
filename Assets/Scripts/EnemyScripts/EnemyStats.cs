using System;
using System.Collections;
using UnityEngine;

public class EnemyStats : LivingEntity
{
    [SerializeField] EnemyData enemyData;
    public event Action<DamageInfo> OnDamaged;


    private void Start()
    {
        SetUp(enemyData);
    }


    public void SetUp(EnemyData enemyData)
    {
        maxHp = enemyData.hp;
        damage = enemyData.damage;
        base.OnEnable();
        OnDeath += Die;
    }



    public override void OnDamage(DamageInfo damageInfo)
    {
        base.OnDamage(damageInfo);
        OnDamaged?.Invoke(damageInfo);
    }

    public override void Die()
    {
        base.Die();
        StartCoroutine(Disappear());
    }

    //죽은 후 2.5초뒤 시체 없어짐.
    IEnumerator Disappear()
    {
        yield return new WaitForSeconds(2.5f);
        Destroy(gameObject);
    }
}
