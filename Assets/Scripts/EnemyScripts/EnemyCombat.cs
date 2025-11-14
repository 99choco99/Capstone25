
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyCombat : MonoBehaviour,IWeaponOwner
{

    private Enemy enemy;
    [SerializeField] private List<Weapon> weapons = new List<Weapon>();

    [SerializeField] Attack[] attacks;
    public int currentAttackIndex = 0;


    [SerializeField] float guardChance;
    public bool canAttack = true;


    private void Awake()
    {
        enemy = GetComponent<Enemy>();


    }

    // 플레이어를 공격했을 때
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon)
    {
        Attack currentAttackData = attacks[currentAttackIndex];
        Collider weaponCollider = weapon.GetComponent<Collider>();
        Vector3 hitPoint = targetCollider.ClosestPoint(weaponCollider.transform.position);
        Vector3 hitDirection = (targetCollider.transform.position - transform.position);
        hitDirection.y = 0;
        hitDirection.Normalize();

        DamageInfo damageInfo = new DamageInfo
        {
            enemy = enemy,
            finalDamage = currentAttackData.damage,
            attackType = currentAttackData.type,
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
        if (enemy.AnimationManager.IsPerformAction || !canAttack) { return; }
        canAttack  = false;


        Attack selectedAttack = attacks[Random.Range(0, attacks.Length)];
        if(selectedAttack.type == AttackType.Heavy)
        {
            SoundManager.Instance.PlaySFX("HeavyAttack");
            if (enemy.UI != null)
            {
                enemy.UI.ShowHeavyAttackIndicator();
            }
        }
        currentAttackIndex = System.Array.IndexOf(attacks, selectedAttack);
        
        enemy.AnimationManager.PlayAnimation($"Attack{currentAttackIndex}", true);
        ApplyAttackCooldown();  //공격 쿨타임
    }

    //방어
    public void DecideDefenseAction()
    {
        if (enemy.Stats.dead || enemy.Stats.IsPlayingDeathBlow) { return; }
        if (enemy.AnimationManager.IsPerformAction) return;
        float value = Random.Range(0f, 1f);

        if (value <= guardChance)
        {
            enemy.AnimationManager.PlayAnimation("Deflect", false);
            enemy.Stats.isDeflecting = true;
            enemy.Motor.Stop();
        }
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

    public void AE_EnemyAttackStart()
    {
        foreach (var weapon in weapons)
        {
            weapon.EnableWeaponCollider();
        }
        SoundManager.Instance.PlaySFX("Attack");

    }

    public void EnemyAttackEnd()
    {
        foreach (var weapon in weapons)
        {
            weapon.DisableWeaponCollider();
        }
    }


}
