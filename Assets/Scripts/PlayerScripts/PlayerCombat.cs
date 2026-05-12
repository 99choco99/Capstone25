using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;



public class PlayerCombat : MonoBehaviour,IWeaponOwner
{
    private Player player;
    public float parryDuration = 0.2f;
    [SerializeField] private Attack[] normalAttacks; // 일반 공격 콤보 데이터

    public Weapon CurrentWeapon { get; private set; }

    [Header("콤보 타이머")]
    [SerializeField] private float comboResetTime = 1.5f;
    private float lastAttackTime = 0f;
    private int comboIndex = 0;


    public event Action OnAttackEnd;
    public event Action<Player> OnExecuteEnd;

    private void Awake()
    {
        player = GetComponent<Player>();
        CurrentWeapon = GetComponentInChildren<Weapon>();
    }
    private void Start()
    {
        if (!player.IsLocalPlayer) { return; }
        player.Stats.OnDamaged += HandleDamageReaction;
        player.Stats.OnDeath += ForceResetAttackState;

    }
    private void OnDestroy()
    {

        if (player != null && player.Stats != null)
        {
            if (!player.IsLocalPlayer) { return; }
            player.Stats.OnDamaged -= HandleDamageReaction;
            player.Stats.OnDeath -= ForceResetAttackState;
        }
    }


    //공격 시작
    public bool StartAttack()
    {
        if(player.AnimatorManager.IsActionLocked || normalAttacks.Length <= 0) { return false; }

        if (Time.time - lastAttackTime > comboResetTime)
        {
            ResetCombo();
        }

        player.Anim.SetTrigger("Attack");
        lastAttackTime = Time.time;
        comboIndex = (comboIndex + 1) % normalAttacks.Length;
        return true;
    }

    //콤보 리셋
    public void ResetCombo() => comboIndex = 0;


    //플레이어가 적을 공격했을 때
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon)
    {
        Attack currentAttackData = normalAttacks[comboIndex];
        Vector3 hitPoint = targetCollider.ClosestPoint(weapon.transform.position);

        DamageInfo result = new DamageInfo
        {
            attacker = player.gameObject,
            amount = currentAttackData.damage,
            knockbackForce = currentAttackData.knockbackPower,
            knockbackDuration = currentAttackData.knockbackDuration,
            hitPoint = hitPoint,
            hitDirection = transform.forward,
            wasGuarded = false,
            wasParried = false,
        };

        target.OnDamage(result);
    }

    //데미지 받았을때 반응
    private void HandleDamageReaction(DamageInfo result)
    {
        if (player.Stats.dead) return;

        ForceResetAttackState(); // 상태 초기화

        //Heavy 공격 피격
        if (result.attackType == AttackType.Heavy)
        {
            player.AnimatorManager.PlayAction(AnimHash.HeavyHit);
            return;
        }

        //패링 성공
        if (result.wasParried)
        {
            player.AnimatorManager.PlayAction(AnimHash.Parry);
            return;
        }

        //가드 성공
        if (result.wasGuarded)
        {
            player.AnimatorManager.PlayAction(AnimHash.GuardHit);
            return;
        }

        //일반 피격
        if (result.amount > 0)
        {
            bool isHitFromBehind = Vector3.Dot(result.hitDirection, transform.forward) > 0;

            if (isHitFromBehind)
                player.AnimatorManager.PlayAction(AnimHash.BackHit);
            else
                player.AnimatorManager.PlayAction(AnimHash.Hit);
        }

        CurrentWeapon.DisableWeaponCollider();
        OnAttackEnd?.Invoke();
    }



    public void ForceResetAttackState()
    {
        if (CurrentWeapon != null)
            CurrentWeapon.DisableWeaponCollider();

        ResetCombo();

        player.Motor.CanRotate = true;

        OnAttackEnd?.Invoke();
    }
    public void OnAnimationPlayerAttackStart()
    {
        player.Motor.CanRotate = false;
        CurrentWeapon.EnableWeaponCollider();
        SoundManager.Instance.PlaySFX("Attack");
    }
    public void OnAnimationPlayerAttackEnd()
    {
        CurrentWeapon.DisableWeaponCollider();
        OnAttackEnd?.Invoke();
    }


}
