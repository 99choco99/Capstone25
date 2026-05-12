using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using Unity.VisualScripting;

public class LivingEntity : MonoBehaviour,IDamageable
{
    public Stat AttackPower { get; protected set; }
    public Stat Defense { get; protected set; }
    public Stat MaxHp { get; protected set; }
    public Stat MaxPosture { get; protected set; }
    public float CurrentPosture { get; protected set; }
    public float CurrentHp { get; protected set; }

    protected float postureRecoveryRate = 1f;
    [SerializeField] protected float postureRecoveryTimer = 2f;

    public bool dead { get; set; }

    public event Action<float, float> OnHpChanged;  // hp 변경
    public event Action<float, float> OnPostureChanged; //가드 게이지 적용
    public event Action OnPostureBroken;
    public event Action OnDeath; // 죽었을 때 이벤트


    protected void Awake()
    {
        //기본값
        MaxHp = new Stat(100f);
        AttackPower = new Stat(10f);
        Defense = new Stat(0f);
        MaxPosture = new Stat(100f);
    }

    protected virtual void OnEnable(){dead = false;}

    protected virtual void Update()
    {
        if (postureRecoveryTimer > 0)
        {
            postureRecoveryTimer -= Time.deltaTime;
        }
        else if (CurrentPosture > 0)
        {
            CurrentPosture -= postureRecoveryRate * Time.deltaTime;
            CurrentPosture = Mathf.Max(CurrentPosture, 0); // 0 이하로 내려가지 않도록
            OnPostureChanged?.Invoke(CurrentPosture, MaxPosture.GetValue());
        }
    }

    //데미지 입었을 때
    public virtual void OnDamage(DamageInfo damageInfo)
    {
        if (dead) return;

        CurrentHp -= damageInfo.amount;
        OnHpChanged?.Invoke(CurrentHp, MaxHp.GetValue());
        // 체력이 0 이하가 되면 사망 처리
        if (CurrentHp <= 0)
        {
            CurrentHp = 0;
            Die();
        }

    }


    //체간 데미지 받기
    public virtual void TakePostureDamage(float amount)
    {
        if (dead) return;

        CurrentPosture += amount;
        OnPostureChanged?.Invoke(CurrentPosture, MaxPosture.GetValue());

        // 체간 회복 시작 딜레이 초기화
        postureRecoveryTimer = 2f;

        if (CurrentPosture >= MaxPosture.GetValue())
        {
            CurrentPosture = MaxPosture.GetValue();
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
        if (CurrentHp + heal >= MaxHp.GetValue()) CurrentHp = MaxHp.GetValue();
        else CurrentHp += heal;

        OnHpChanged?.Invoke(CurrentHp, MaxHp.GetValue());
    }

}
