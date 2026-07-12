using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;



public class PlayerCombat : MonoBehaviour, IWeaponOwner
{
    [SerializeField] Player player;

    public Faction OwnerFaction => Faction.PlayerTeam;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    public Weapon CurrentWeapon { get; private set; }

    public AttackData FirstAttackData;
    public AttackData CurrentAttack{ get; private set; } //현재 공격 데이터
    public DefenseType CurrentDefenseType { get; set; } = DefenseType.None; //현재 가드 상태

    [Header("방어 설정")]
    [SerializeField, Range(0f, 180f)]
    private float guardAngle = 120f;

    private void Awake()
    {
        CurrentWeapon = GetComponentInChildren<Weapon>();
    }

    //=============== 공격 함수 ========================

    //공격 시작
    public void SetCurrentAttackData(AttackData attackData)
    {
        CurrentAttack = attackData;
    }

    //=============== 타격 및 피격 함수 ========================


    //플레이어가 적을 공격했을 때
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon)
    {
        if (CurrentAttack == null) return;
        if (!hitTargets.Add(target)) { return; }
        Vector3 hitPoint = targetCollider.ClosestPoint(weapon.transform.position);
        Vector3 hitDirection = targetCollider.transform.position - transform.position;
        DamageEvent result = new(gameObject, CurrentAttack, hitPoint, hitDirection);
        result.currentDamage += player.Stats.AttackPower.Value;
        target.TakeDamage(ref result);
    }

    //데미지 받았을때 반응
    public int EvaluateHitReaction(ref DamageEvent result)
    {
        ForceResetAttackState(); // 상태 초기화

        if (result.attackData == null) return AnimHash.HitFront;
        if (result.attackData.CanGuard && result.defenseResult == DefenseType.PerfectParry) { return AnimHash.Parry; }
        else if (result.attackData.CanGuard && (result.defenseResult == DefenseType.NormalGuard 
            || result.defenseResult == DefenseType.FailedGuard)){ return AnimHash.GuardHit; }
        else if (result.currentDamage > 0 || result.defenseResult == DefenseType.None )
        {
            float hitAngle = Vector3.SignedAngle(transform.forward, result.hitDirection, Vector3.up);
            if (Mathf.Abs(hitAngle) <= 45f)
            {
                return UnityEngine.Random.Range(0, 2) == 0 ? AnimHash.BackHit1 : AnimHash.BackHit2;
            }
            else if (hitAngle > 45f && hitAngle <= 135f)
            {
                return AnimHash.HitLeft;
            }
            else if (hitAngle >= -135f && hitAngle < -45f)
            {
                return AnimHash.HitRight;
            }
            else
            {
                return AnimHash.HitFront; 
            }
        }
        return 0;
    }

    //공격 방향 판정
    private bool IsAttackFromFront(Vector3 hitDirection)
    {
        float dot = Vector3.Dot(transform.forward, hitDirection);
        float halfAngle = guardAngle / 2f;
        float threshold = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        return dot <= -threshold;
    }

    public void EvaluateDefense(ref DamageEvent info)
    {
        if (!IsAttackFromFront(info.hitDirection)) return;
        if (info.attackData.CanGuard && CurrentDefenseType != DefenseType.None)
        {
            info.defenseResult = CurrentDefenseType;
        }
    }


    public void ResetCombo() => CurrentAttack = null;


    public void ForceResetAttackState()
    {
        if (CurrentWeapon != null)
            CurrentWeapon.DisableWeaponCollider();

        ResetCombo();
    }


    //====================무기 on/off=====================
    public void OnAnimationPlayerAttackStart()
    {
        hitTargets.Clear();
        CurrentWeapon.EnableWeaponCollider();
    }
    public void OnAnimationPlayerAttackEnd()
    {
        CurrentWeapon.DisableWeaponCollider();
    }


}
