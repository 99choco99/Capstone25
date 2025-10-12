using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyCombat : MonoBehaviour,IWeaponOwner
{

    private Enemy enemy;
    [SerializeField] private List<Weapon> weapons = new List<Weapon>();

    [SerializeField] Attack[] attacks;
    public int currentAttackIndex;

    [SerializeField] float guardChance;
    public bool canAttack = true;


    public event Action OnAttackEnd;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        canAttack = true;
    }


    // 적을 공격했을 때
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon)
    {
        Attack currentAttackData = attacks[currentAttackIndex];
        Collider weaponCollider = weapon.GetComponent<Collider>();
        Vector3 hitPoint = targetCollider.ClosestPoint(transform.position);
        Vector3 hitDirection = transform.forward;

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

    //공격
    public void PerformAttack()
    {
        if (!canAttack) { return; }

        List<Attack> normalAttacks = new List<Attack>();
        foreach (var attack in attacks)
        {
            if(attack.type == AttackType.Normal)
            {
                normalAttacks.Add(attack);
            }
        }

        if (normalAttacks.Count == 0)
        {
            Debug.LogWarning("실행할 Normal Attack이 없습니다.");
            return;
        }
        canAttack = false;

        Attack selectedAttack = normalAttacks[Random.Range(0, normalAttacks.Count)];
        currentAttackIndex = System.Array.IndexOf(attacks, selectedAttack);

        // Motor에게 애니메이션 재생을 요청
        enemy.Motor.PlayAttackAnimation(currentAttackIndex);
    }

    //강공격
    public void PerformHeavyAttack()
    {
        if (!canAttack) { return; }

        List<Attack> heavyAttacks = new List<Attack>();
        foreach (var attack in attacks)
        {
            if (attack.type == AttackType.Normal)
            {
                heavyAttacks.Add(attack);
            }
        }

        if (heavyAttacks.Count == 0)
        {
            Debug.LogWarning("실행할 Normal Attack이 없습니다.");
            return;
        }
        canAttack = false;

        Attack selectedAttack = heavyAttacks[Random.Range(0, heavyAttacks.Count)];
        currentAttackIndex = System.Array.IndexOf(attacks, selectedAttack);

        enemy.Motor.PlayHeavyAttackAnimation(currentAttackIndex);
    }


    public void DecideDefenseAction()
    {
        if (!canAttack) return;

        float value = Random.Range(0f, 1f);


        if (value <= guardChance)
        {
            enemy.AnimationManager.PlayAnimation("Deflect", true);
        }
        else
        {
            enemy.AnimationManager.PlayAnimation("Hit", true);
        }

        enemy.Motor.Stop();
    }

    public void ApplyAttackCooldown()
    {
        Attack currentAttackData = attacks[currentAttackIndex];
        float randomCooldown = Random.Range(currentAttackData.minAttackCooldown, currentAttackData.maxAttackCooldown);
        StartCoroutine(AttackTimer(randomCooldown));
    }

    IEnumerator AttackTimer(float timer)
    {
        yield return new WaitForSeconds(timer);
        canAttack = true;
    }

    public int GetAttackCount()
    {
        return attacks.Count();
    }

    public void AE_EnemyAttackStart()
    {
        foreach (var weapon in weapons)
        {
            weapon.EnableWeaponCollider();
        }
        SoundManager.Instance.PlaySFX("Attack");
    }

    public void AE_EnemyAttackEnd()
    {
        foreach (var weapon in weapons)
        {
            weapon.DisableWeaponCollider();
        }
        OnAttackEnd?.Invoke();
        ApplyAttackCooldown();
    }
}
