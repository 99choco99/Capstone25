using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EnemyStats : LivingEntity
{
    Enemy enemy;

    [SerializeField] EnemyData enemyData;


    public event Action<DamageInfo> OnDamaged;
    public static event Action<int> OnEnemyDied;

    private float postureBrokenDuration = 3f; // 체간 붕괴 지속 시간 (3초)
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

        // --- 1. 현재 자신의 상태 확인 ---
        AnimatorStateInfo stateInfo = enemy.Anim.GetCurrentAnimatorStateInfo(0);

        bool isDeflecting = stateInfo.IsTag("Deflect");
        bool isGuarding = stateInfo.IsTag("Guard");

        if (isDeflecting)
        {
            Debug.Log("적: 튕겨내기 성공!");

            // a) 나는 데미지(체력, 체간)를 입지 않음

            // b) 반격: 공격자의 체간에 큰 데미지를 줌 (세키로 핵심)
            //    NetworkManager를 통해 공격자(플레이어)를 찾아 TakePostureDamage를 호출


            // c) 튕겨내기 성공 이펙트(번쩍!), 사운드 재생 요청
            // EffectManager.Instance.SpawnEffect("DeflectSpark", ...);
        }
        // 일반 가드 성공
        else if (isGuarding)
        {
            Debug.Log("적: 가드 성공!");
            TakePostureDamage(damageInfo.finalDamage);
        }
        //그냥 맞음
        else
        {
            base.OnDamage(damageInfo);
            TakePostureDamage(damageInfo.finalDamage);
;        }
        OnDamaged?.Invoke(damageInfo);
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
