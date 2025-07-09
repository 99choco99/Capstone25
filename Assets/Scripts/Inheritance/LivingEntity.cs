using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;

public class LivingEntity : MonoBehaviour,IDamageable
{
    public int level = 0;
    public float maxHp;  // 최대 체력
    public float currentHp;  // 현재 체력
    public float damage;// 공격력
    public float defense; //방어력
    public float speed; //이동속도

    public float D_health;
    public float D_speed;
    public float D_damage;
    public float D_defense;
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
