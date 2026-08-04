using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Player))]
public class PlayerCombat : MonoBehaviour, IWeaponOwner, IDefenser
{
    [SerializeField] private Player player;

    public Faction OwnerFaction => Faction.PlayerTeam;
    public Weapon CurrentWeapon { get; private set; }

    [Header("공격 데이터")]
    public AttackData FirstAttackData;
    public AttackData SprintAttackData;
    public AttackData CurrentAttack { get; private set; }

    public DefenseType CurrentDefenseType { get; set; } = DefenseType.None;

    [Header("방어 설정")]
    [SerializeField, Range(0f, 180f)]
    private float guardAngle = 120f;

    private readonly HashSet<IDamageable> hitTargets = new();
    private bool isAttackCommitted;

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
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon, Vector3 hitPoint)
    {
        if (CurrentAttack == null) return;
        if (!hitTargets.Add(target)) return;

        Vector3 hitDirection = targetCollider.transform.position - transform.position;

        DamageRequest request = DamageRequest.AttackDamage(gameObject, weapon, CurrentAttack, player.Stats.AttackPower.GetValue(), hitPoint, hitDirection);

        DamageResult result = target.ReceiveDamage(request);

        if (result.IsAccepted)
        {
            if (HitStopManager.Instance != null)
                HitStopManager.Instance.TriggerHitStop(result);

            player.StateMachine?.CurrentState?.HandleAttackAccepted(result);
        }
    }


    /// <summary>
    /// 피격 시 애니메이션 결정
    /// </summary>
    public int DecideHitReaction(in DamageResult result)
    {
        ForceResetAttackState();

        if (result.DefenseType == DefenseType.Parry) return AnimHash.Parry;
        if (result.DefenseType == DefenseType.NormalGuard) return AnimHash.GuardHit;

        float hitAngle = Vector3.SignedAngle(transform.forward,result.HitDirection, Vector3.up);

        if (Mathf.Abs(hitAngle) <= 45f)
            return Random.Range(0, 2) == 0 ? AnimHash.BackHit1 : AnimHash.BackHit2;

        if (hitAngle > 45f && hitAngle <= 135f)
            return AnimHash.HitLeft;

        if (hitAngle >= -135f && hitAngle < -45f)
            return AnimHash.HitRight;

        return AnimHash.HitFront;
    }

    /// <summary>
    /// 방어 유형 최종 결정
    /// </summary>
    public DefenseType DecideDefense(in DamageRequest request)
    {
        if (!request.CanGuard) return DefenseType.None;
        if (CurrentDefenseType == DefenseType.None) return DefenseType.None;
        if (!IsAttackFromFront(request.HitDirection)) return DefenseType.None;

        return CurrentDefenseType;
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

    public void ForceResetAttackState()
    {
        if (CurrentWeapon != null)
            CurrentWeapon.DisableWeaponCollider();

        ResetCombo();
    }

    public void ResetCombo() => EndCurrentAttack();

    //============ 공격 함수 순서대로 ===========

    /// <summary>
    /// 현재 공격할 데이터 세팅
    /// </summary>
    public void SetCurrentAttackData(AttackData attackData)
    {
        EndCurrentAttack();
        if (attackData == null) return;

        CurrentAttack = attackData;
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
    }

    /// <summary>
    /// 플레이어 공격 판정 끝
    /// </summary>
    public void PlayerAttackEnd()
    {
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
}
