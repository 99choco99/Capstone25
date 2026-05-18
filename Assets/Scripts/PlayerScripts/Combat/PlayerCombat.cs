using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;



public class PlayerCombat : MonoBehaviour, IWeaponOwner
{
    private Player player;
    public Faction OwnerFaction => Faction.PlayerTeam;

    public bool IsParryWindowOpen { get; private set; }
    public bool IsGuarding { get; set; }

    public Weapon CurrentWeapon { get; private set; }

    [Header("공격 데이터")]
    [SerializeField] private PlayerAttackData firstNormalAttack; // 일반 공격 콤보 데이터
    [SerializeField] private PlayerAttackData heavyAttack;
    [SerializeField] private PlayerAttackData sprintAttack;
    private PlayerAttackData currentAttack; //현재 공격 데이터



    [Header("연속 공격 타이머")]
    [SerializeField] private float comboResetTime = 1.5f;
    private float lastAttackTime = 0f;

    public event Action<Player> OnExecuteEnd;
    public event Action<int> OnHitStunTriggered;

    private void Awake()
    {
        player = GetComponent<Player>();
        CurrentWeapon = GetComponentInChildren<Weapon>();
    }
    private void Start()
    {
        if (!player.IsLocalPlayer) { return; }
        player.Stats.OnDamaged += EvaluateHitReaction;
        player.Stats.OnDeath += ForceResetAttackState;

    }
    private void OnDestroy()
    {

        if (player != null && player.Stats != null)
        {
            if (!player.IsLocalPlayer) { return; }
            player.Stats.OnDamaged -= EvaluateHitReaction;
            player.Stats.OnDeath -= ForceResetAttackState;
        }
    }

    //=============== 공격 함수 ========================

    //공격 시작
    public bool StartNormalAttack()
    {
        if (currentAttack == null || Time.time - lastAttackTime > comboResetTime)
            currentAttack = firstNormalAttack;
        else if (currentAttack.nextAttack != null)
            currentAttack = currentAttack.nextAttack;
        else
            return false; // 막타를 쳤으면 무시

        ExecuteAttack(currentAttack);
        return true;
    }

    public bool StartHeavyAttack()
    {
        currentAttack = heavyAttack;
        ExecuteAttack(heavyAttack);
        return true;
    }

    public bool StartSprintAttack()
    {
        currentAttack = sprintAttack;
        ExecuteAttack(sprintAttack);
        return true;
    }

    private void ExecuteAttack(PlayerAttackData attackData)
    {
        player.AnimatorController.PlayAction(attackData.AnimationHash);
        lastAttackTime = Time.time;
    }

    //=============== 타격 및 피격 함수 ========================


    //플레이어가 적을 공격했을 때
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon)
    {
        if (currentAttack == null) return;
        Vector3 hitPoint = targetCollider.ClosestPoint(weapon.transform.position);

        DamageInfo result = new DamageInfo
        {
            attacker = player.gameObject,
            amount = currentAttack.damage,
            knockbackForce = currentAttack.knockbackPower,
            knockbackDuration = currentAttack.knockbackDuration,
            hitPoint = hitPoint,
            hitDirection = transform.forward,
            wasGuarded = false,
            wasParried = false,
        };

        target.TakeDamage(result);
    }

    //데미지 받았을때 반응
    private void EvaluateHitReaction(DamageInfo result)
    {
        ForceResetAttackState(); // 상태 초기화

        int hitAnimHash = 0;

        if (result.attackType == AttackType.Heavy) { hitAnimHash = AnimHash.HeavyHit; }        //Heavy 공격 피격
        else if (result.wasParried) { hitAnimHash = AnimHash.Parry; }                          //패링 성공
        else if (result.wasGuarded) { hitAnimHash = AnimHash.GuardHit; }                       //가드 성공
        else if (result.amount > 0)
        {
            bool isHitFromBehind = Vector3.Dot(result.hitDirection, transform.forward) > 0;
            hitAnimHash = isHitFromBehind ? AnimHash.BackHit : AnimHash.Hit;
        }
        if (hitAnimHash != 0)
        {
            OnHitStunTriggered?.Invoke(hitAnimHash);
        }
    }

    public DamageInfo ProcessDefense(DamageInfo info)
    {
        bool isFrontHit = Vector3.Dot(transform.forward, info.hitDirection) < 0.2f;
        bool canGuard = (info.attackType != AttackType.Heavy);

        if (canGuard && isFrontHit && IsGuarding)
        {
            //패링 성공시
            if (IsParryWindowOpen)
            {
                info.wasParried = true;
                info.amount = 0f;

                if (info.attacker != null &&
                    info.attacker.TryGetComponent<LivingEntity>(out var attacker))
                {
                    attacker.TakePostureDamage(info.postureDamage);
                }
            }
            else //일반 가드 시
            {
                info.wasGuarded = true;
                info.amount = 0f;
            }
        }

        return info;
    }

    public void SetParryWindow(bool isOpen)
    {
        IsParryWindowOpen = isOpen;
    }

    public void ResetCombo() => currentAttack = null;


    public void ForceResetAttackState()
    {
        if (CurrentWeapon != null)
            CurrentWeapon.DisableWeaponCollider();

        ResetCombo();
        SetParryWindow(false);

    }


    //====================애니메이션 이벤트=====================
    public void OnAnimationPlayerAttackStart()
    {
        CurrentWeapon.EnableWeaponCollider();
    }
    public void OnAnimationPlayerAttackEnd()
    {
        CurrentWeapon.DisableWeaponCollider();
    }


}
