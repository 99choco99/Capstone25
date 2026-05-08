using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyStats : LivingEntity
{
    Enemy enemy;

    [SerializeField] EnemyData enemyData;

    public event Action<DamageInfo> OnDamaged;

    private float postureBrokenDuration = 3f; // 체간 붕괴 지속 시간 (3초)
    private float postureBrokenTimer = 0f;

    public bool IsPostureBroken {  get; private set; }
    public bool isDeflecting;
    public bool IsPlayingDeathBlow;


    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        SetUp(enemyData);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    private void OnDestroy()
    {
        OnPostureBroken -= HandlePostureBroken;
        OnDeath -= Die;
    }


    public void SetUp(EnemyData enemyData)
    {
        maxHp = enemyData.hp;
        currentHp = maxHp;
        maxPosture = enemyData.defense;


        OnDeath += Die;
        OnPostureBroken += HandlePostureBroken;
    }

    void HandlePostureBroken()
    {
        IsPostureBroken = true;
        postureBrokenTimer = postureBrokenDuration;
    }

    protected override void Update()
    {
        base.Update();

        if (IsPostureBroken) 
        {
            postureBrokenTimer -= Time.deltaTime;
            if(postureBrokenTimer <= 0f)
            {
                IsPostureBroken = false;
                currentPosture = 0;
            }
        }
    }


    public override void OnDamage(DamageInfo result)
    {
        if (dead) return;

        if (isDeflecting)
        {
            Quaternion effectRotation = Quaternion.LookRotation(result.hitDirection);
            EffectManager.Instance.PlayEffect("GuardHit", result.hitPoint, effectRotation, transform);
            SoundManager.Instance.PlaySFX("GuardHit");
            enemy.Stats.isDeflecting = false;
            TakePostureDamage(result.damage * 0.5f);
        }
        else if (Vector3.Dot(result.hitDirection, transform.forward) > 0)
        {
            enemy.AnimationManager.PlayAnimation("BackHit", false);
            base.OnDamage(result);
            EffectManager.Instance.PlayEffect("Blood", result.hitPoint, Quaternion.identity, transform);
            SoundManager.Instance.PlaySFX("Cutting flesh");
            TakePostureDamage(result.damage);
        }
        else
        {
            base.OnDamage(result);
            if (!enemy.AnimationManager.IsPerformAction || !enemy.Combat.canAttack)
            {
                enemy.AnimationManager.PlayAnimation("Hit", false);
            }
            EffectManager.Instance.PlayEffect("Blood", result.hitPoint, Quaternion.identity, transform);
            SoundManager.Instance.PlaySFX("Cutting flesh");
            TakePostureDamage(result.damage);
        }

        enemy.Senses.DetectWithAttack(result.player);
        OnDamaged?.Invoke(result);
    }

    public override void Die()
    {
        if(dead) return;//한번만 실행되도록

        enemy.Combat.EnemyAttackEnd();
        base.Die();
    }


    public void DeathBlowProcess(Player player)
    {
        enemy.Anim.enabled = false;
        enemy.Combat.EnemyAttackEnd();
        DamageInfo damage = new DamageInfo()
        {
            player = player,
            damage = 99999
        };
        base.OnDamage(damage);
    }
}
