using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using Unity.VisualScripting;

public class LivingEntity : MonoBehaviour,IDamageable
{
    public float maxHp { get; protected set; }
    public float currentHp { get; protected set; }
    public float damage { get; protected set; }
    public float defense { get; protected set; }

    public bool dead { get; set;}

    protected event Action OnDeath; // 죽었을 때 이벤트


    protected virtual void OnEnable()
    {
        dead = false;
    }


    //데미지 입었을 때
    public virtual void OnDamage(DamageInfo damageInfo)
    {
        if (dead) return;

        currentHp -= damageInfo.finalDamage;

        // 체력이 0 이하가 되면 사망 처리
        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
    }

    //죽었을 때
    public virtual void Die()
    {
        if (dead) return;
        dead = true;
        OnDeath?.Invoke();
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

}
