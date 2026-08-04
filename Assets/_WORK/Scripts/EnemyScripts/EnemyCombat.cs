using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

/// <summary>
/// 적의 무기 판정과 방어 판정을 담당
/// </summary>
public class EnemyCombat : MonoBehaviour, IWeaponOwner, IDefenser
{
    private Enemy owner;

    public Faction OwnerFaction => Faction.EnemyTeam;
    public AttackData CurrentAttack { get; private set; }
    public DefenseType CurrentDefense { get; private set; } = DefenseType.None;

    /// <summary>
    /// TargetingUI는 이 값이 true인 동안 적 위에 경고를 표시
    /// </summary>
    public bool IsSpecialAttack { get; private set; }

    [Header("방어 설정")]
    [Tooltip("공격을 막을 수 있는 전체 각도")]
    [SerializeField, Range(0f, 180f)] private float guardAngle = 120f;

    [SerializeField] private List<Weapon> weapons = new();
    // 같은 대상을 중복 타격하지 않도록 기록
    private readonly HashSet<IDamageable> hitTargets = new();


    private void Awake()
    {
        owner = GetComponent<Enemy>();
        weapons = GetComponentsInChildren<Weapon>().ToList();
    }

    /// <summary>새 공격을 시작할 때 공격 원본 데이터를 등록</summary>
    public void SetAttackData(AttackData attackData)
    {
        CurrentAttack = attackData;
        IsSpecialAttack = attackData != null && attackData.Type == AttackType.Special;
    }

    /// <summary>
    /// 무기가 상대에게 닿았을 때 데미지 파이프라인 시작점
    /// </summary>
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon, Vector3 hitPoint)
    {
        if (CurrentAttack == null || target == null || targetCollider == null || weapon == null) return;
        if (!hitTargets.Add(target)) return;

        Vector3 hitDirection = targetCollider.transform.position - transform.position;

        DamageRequest request = DamageRequest.AttackDamage( gameObject, weapon, CurrentAttack, owner.Stats.AttackPower.GetValue(), hitPoint, hitDirection);

        DamageResult result = target.ReceiveDamage(request);

        if (result.IsAccepted)
        {
            IsSpecialAttack = false;

            if (HitStopManager.Instance != null)
                HitStopManager.Instance.TriggerHitStop(result);

            owner.StateMachine?.CurrentState?.HandleAttackAccepted(result);
        }
    }

    /// <summary>
    /// 피해를 적용하기 전에 방어 가능한지 확인하는 함수
    /// </summary>
    public DefenseType DecideDefense(in DamageRequest request)
    {
        if (!request.CanGuard) return DefenseType.None;
        if (CurrentDefense == DefenseType.None) return DefenseType.None;
        if (!IsAttackFromFront(request.HitDirection)) return DefenseType.None;

        return CurrentDefense;
    }

    /// <summary>피해 결과에 맞는 피격 애니메이션 선택</summary>
    public int DecideHitReaction(in DamageResult result)
    {
        CancelAttack();

        if (result.DefenseType == DefenseType.Parry) return AnimHash.Parry;
        if (result.DefenseType == DefenseType.NormalGuard) return AnimHash.GuardHit;

        float hitAngle = Vector3.SignedAngle(transform.forward, result.HitDirection, Vector3.up);

        if (Mathf.Abs(hitAngle) <= 45f)
            return Random.Range(0, 2) == 0 ? AnimHash.BackHit1 : AnimHash.BackHit2;
        if (hitAngle > 45f && hitAngle <= 135f)
            return AnimHash.HitLeft;
        if (hitAngle >= -135f && hitAngle < -45f)
            return AnimHash.HitRight;

        return AnimHash.HitFront;
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
            weapon.EnableWeaponCollider();
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFXAtPoint(SfxKeys.Attack, transform.position);
    }



    /// <summary>
    /// 공격판정 종료
    /// </summary>
    public void CloseAttackHitWindow()
    {
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
        IsSpecialAttack = false;
    }


    /// <summary>
    /// 앞에서 맞은건가 뒤로 맞은건가 판정
    /// </summary>
    private bool IsAttackFromFront(Vector3 hitDirection)
    {
        float dot = Vector3.Dot(transform.forward, hitDirection);
        float threshold = Mathf.Cos(guardAngle * 0.5f * Mathf.Deg2Rad);
        return dot <= -threshold;
    }
}
