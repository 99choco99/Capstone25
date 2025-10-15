
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
    public int currentAttackIndex;
    private List<Attack> _normalAttacks = new List<Attack>();
    private List<Attack> _heavyAttacks = new List<Attack>();

    [SerializeField] float guardChance;
    public bool canPerformAction = true;


    public event Action OnAttackEnd;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        InitializeAttacks();
        canPerformAction  = true;
    }

    private void InitializeAttacks()
    {
        foreach (var attack in attacks)
        {
            if (attack.type == AttackType.Normal)
                _normalAttacks.Add(attack);
            else if (attack.type == AttackType.Heavy)
                _heavyAttacks.Add(attack);
        }
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
        if (!canPerformAction  || _normalAttacks.Count == 0) { return; }

        canPerformAction  = false;

        Attack selectedAttack = _normalAttacks[Random.Range(0, _normalAttacks.Count)];
        currentAttackIndex = System.Array.IndexOf(attacks, selectedAttack);
        enemy.Motor.PlayAttackAnimation(currentAttackIndex);
    }

    //강공격
    public void PerformHeavyAttack()
    {
        if (!canPerformAction  || _heavyAttacks.Count == 0) return;
        canPerformAction  = false;

        Attack selectedAttack = _heavyAttacks[Random.Range(0, _heavyAttacks.Count)];
        currentAttackIndex = System.Array.IndexOf(attacks, selectedAttack);
        enemy.Motor.PlayHeavyAttackAnimation(currentAttackIndex);
    }

    public void DecideDefenseAction()
    {
        if (!canPerformAction ) return;

        float value = Random.Range(0f, 1f);


        if (value <= guardChance)
        {
            enemy.Stats.isDeflecting = true;
            enemy.AnimationManager.PlayAnimation("Deflect", false);
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
        canPerformAction  = true;
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
        OnAttackEnd?.Invoke();  //공격 끝남 알림
        ApplyAttackCooldown();  //공격 쿨타임
    }


}
