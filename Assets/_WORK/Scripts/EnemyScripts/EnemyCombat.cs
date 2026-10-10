using System.Collections.Generic;
using UnityEngine;
using SoundEffectManager;

/// <summary>
/// 적의 무기 판정과 방어 판정을 담당
/// </summary>
public class EnemyCombat : MonoBehaviour, IWeaponOwner, IDefenser
{
    private Enemy owner;

    public Faction OwnerFaction => Faction.EnemyTeam;
    public bool IsDefensibleAttack => CurrentAttack != null && (CurrentAttack.Type == AttackType.Normal || CurrentAttack.Type == AttackType.Thrust);
    public EnemyAttackData CurrentAttack { get; private set; }
    public DefenseType CurrentDefense { get; private set; } = DefenseType.None;

    /// <summary>
    /// TargetingUI는 이 값이 true인 동안 적 위에 경고를 표시
    /// </summary>
    public bool IsSpecialAttack { get; private set; }

    private EnemyData data;

    private Weapon[] weapons;
    // 같은 대상을 중복 타격하지 않도록 기록
    private readonly HashSet<IDamageable> hitTargets = new();

    private void Awake()
    {
        owner = GetComponent<Enemy>();
        data = GetComponent<EnemyStats>().Data;
        weapons = GetComponentsInChildren<Weapon>();
    }

    /// <summary>새 공격을 시작할 때 공격 원본 데이터를 등록</summary>
    public void SetAttackData(EnemyAttackData attackData)
    {
        owner.Motor.StopAttackPressure();
        CurrentAttack = attackData;
        IsSpecialAttack = attackData != null && attackData.Type != AttackType.Normal;
    }

    /// <summary>
    /// 무기가 상대에게 닿았을 때 데미지 파이프라인 시작점
    /// </summary>
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon, Vector3 hitPoint, bool defenseOnly = false)
    {
        if (CurrentAttack == null || target == null || targetCollider == null || weapon == null) return;

        Vector3 hitDirection = targetCollider.transform.position - transform.position;

        DamageRequest request = DamageRequest.AttackDamage(gameObject, weapon, CurrentAttack, owner.Stats.AttackPower.GetValue(), hitPoint, hitDirection);

        if (defenseOnly && ((!request.CanGuard && !request.CanDeflect) || !targetCollider.TryGetComponent<IDefenser>(out var defenser)
            || defenser.DecideDefense(request) == DefenseType.None)) return;
        if (!hitTargets.Add(target)) return;

        DamageResult result = target.ReceiveDamage(request);
        if (defenseOnly && !result.IsAccepted) hitTargets.Remove(target);

        if (result.IsAccepted)
        {
            IsSpecialAttack = false;

            Player playerTarget = targetCollider.GetComponentInParent<Player>();
            owner.Motor.RecordAttackContact(playerTarget, result);
            if (KnockBackPolicy.CanApplyAttackPressure(result))
                owner.Motor.StartAttackPressure(playerTarget, result);
            else
                owner.Motor.StopAttackPressure();

            if (HitStopManager.Instance != null)
                HitStopManager.Instance.HitStop(result);

            owner.StateMachine?.CurrentState?.HandleAttackAccepted(result);

            //넉백
            if (result.DefenseType == DefenseType.Parry && result.Request.DeflectResponse == DeflectResponse.Rebound && !owner.Stats.IsDead
                && !owner.Stats.IsHealthDepleted && !owner.Stats.IsPostureBroken && !owner.IsBeingExecuted)
                owner.Motor.StartKnockback(-result.HitDirection, KnockBackPolicy.AttackerKnockBack(result));
        }
    }

    /// <summary>
    /// 피해를 적용하기 전에 방어 가능한지 확인하는 함수
    /// </summary>
    public DefenseType DecideDefense(in DamageRequest request)
    {
        if (CurrentDefense == DefenseType.None) return DefenseType.None;
        if (!IsAttackFromFront(request.HitDirection)) return DefenseType.None;

        return CurrentDefense switch
        {
            DefenseType.NormalGuard when request.CanGuard => DefenseType.NormalGuard,
            DefenseType.Parry when request.CanDeflect => DefenseType.Parry,
            _ => DefenseType.None
        };
    }

    public bool CanReceiveAttackPressure(Vector3 hitDirection, DefenseType defense)
    {
        if (!owner.isActiveAndEnabled || owner.Stats.IsDead || owner.Stats.IsHealthDepleted
            || owner.Stats.IsPostureBroken || owner.Stats.IsInvincible || owner.IsBeingExecuted)
            return false;

        if (defense == DefenseType.None)
            return owner.StateMachine?.CurrentState is EnemyHitState;

        return owner.StateMachine?.CurrentState is EnemyGuardState && IsAttackFromFront(hitDirection);
    }

    /// <summary>
    /// 현재 방어 타입 세팅
    /// </summary>
    public void SetDefense(DefenseType defense)
    {
        CurrentDefense = defense;
    }

    /// <summary>
    /// 현재 방어 타입 초기화
    /// </summary>
    public void ClearDefense()
    {
        CurrentDefense = DefenseType.None;
    }

    /// <summary>
    /// 공격판정 시작
    /// </summary>
    public void OpenAttackHitWindow()
    {
        hitTargets.Clear();
        foreach (Weapon weapon in weapons)
        {
            if (weapon.HitGroup == CurrentAttack.HitGroup)
                weapon.EnableWeaponCollider();
            else
                weapon.DisableWeaponCollider();
        }
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(SfxKeys.Attack, transform.position);
    }

    /// <summary>
    /// 공격판정 종료
    /// </summary>
    public void CloseAttackHitWindow(bool preservePressure = false)
    {
        if (!preservePressure) owner.Motor.StopAttackPressure();
        foreach (Weapon weapon in weapons)
            weapon.DisableWeaponCollider();

        IsSpecialAttack = false;
    }

    /// <summary>
    /// 공격 취소
    /// </summary>
    public void CancelAttack()
    {
        CloseAttackHitWindow();
        hitTargets.Clear();
        CurrentAttack = null;
    }

    /// <summary>
    /// 앞에서 맞은건가 뒤로 맞은건가 판정
    /// </summary>
    private bool IsAttackFromFront(Vector3 hitDirection)
    {
        float dot = Vector3.Dot(transform.forward, hitDirection);
        float threshold = Mathf.Cos(data.guardAngle * 0.5f * Mathf.Deg2Rad);
        return dot <= -threshold;
    }
}
