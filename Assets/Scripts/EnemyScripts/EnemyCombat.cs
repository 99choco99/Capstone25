
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyCombat : MonoBehaviour,IWeaponOwner
{
    public Faction OwnerFaction => Faction.EnemyTeam;
    private Enemy enemy;
    [SerializeField] private List<Weapon> weapons = new List<Weapon>();
    [SerializeField] EnemyAttackData[] attacks;

    public int currentAttackIndex = 0;
    [SerializeField] float guardChance;


    private float nextAttackTime = 0f;
    public bool canAttack => Time.time >= nextAttackTime;


    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    // 플레이어를 공격했을 때
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon)
    {
        AttackData currentAttackData = attacks[currentAttackIndex];
        Collider weaponCollider = weapon.GetComponent<Collider>();
        Vector3 hitPoint = targetCollider.ClosestPoint(weaponCollider.transform.position);

        Vector3 hitDirection = (targetCollider.transform.position - transform.position);
        hitDirection.y = 0;
        hitDirection.Normalize();

        DamageInfo damageInfo = new DamageInfo
        {
            attacker = enemy.gameObject,
            amount = currentAttackData.damage,
            attackType = currentAttackData.type,
            knockbackForce = currentAttackData.knockbackPower,
            knockbackDuration = currentAttackData.knockbackDuration,
            hitPoint = hitPoint,
            hitDirection = hitDirection,
            wasGuarded = false,
            wasParried = false,
        };

        target.TakeDamage(damageInfo);
    }

    private EnemyAttackData ChooseBestAttack()
    {
        float distance = enemy.Senses.DistanceToTarget;
        List<EnemyAttackData> validAttacks = new List<EnemyAttackData>();
        float totalWeight = 0f;

        foreach (EnemyAttackData attack in attacks)
        {
            if (attack.minDistance >= distance && attack.maxDistance <= distance)
            {
                validAttacks.Add(attack);
                totalWeight += attack.minDistance;
            }
        }

        if(validAttacks.Count == 0) { return null;}

        float randomValue = Random.Range(0, totalWeight);
        float weightSum = 0f;
        foreach (EnemyAttackData attack in validAttacks)
        {
            if(weightSum >= randomValue) { return attack; }
            weightSum += attack.weight;
        }

        return validAttacks[0];
    }

    //공격
    public void PerformAttack()
    {
        if (enemy.AnimationManager.IsPerformAction || !canAttack) { return; }

        EnemyAttackData selectedAttack = ChooseBestAttack();

        if (selectedAttack == null) { return; }
        if (selectedAttack.type == AttackType.Heavy)
        {
            SoundManager.Instance.PlaySFX("HeavyAttack");
            if (enemy.UI != null)
            {
                enemy.UI.ShowHeavyAttackIndicator();
            }
        }
        currentAttackIndex = System.Array.IndexOf(attacks, selectedAttack);
        enemy.AnimationManager.PlayAnimation($"AttackData{currentAttackIndex}", true);


        float randomCooldown = Random.Range(selectedAttack.minAttackCooldown, selectedAttack.maxAttackCooldown);
        nextAttackTime = Time.time + randomCooldown;
    }
    //방어
    public void DecideDefenseAction()
    {
        if (enemy.Stats.IsDead || enemy.Stats.IsPlayingDeathBlow || enemy.AnimationManager.IsPerformAction) { return; }
        if (Random.value <= guardChance)
        {
            enemy.AnimationManager.PlayAnimation("Deflect", false);
            enemy.Stats.isDeflecting = true;
            enemy.Motor.Stop();
        }
    }
    public void OnEnemyAttackStart()
    {
        foreach (var weapon in weapons)
        {
            weapon.EnableWeaponCollider();
        }
        SoundManager.Instance.PlaySFX("AttackData");

    }

    public void EnemyAttackEnd()
    {
        foreach (var weapon in weapons)
        {
            weapon.DisableWeaponCollider();
        }
    }


}
