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



    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        SetUp(enemyData);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
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

        if (Vector3.Dot(result.hitDirection, transform.forward) > 0)
        {
            enemy.AnimationManager.PlayAnimation("BackHit", false);
        }

        else if (isDeflecting)
        {
            TakePostureDamage(result.finalDamage);
            EffectManager.Instance.PlayEffect("GuardHit", result.hitPoint, Quaternion.identity, transform);
            SoundManager.Instance.PlaySFX("GuardHit");
            enemy.Stats.isDeflecting = false;
        }
        else
        {
            base.OnDamage(result);
            TakePostureDamage(result.finalDamage);
            if (!enemy.AnimationManager.IsPerformAction || !enemy.Combat.canAttack)
            {
                enemy.AnimationManager.PlayAnimation("Hit", false);
            }
            EffectManager.Instance.PlayEffect("Blood", result.hitPoint, Quaternion.identity, transform);
            SoundManager.Instance.PlaySFX("Cutting flesh");
        }

        enemy.Senses.DetectWithAttack(result.player);
        OnDamaged?.Invoke(result);
    }

    public override void Die()
    {
        base.Die();
        if (lastAttacker == null)
        {
            Debug.Log("보상 없음.");
            return;
        }
        else
        {
            lastAttacker.Stats.AddExp(enemyData.exp);
            lastAttacker.Stats.AddGold(enemyData.gold);
            lastAttacker.Quest.ReportEnemyKilled(enemyData.id);
        }
    }


    public void DeathBlowProcess(Player player)
    {
        enemy.Anim.enabled = false;
        DamageInfo damage = new DamageInfo()
        {
            player = player,
            finalDamage = 99999
        };
        base.OnDamage(damage);
    }
}
