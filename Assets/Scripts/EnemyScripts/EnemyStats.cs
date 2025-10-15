using System;
using System.Collections;
using UnityEngine;

public class EnemyStats : LivingEntity
{
    Enemy enemy;

    [SerializeField] EnemyData enemyData;

    public static event Action<int> OnEnemyDied;
    public event Action<DamageInfo> OnDamaged;

    private float postureBrokenDuration = 3f; // Ã¼°£ ºØ±« Áö¼Ó ½Ã°£ (3ÃÊ)
    private float postureBrokenTimer = 0f;

    public bool IsPostureBroken {  get; private set; }
    public bool isDeflecting;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetUp(enemyData);
    }


    public void SetUp(EnemyData enemyData)
    {
        maxHp = enemyData.hp;
        currentHp = maxHp;
        damage = enemyData.damage;
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
                OnPostureChanged?.Invoke(currentPosture, maxPosture);
            }
        }
    }


    public override void OnDamage(DamageInfo result)
    {

        if (Vector3.Dot(result.hitDirection, transform.forward) > 0)
        {
            enemy.AnimationManager.PlayAnimation("BackHit", false);
        }
        else
        {
            enemy.AnimationManager.PlayAnimation("Hit", false);

        }


        if (isDeflecting)
        {
            Quaternion effectRotation = Quaternion.LookRotation(result.hitDirection);
            EffectManager.Instance.PlayEffect("GuardHit", result.hitPoint, Quaternion.identity, transform);
            TakePostureDamage(result.finalDamage);
            isDeflecting = false;
        }
        else
        {
            base.OnDamage(result);
            TakePostureDamage(result.finalDamage);
            EffectManager.Instance.PlayEffect("Blood", result.hitPoint, Quaternion.identity, transform);
            SoundManager.Instance.PlaySFX("Cutting flesh");
        }

        OnDamaged?.Invoke(result);
    }
}
