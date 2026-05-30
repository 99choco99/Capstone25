using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;



public class PlayerCombat : MonoBehaviour, IWeaponOwner
{
    public Faction OwnerFaction => Faction.PlayerTeam;

    public bool IsParryWindowOpen { get; private set; }
    public bool IsGuarding { get; set; }

    public Weapon CurrentWeapon { get; private set; }

    public AttackData FirstAttackData;
    public AttackData CurrentAttack{ get; private set; } //현재 공격 데이터

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
        Vector3 hitPoint = targetCollider.ClosestPoint(weapon.transform.position);
        Vector3 hitDirection = targetCollider.transform.position - transform.position; 
        DamageEvent result = new()
        {
            attacker = this.gameObject,
            attackData = CurrentAttack,
            hitPoint = hitPoint,
            hitDirection = hitDirection,
        };

        target.TakeDamage(ref result);
    }

    //데미지 받았을때 반응
    public int EvaluateHitReaction(ref DamageEvent result)
    {
        ForceResetAttackState(); // 상태 초기화

        if (result.attackData.CanGuard && result.wasParried) { return AnimHash.Parry; }
        else if (result.attackData.CanGuard && result.wasGuarded) { return AnimHash.GuardHit; }
        else if (result.currentDamage > 0)
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
        if (info.attackData.CanGuard && IsGuarding)
        {
            //패링 성공시
            if (IsParryWindowOpen)
            {
                info.wasParried = true;
            }
            else //일반 가드 시
            {
                info.wasGuarded = true;
            }
        }
    }

    public void SetParryWindow(bool isOpen) => IsParryWindowOpen = isOpen;
    public void ResetCombo() => CurrentAttack = null;


    public void ForceResetAttackState()
    {
        if (CurrentWeapon != null)
            CurrentWeapon.DisableWeaponCollider();

        ResetCombo();
        SetParryWindow(false);

    }


    //====================무기 on/off=====================
    public void OnAnimationPlayerAttackStart()
    {
        CurrentWeapon.EnableWeaponCollider();
    }
    public void OnAnimationPlayerAttackEnd()
    {
        CurrentWeapon.DisableWeaponCollider();
    }


}
