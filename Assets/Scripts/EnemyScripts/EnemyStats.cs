using System;
using System.Collections;
using UnityEngine;

public class EnemyStats : LivingEntity
{
    Enemy enemy;

    [SerializeField] EnemyData enemyData;

    public static event Action<int> OnEnemyDied;
    public event Action<DamageInfo> OnDamaged;

    private float postureBrokenDuration = 3f; // 체간 붕괴 지속 시간 (3초)
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
            EffectManager.Instance.PlayEffect("GuardHit", result.hitPoint, effectRotation);
            TakePostureDamage(result.finalDamage);
            isDeflecting = false;
        }
        else
        {
            base.OnDamage(result);
            TakePostureDamage(result.finalDamage);
            Quaternion effectRotation = Quaternion.LookRotation(result.hitPoint);
            GameObject bloodEffect = EffectManager.Instance.PlayEffect("Blood", result.hitPoint, effectRotation);
            SoundManager.Instance.PlaySFX("Cutting flesh");
            if (bloodEffect != null)
            {
                // bloodEffect의 부모를 피격된 적인 result.victim으로 설정합니다.
                bloodEffect.transform.SetParent(transform);
            }
        }

        Debug.Log("6");
        OnDamaged?.Invoke(result);
    }

    public override void Die()
    {
        base.Die();
        StartCoroutine(Disappear());
    }

    //죽은 후 2.5초뒤 시체 없어짐.
    IEnumerator Disappear()
    {
        yield return new WaitForSeconds(2.5f);
        Destroy(gameObject);
    }
}
