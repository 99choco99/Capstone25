using System;
using UnityEngine;

public class LivingEntity : MonoBehaviour,IDamageable
{

    public float maxHp { get; protected set; }   // 최대 체력
    public float currentHp { get; protected set; }  // 현재 체력
    public float damage { get; protected set; }// 공격력
    public bool dead { get; protected set; }  // 죽음

    protected event Action OnDeath; // 죽었을 때 이벤트

    //데미지 입었을 때
    public virtual void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal) {
        currentHp -= damage;
        if(currentHp <= 0 && !dead)
        {
            Die();
        }
    }

    //죽었을 때
    public virtual void Die()
    {
        OnDeath?.Invoke();
        dead = true;
    }


    // 피회복
    public virtual void RestoreHealth(float heal)
    {
        if (currentHp + heal >= maxHp)
        {
            currentHp = maxHp;
        }
        else
        {
            currentHp += heal;
        }
    }

    // 생명체 활성화 시 상태 리셋
    protected virtual void OnEnable() {
        dead = false;
        currentHp = maxHp;
    }
}
