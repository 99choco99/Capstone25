using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using Unity.VisualScripting;

public class LivingEntity : MonoBehaviour,IDamageable
{
    public float maxHp { get; protected set; }
    public float currentHp { get; protected set; }
    public float damage { get; protected set; }
    public float maxPosture { get; protected set; }
    public float currentPosture { get; protected set; }
    protected float postureRecoveryRate = 1f;
    [SerializeField] protected float postureRecoveryTimer = 2f;

    public bool dead { get; set;}
    protected Player lastAttacker;


    public event Action<float> OnHpChanged;  // hp 변경
    public event Action<float, float> OnPostureChanged; //가드 게이지 적용
    public event Action OnPostureBroken;
    public event Action OnDeath; // 죽었을 때 이벤트


    protected virtual void OnEnable()
    {
        dead = false;
    }

    protected virtual void Update()
    {
        if (postureRecoveryTimer > 0)
        {
            postureRecoveryTimer -= Time.deltaTime;
        }
        else if (currentPosture > 0)
        {
            currentPosture -= postureRecoveryRate * Time.deltaTime;
            currentPosture = Mathf.Max(currentPosture, 0); // 0 이하로 내려가지 않도록
            OnPostureChanged?.Invoke(currentPosture, maxPosture);
        }
    }

    //데미지 입었을 때
    public virtual void OnDamage(DamageInfo damageInfo)
    {
        if (dead) return;

        if (damageInfo.player != null)
        {
            lastAttacker = damageInfo.player;
        }


        currentHp -= damageInfo.finalDamage;
        OnHpChanged?.Invoke(currentHp);
        // 체력이 0 이하가 되면 사망 처리
        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }

    }


    //체간 데미지 받기
    public virtual void TakePostureDamage(float amount)
    {
        if (dead) return;

        currentPosture += amount;
        OnPostureChanged?.Invoke(currentPosture, maxPosture);

        // 체간 회복 시작 딜레이 초기화
        postureRecoveryTimer = 2f;

        if (currentPosture >= maxPosture)
        {
            currentPosture = maxPosture;
            OnPostureBroken?.Invoke();
        }
    }

    //죽었을 때
    public virtual void Die()
    {
        if (dead) return;
        dead = true;
        OnDeath?.Invoke();
    }


    // 피회복
    public virtual void RestoreHealth(float heal)
    {
        if (currentHp + heal >= maxHp)
        {
            currentHp = maxHp;
        }
        else
        {
            currentHp += heal;
        }

        OnHpChanged?.Invoke(currentHp);
    }

}
