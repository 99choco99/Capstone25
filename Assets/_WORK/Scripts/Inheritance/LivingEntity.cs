using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using Unity.VisualScripting;

public abstract class LivingEntity : MonoBehaviour,IDamageable
{
    public abstract Faction TargetFaction { get; }
    public Stat AttackPower { get; protected set; }
    public Stat Defense { get; protected set; }
    public Stat MaxHp { get; protected set; }
    public Stat MaxPosture { get; protected set; }
    public float CurrentPosture { get; protected set; }
    public float CurrentHp { get; protected set; }

    [SerializeField] protected float postureRecoveryDelay = 2f; // 피격 후 회복 시작까지의 대기 시간
    [SerializeField] protected float basePostureRecoveryRate = 20f; // 기본 회복 속도
    private float currentRecoveryTimer;
    private Coroutine rapidDrainCoroutine;

    public bool IsDead { get; set; }

    public event Action<float, float> OnHpChanged;  // maxHp 변경
    public event Action<float, float> OnPostureChanged; //가드 게이지 적용
    public event Action OnPostureBroken;
    public event Action OnDeath; // 죽었을 때 이벤트


    protected virtual void Awake()
    {
        //기본값
        MaxHp = new Stat(100f);
        AttackPower = new Stat(10f);
        Defense = new Stat(0f);
        MaxPosture = new Stat(100f);
    }

    protected virtual void OnEnable(){IsDead = false;}

    protected virtual void Update()
    {
        HandlePostureAutoRecovery();
    }

    private void HandlePostureAutoRecovery()
    {
        if (IsDead || CurrentPosture <= 0 || rapidDrainCoroutine != null) return;

        if (currentRecoveryTimer > 0)
        {
            currentRecoveryTimer -= Time.deltaTime;
        }
        else
        {
            float hpPercentage = CurrentHp / MaxHp.GetValue();
            float dynamicRecoveryRate = basePostureRecoveryRate;

            if(hpPercentage < 0.3f) { dynamicRecoveryRate *= 0.3f; }
            else if(hpPercentage < 0.6f) { dynamicRecoveryRate *= 0.6f; }

            CurrentPosture -= dynamicRecoveryRate * Time.deltaTime;
            CurrentPosture = Mathf.Max(CurrentPosture, 0);
            OnPostureChanged?.Invoke(CurrentPosture, MaxPosture.GetValue());
        }
    }


    //데미지 입었을 때
    public virtual void TakeDamage(ref DamageEvent damageInfo)
    {
        if (IsDead) return;

        CurrentHp -= damageInfo.currentDamage;
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
        if (IsDead) return;

        // 체간 회복 시작 딜레이 초기화
        CurrentPosture += amount;
        currentRecoveryTimer = postureRecoveryDelay;

        if (CurrentPosture >= MaxPosture.GetValue())
        {
            CurrentPosture = MaxPosture.GetValue();
            OnPostureBroken?.Invoke();
        }
        else
        {
            OnPostureChanged?.Invoke(CurrentPosture, MaxPosture.GetValue());
        }
    }


    //죽었을 때
    public virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke();
    }


    // 피회복
    public virtual void RestoreHealth(float heal)
    {
        if (CurrentHp + heal >= MaxHp.GetValue()) CurrentHp = MaxHp.GetValue();
        else CurrentHp += heal;

        OnHpChanged?.Invoke(CurrentHp, MaxHp.GetValue());
    }

    public virtual void ResetPosture(bool instant = false)
    {
        if (rapidDrainCoroutine != null) StopCoroutine(rapidDrainCoroutine);

        rapidDrainCoroutine = StartCoroutine(RapidDrainPostureRoutine());
    }

    private IEnumerator RapidDrainPostureRoutine()
    {
        float drainSpeed = MaxPosture.GetValue() * 0.5f;

        while (CurrentPosture > 0)
        {
            CurrentPosture -= drainSpeed * Time.deltaTime;
            CurrentPosture = Mathf.Max(CurrentPosture, 0);

            OnPostureChanged?.Invoke(CurrentPosture, MaxPosture.GetValue());
            yield return null;
        }

        rapidDrainCoroutine = null;
    }
}
