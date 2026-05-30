
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyCombat : MonoBehaviour,IWeaponOwner
{
    public Faction OwnerFaction => Faction.EnemyTeam;
    [SerializeField] private List<Weapon> weapons = new List<Weapon>();

    public AttackData CurrentAttack { get; private set; }
    public bool IsParryWindowOpen { get; private set; }
    public bool IsGuarding { get; set; }


    [Header("방어 설정")]
    [SerializeField, Range(0f, 180f)]
    private float guardAngle = 120f;

    private void Awake()
    {
        weapons = GetComponentsInChildren<Weapon>().ToList();
    }

    // ================= 공격 로직 =================
    public void SetCurrentAttackData(AttackData attackData)
    {
        CurrentAttack = attackData;
    }

    // 플레이어를 공격했을 때
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon)
    {
        if (CurrentAttack == null) return; 

        Vector3 hitPoint = targetCollider.ClosestPoint(weapon.transform.position);

        Vector3 hitDirection = (targetCollider.transform.position - transform.position);
        hitDirection.y = 0;
        hitDirection.Normalize();

        DamageEvent damageInfo = new()
        {
            attacker = gameObject,
            attackData = CurrentAttack,
            hitPoint = hitPoint,
            hitDirection = hitDirection,
        };

        target.TakeDamage(ref damageInfo);
    }


    // ================= 방어 및 피격 판정 =================
    private bool IsAttackFromFront(Vector3 hitDirection)
    {
        float dot = Vector3.Dot(transform.forward, hitDirection);
        float halfAngle = guardAngle / 2f;
        float threshold = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        return dot <= -threshold;
    }

    // 플레이어의 공격을 막거나 패링할 수 있는지 계산
    public void EvaluateDefense(ref DamageEvent info)
    {
        if (!IsAttackFromFront(info.hitDirection)) return;

        if (info.attackData.CanGuard && IsGuarding)
        {
            if (IsParryWindowOpen) info.wasParried = true;
            else info.wasGuarded = true;
        }
    }

    public int EvaluateHitReaction(ref DamageEvent result)
    {
        ForceResetAttackState();

        if (result.wasParried) return AnimHash.Parry;
        else if (result.wasGuarded) return AnimHash.GuardHit;

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

    // ================= 상태 변화 =================
    public void ForceResetAttackState()
    {
        foreach (var weapon in weapons) weapon.DisableWeaponCollider();
        ResetCombo();
        SetParryWindow(false);
    }

    public void SetParryWindow(bool isOpen) => IsParryWindowOpen = isOpen;
    public void ResetCombo() => CurrentAttack = null;


    //===========무기 on/off==========
    public void EnemyAttackStart()
    {
        foreach (var weapon in weapons)
        {
            weapon.EnableWeaponCollider();
        }
    }

    public void EnemyAttackEnd()
    {
        foreach (var weapon in weapons)
        {
            weapon.DisableWeaponCollider();
        }
    }


}
