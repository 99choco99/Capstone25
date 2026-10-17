using System;
using SoundEffectManager;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerCombat : MonoBehaviour, IWeaponOwner, IDefenser
{
    private Player player;

    public Faction OwnerFaction => Faction.PlayerTeam;
    public bool IsDefensibleAttack => CurrentAttack != null;
    public Weapon CurrentWeapon { get; private set; }

    [Header("공격 데이터")]
    public PlayerAttackData FirstAttackData;
    public PlayerAttackData SprintAttackData;
    public PlayerAttackData CurrentAttack { get; private set; }

    public DefenseType CurrentDefenseType { get; set; } = DefenseType.None;

    [Header("방어 설정")]
    [SerializeField, Range(0f, 180f)]
    private float guardAngle = 120f;

    private readonly AttackHitResolver hitTargets = new();
    private bool isAttackCommitted;
    private float nextAttackTime;
    public bool CanStartAttack => Time.time >= nextAttackTime;

    //이벤트
    public event Action<float> AttackStarted;
    public event Action AttackEnded;

    //============ 초기화와 비활성화 ============

    private void Awake()
    {
        player = GetComponent<Player>();
        CurrentWeapon = GetComponentInChildren<Weapon>();
    }

    private void OnDisable()
    {
        nextAttackTime = 0f;
    }

    //============ 공격 준비와 시작 ============

    /// <summary>
    /// 현재 공격할 데이터 세팅
    /// </summary>
    public void SetCurrentAttackData(PlayerAttackData attackData)
    {
        EndCurrentAttack();
        player.Motor.StopAttackPressure();
        if (attackData == null)
            return;

        CurrentAttack = attackData;
    }

    /// <summary>
    /// 공격이 확정됐을 때 이벤트 발송
    /// </summary>
    public void CommitCurrentAttack(float expectedActiveAt)
    {
        if (CurrentAttack == null || isAttackCommitted)
            return;

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

    //============ 공격 접촉과 반응 ============

    /// <summary>
    /// 무기에 닿았을 때 호출, 데미지 파이프라인의 첫번째 지점
    /// </summary>
    public void OnWeaponHit(
        IDamageable target,
        Collider targetCollider,
        Weapon weapon,
        Vector3 hitPoint,
        bool defenseOnly = false)
    {
        if (CurrentAttack == null)
            return;

        Vector3 hitDirection = targetCollider.transform.position - transform.position;

        DamageRequest request = DamageRequest.AttackDamage(
            gameObject,
            weapon,
            CurrentAttack,
            hitPoint,
            hitDirection,
            player.Stats.AttackPower.GetValue());

        if (hitTargets.TryResolve(target, targetCollider, request, defenseOnly, out DamageResult result))
        {
            HandleAcceptedAttack(targetCollider, result);
        }
    }

    private void HandleAcceptedAttack(Collider targetCollider, in DamageResult result)
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
        if (result.IsParryRebound && !player.Stats.IsDead
            && !player.Stats.IsHealthDepleted && !player.Stats.IsPostureBroken && !player.Execution.IsExecuting)
            player.Motor.StartKnockback(-result.HitDirection, KnockBackPolicy.AttackerKnockBack(result));
    }

    public bool HasProcessedTarget(IDamageable target) => hitTargets.HasProcessedTarget(target);

    //============ 공격 종료와 재공격 대기 ============

    /// <summary>
    /// 플레이어 공격 판정 끝
    /// </summary>
    public void ForceResetAttackState(bool preservePressure = false)
    {
        if (!preservePressure)
            player.Motor.StopAttackPressure();
        if (CurrentWeapon != null)
            CurrentWeapon.DisableWeaponCollider();

        EndCurrentAttack();
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

    public void PreserveAttackRecovery(PlayerAttackData attackData, float currentFrame)
    {
        float remainingFrames = Mathf.Max(0f, attackData.comboStartFrame - currentFrame);
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + remainingFrames / attackData.FramesPerSecond);
    }

    //============ 방어와 압박 수신 ============

    /// <summary>
    /// 방어 유형 최종 결정
    /// </summary>
    public DefenseType DecideDefense(in DamageRequest request)
    {
        if (CurrentDefenseType == DefenseType.None)
            return DefenseType.None;
        if (!IsAttackFromFront(request.HitDirection))
            return DefenseType.None;

        return DefenseResolver.Resolve(CurrentDefenseType, request);
    }

    public bool CanReceiveAttackPressure(Vector3 hitDirection, DefenseType defense)
    {
        if (!player.isActiveAndEnabled
            || !player.Motor.Controller.enabled
            || player.Stats.IsDead
            || player.Stats.IsHealthDepleted
            || player.Stats.IsPostureBroken
            || player.Stats.IsInvincible
            || player.Execution.IsExecuting)
            return false;

        if (defense == DefenseType.None)
            return player.StateMachine?.CurrentState is PlayerHitState
                || player.StateMachine?.CurrentState is PlayerGuardState guardState && guardState.IsRecoveringFromHit;

        return player.StateMachine?.CurrentState is PlayerGuardState && IsAttackFromFront(hitDirection);
    }

    /// <summary>
    /// 앞에서 때리는건지 아닌지 검사
    /// </summary>
    private bool IsAttackFromFront(Vector3 hitDirection)
    {
        return DefenseResolver.IsAttackFromFront(transform.forward, hitDirection, guardAngle);
    }
}
