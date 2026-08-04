using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class LivingEntity : MonoBehaviour, IDamageable
{
    public abstract Faction TargetFaction { get; }

    //====스탯====
    public Stat MaxHp { get; protected set; }
    public Stat MaxPosture { get; protected set; }
    public Stat AttackPower { get; protected set; }
    public float CurrentPosture { get; protected set; }
    public float CurrentHp { get; protected set; }

    //====상태 변수====
    public bool IsDead { get; private set; }
    public bool IsInvincible { get; set; }
    public bool IsHealthDepleted => CurrentHp <= 0f;
    public bool IsPostureBroken { get; private set; }


    //==== 이벤트 ====
    public event Action<float, float> OnHpChanged;
    public event Action<float, float> OnPostureChanged;
    public event Action OnHealthDepleted;
    public event Action OnPostureBroken;
    public event Action<DamageResult>  OnDamage;
    public event Action OnDeath;


    [Header("체간 회복")]
    [SerializeField, Min(0f)] protected float postureRecoveryDelay = 2f;
    [SerializeField, Min(0f)] protected float postureRecoveryRateRatio = 0.25f;
    [Tooltip("가드일 때 회복 배속")]
    [SerializeField, Min(1f)] protected float guardPostureRecoveryMultiplier = 2f;
    private float currentRecoveryTimer;
    private int lastPostureDamageFrame = -1;


    protected virtual IDefenser Defenser => null;
    protected virtual PostureRecoveryMode CurrentPostureRecoveryMode => PostureRecoveryMode.Disabled;

    protected virtual void Awake()
    {
        MaxHp = new Stat(100f);
        MaxPosture = new Stat(100f);
        AttackPower = new Stat(1f);
    }

    protected virtual void LateUpdate()
    {
        HandlePostureAutoRecovery();
    }

    /// <summary>
    /// 체간 자동 회복
    /// </summary>
    private void HandlePostureAutoRecovery()
    {
        if (IsDead || CurrentPosture <= 0f) return;

        if (currentRecoveryTimer > 0f)
        {
            currentRecoveryTimer = Mathf.Max(0f, currentRecoveryTimer - Time.deltaTime);
            return;
        }

        if (lastPostureDamageFrame == Time.frameCount) return;

        PostureRecoveryMode recoveryMode = CurrentPostureRecoveryMode;
        if (recoveryMode == PostureRecoveryMode.Disabled) return;

        float maxHp = MaxHp.GetValue();
        float hpPercentage = maxHp > 0f ? CurrentHp / maxHp : 0f;
        float maxPosture = MaxPosture.GetValue();
        float dynamicRecoveryRate =
            maxPosture * postureRecoveryRateRatio * GetPostureRecoveryHealthMultiplier(hpPercentage);

        if (recoveryMode == PostureRecoveryMode.GuardBoosted)
            dynamicRecoveryRate *= guardPostureRecoveryMultiplier;

        if (dynamicRecoveryRate <= 0f) return;

        float previousPosture = CurrentPosture;
        CurrentPosture = Mathf.Max(0f, CurrentPosture - dynamicRecoveryRate * Time.deltaTime);

        if (!Mathf.Approximately(previousPosture, CurrentPosture))
            OnPostureChanged?.Invoke(CurrentPosture, maxPosture);
    }

    /// <summary>
    /// hp 비율당 체간 회복률 가져오기
    /// </summary>
    protected virtual float GetPostureRecoveryHealthMultiplier(float hpPercentage)
    {
        if (hpPercentage > 0.75f) return 1f;
        if (hpPercentage > 0.50f) return 0.75f;
        if (hpPercentage > 0.25f) return 0.50f;
        return 0.25f;
    }

    /// <summary>
    /// 모든 공격을 계산하고 피해를 입히는 함수
    /// </summary>
    public DamageResult ReceiveDamage(in DamageRequest request)
    {
        if (!CanReceiveDamage())
            return DamageResult.Ignored(request);

        DefenseType defense = Defenser != null? Defenser.DecideDefense(request): DefenseType.None;

        DamagePayload payload = DamageCalculator.Calculate(request, defense);

        float previousHp = CurrentHp;
        CurrentHp = Mathf.Max(0f, CurrentHp - payload.HealthDamage);

        if (CurrentHp < previousHp)
            OnHpChanged?.Invoke(CurrentHp, MaxHp.GetValue());

        // 완벽 패링도 체간 피해는 받지만 체간이 붕괴되진 않음.
        bool canBreakPosture = defense != DefenseType.Parry;
        bool postureBrokenNow = ApplyPostureDamage(payload.PostureDamage, canBreakPosture);
        bool healthDepletedNow = previousHp > 0f && CurrentHp <= 0f;

        if (healthDepletedNow)
        {
            OnHealthDepleted?.Invoke();
            HandleHealthDepleted();
        }
        else if (postureBrokenNow)
        {
            OnPostureBroken?.Invoke();
        }

        DamageResult result = DamageResult.Accepted(request, defense);

        OnDamage?.Invoke(result);

        if (defense == DefenseType.Parry)
            ApplyDeflectPosture(request);

        return result;
    }

    /// <summary>
    /// 체간 피해만 적용
    /// </summary>
    public void ReceivePostureDamage(float postureDamage)
    {
        if (!CanReceiveDamage() || postureDamage <= 0f)
            return;

        bool postureBroken = ApplyPostureDamage(postureDamage, canBreak: true);

        if (postureBroken)
            OnPostureBroken?.Invoke();
    }

    /// <summary>데미지를 받을 수 있는 상태인지</summary>
    protected virtual bool CanReceiveDamage()
    {
        return !IsDead && !IsInvincible && !IsHealthDepleted;
    }

    /// <summary>
    /// 피가 0이 될 때 호출되는 함수
    /// <para>기본적으로 목숨이 없으면 Die함수 호출</para>
    /// </summary>
    protected virtual void HandleHealthDepleted()
    {
        Die();
    }
    
    /// <summary>
    /// 체간 데미지 적용
    /// </summary>
    /// <returns>이번 체간 피해로 실제 체간 붕괴 조건을 처음 만족했다면 true.</returns>
    private bool ApplyPostureDamage(float postureDamage, bool canBreak)
    {
        postureDamage = Mathf.Max(0f, postureDamage);
        if (postureDamage <= 0f)
            return false;

        float maxPosture = Mathf.Max(0f, MaxPosture.GetValue());
        if (maxPosture <= 0f)
            return false;

        float previousPosture = CurrentPosture;
        CurrentPosture = Mathf.Min(previousPosture + postureDamage, maxPosture);

        currentRecoveryTimer = postureRecoveryDelay;
        lastPostureDamageFrame = Time.frameCount;
        OnPostureChanged?.Invoke(CurrentPosture, maxPosture);

        bool brokeNow = canBreak && !IsPostureBroken && previousPosture + postureDamage >= maxPosture;

        if (brokeNow)
            IsPostureBroken = true;

        return brokeNow;
    }

    /// <summary>
    /// 패링 성공 시 체간 피해를 반사
    /// </summary>
    private void ApplyDeflectPosture(in DamageRequest request)
    {
        if (request.Attacker == null)
            return;

        float postureDamage = request.RequestedPayload.PostureDamage * DamageCalculator.DeflectPostureRatio;
        if (postureDamage <= 0f)
            return;

        if (!request.Attacker.TryGetComponent(out IDamageable attacker) || attacker.IsDead)
            return;

        attacker.ReceivePostureDamage(postureDamage);
    }


    /// <summary>
    /// 죽음 처리
    /// </summary>
    public virtual void Die()
    {
        if (IsDead) return;

        IsDead = true;
        OnDeath?.Invoke();
    }


    /// <summary>
    /// 체력 회복
    /// </summary>
    public virtual void RestoreHealth(float heal)
    {
        CurrentHp = Mathf.Min(CurrentHp + Mathf.Max(0f, heal), MaxHp.GetValue());
        OnHpChanged?.Invoke(CurrentHp, MaxHp.GetValue());
    }


    /// <summary>
    /// 체간 초기화
    /// </summary>
    public virtual void ResetPosture()
    {
        CurrentPosture = 0f;
        IsPostureBroken = false;
        currentRecoveryTimer = 0f;
        OnPostureChanged?.Invoke(CurrentPosture, MaxPosture.GetValue());
    }
}
