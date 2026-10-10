using System;
using System.Collections.Generic;
using SoundEffectManager;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerCombat : MonoBehaviour, IWeaponOwner, IDefenser
{
    private Player player;

    public Faction OwnerFaction => Faction.PlayerTeam;
    public bool IsDefensibleAttack => CurrentAttack != null && (CurrentAttack.Type == AttackType.Normal || CurrentAttack.Type == AttackType.Thrust);
    public Weapon CurrentWeapon { get; private set; }

    [Header("공격 데이터")]
    public PlayerAttackData FirstAttackData;
    public PlayerAttackData SprintAttackData;
    public PlayerAttackData CurrentAttack { get; private set; }

    public DefenseType CurrentDefenseType { get; set; } = DefenseType.None;

    [Header("방어 설정")]
    [SerializeField, Range(0f, 180f)]
    private float guardAngle = 120f;

    private readonly HashSet<IDamageable> hitTargets = new();
    private bool isAttackCommitted;
    private float nextAttackTime;
    public bool CanStartAttack => Time.time >= nextAttackTime;

    //이벤트
    public event Action<float> AttackStarted;
    public event Action AttackEnded;

    private void Awake()
    {
        player = GetComponent<Player>();
        CurrentWeapon = GetComponentInChildren<Weapon>();
    }

    /// <summary>
    /// 무기에 닿았을 때 호출, 데미지 파이프라인의 첫번째 지점
    /// </summary>
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon, Vector3 hitPoint, bool defenseOnly = false)
    {
        if (CurrentAttack == null) return;

        Vector3 hitDirection = targetCollider.transform.position - transform.position;

        DamageRequest request = DamageRequest.AttackDamage(gameObject, weapon, CurrentAttack, player.Stats.AttackPower.GetValue(), hitPoint, hitDirection);

        if (defenseOnly && ((!request.CanGuard && !request.CanDeflect) || !targetCollider.TryGetComponent<IDefenser>(out var defenser)
            || defenser.DecideDefense(request) == DefenseType.None)) return;
        if (!hitTargets.Add(target)) return;

        DamageResult result = target.ReceiveDamage(request);
        if (defenseOnly && !result.IsAccepted) hitTargets.Remove(target);

        if (result.IsAccepted)
        {
            Enemy enemyTarget = targetCollider.GetComponentInParent<Enemy>();
            player.Motor.RecordAttackContact(enemyTarget, result);
            if (KnockBackPolicy.CanApplyAttackPressure(result))
                player.Motor.StartAttackPressure(enemyTarget, result);
            else
                player.Motor.StopAttackPressure();

            if (HitStopManager.Instance != null)
                HitStopManager.Instance.HitStop(result);

            player.StateMachine?.CurrentState?.HandleAttackAccepted(result);

            //공격자 넉백
            if (result.DefenseType == DefenseType.Parry && result.Request.DeflectResponse == DeflectResponse.Rebound && !player.Stats.IsDead
                && !player.Stats.IsHealthDepleted && !player.Stats.IsPostureBroken && !player.Execution.IsExecuting)
                player.Motor.StartKnockback(-result.HitDirection, KnockBackPolicy.AttackerKnockBack(result));
        }
    }

    /// <summary>
    /// 방어 유형 최종 결정
    /// </summary>
    public DefenseType DecideDefense(in DamageRequest request)
    {
        if (CurrentDefenseType == DefenseType.None) return DefenseType.None;
        if (!IsAttackFromFront(request.HitDirection)) return DefenseType.None;

        return CurrentDefenseType switch
        {
            DefenseType.NormalGuard when request.CanGuard => DefenseType.NormalGuard,
            DefenseType.Parry when request.CanDeflect => DefenseType.Parry,
            _ => DefenseType.None
        };
    }

    public bool CanReceiveAttackPressure(Vector3 hitDirection, DefenseType defense)
    {
        if (!player.isActiveAndEnabled || !player.Motor.Controller.enabled || player.Stats.IsDead || player.Stats.IsHealthDepleted || player.Stats.IsPostureBroken
            || player.Stats.IsInvincible || player.Execution.IsExecuting)
            return false;

        if (defense == DefenseType.None)
            return player.StateMachine?.CurrentState is PlayerHitState;

        return player.StateMachine?.CurrentState is PlayerGuardState && IsAttackFromFront(hitDirection);
    }

    /// <summary>
    /// 앞에서 때리는건지 아닌지 검사
    /// </summary>
    private bool IsAttackFromFront(Vector3 hitDirection)
    {
        float dot = Vector3.Dot(transform.forward, hitDirection);
        float halfAngle = guardAngle / 2f;
        float threshold = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        return dot <= -threshold;
    }

    /// <summary>
    /// 플레이어 공격 판정 끝
    /// </summary>
    public void ForceResetAttackState(bool preservePressure = false)
    {
        if (!preservePressure) player.Motor.StopAttackPressure();
        if (CurrentWeapon != null)
            CurrentWeapon.DisableWeaponCollider();

        EndCurrentAttack();
    }

    //============ 공격 함수 순서대로 ===========

    /// <summary>
    /// 현재 공격할 데이터 세팅
    /// </summary>
    public void SetCurrentAttackData(PlayerAttackData attackData)
    {
        EndCurrentAttack();
        player.Motor.StopAttackPressure();
        if (attackData == null) return;

        CurrentAttack = attackData;
    }

    public void PreserveAttackRecovery(PlayerAttackData attackData, float currentFrame)
    {
        float remainingFrames = Mathf.Max(0f, attackData.comboStartFrame - currentFrame);
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + remainingFrames / attackData.FramesPerSecond);
    }

    private void OnDisable()
    {
        nextAttackTime = 0f;
    }

    /// <summary>
    /// 공격이 확정됐을 때 이벤트 발송
    /// </summary>
    public void CommitCurrentAttack(float expectedActiveAt)
    {
        if (CurrentAttack == null || isAttackCommitted) return;

        isAttackCommitted = true;
        AttackStarted?.Invoke(expectedActiveAt);
    }

    /// <summary>
    /// 플레이어의 공격 판정 시작
    /// </summary>
    public void PlayerAttackStart()
    {
        hitTargets.Clear();
        CurrentWeapon.EnableWeaponCollider();
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(SfxKeys.Attack, CurrentWeapon.transform.position);
    }

    /// <summary>
    /// 공격 끝, 마무리 작업
    /// </summary>
    private void EndCurrentAttack()
    {
        if (CurrentAttack == null)
        {
            isAttackCommitted = false;
            return;
        }

        CurrentAttack = null;

        if (isAttackCommitted)
        {
            isAttackCommitted = false;
            AttackEnded?.Invoke();
        }
    }
}
