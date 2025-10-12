using System;
using System.Collections;
using Unity.Netcode;
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


    public override void OnDamage(DamageInfo damageInfo)
    {

        if(Vector3.Dot(damageInfo.hitDirection,transform.forward) < 0)
        {
            if (enemy.Senses.IsPlayerAttacking)
            {
                enemy.Combat.DecideDefenseAction();
            }
        }
        else
        {
            enemy.AnimationManager.PlayAnimation("BackHit", true);
        }


        AnimatorStateInfo stateInfo = enemy.Anim.GetCurrentAnimatorStateInfo(0);

        bool isDeflecting = stateInfo.IsTag("Deflect");

        if (isDeflecting)
        {
            Debug.Log("Àû: Æ¨°Ü³»±â ¼º°ø!");
            TakePostureDamage(damageInfo.finalDamage);
        }
        else
        {
            base.OnDamage(damageInfo);
            TakePostureDamage(damageInfo.finalDamage);
;       }

        OnDamaged?.Invoke(damageInfo);
    }

    public override void Die()
    {
        base.Die();
        StartCoroutine(Disappear());
    }

    //Á×Àº ÈÄ 2.5ÃÊµÚ ½ÃÃ¼ ¾ø¾îÁü.
    IEnumerator Disappear()
    {
        yield return new WaitForSeconds(2.5f);
        Destroy(gameObject);
    }
}
