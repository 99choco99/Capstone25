using System;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


public class PlayerCombat : MonoBehaviour,IWeaponOwner
{
    private Player player;
    public float parryDuration = 0.2f;
    [SerializeField] private Attack[] normalAttacks; // 일반 공격 콤보 데이터

    [SerializeField] private Weapon weapon;
    [SerializeField] private Collider weaponCollider;

    private int comboIndex = 0;
    public event Action OnAttackEnd;

    private void Awake()
    {
        player = GetComponent<Player>();
        weapon = GetComponentInChildren<Weapon>();
        weaponCollider = weapon.GetComponent<Collider>();

    }
    private void Start()
    {
        player.Stats.OnDamaged += HandleDamageReaction;
    }
    private void OnDestroy()
    {

        if (player != null && player.Stats != null)
        {
            player.Stats.OnDamaged -= HandleDamageReaction;
        }
    }

    //공격 시작
    public void StartAttack()
    {
        if(normalAttacks.Length <= 0) { return; }
        player.Anim.SetTrigger("Attack");

        // 다음 공격을 위해 콤보 인덱스 증가
        comboIndex = (comboIndex + 1) % normalAttacks.Length;

    }

    //콤보 리셋
    public void ResetCombo()
    {
        comboIndex = 0;
    }

    //플레이어가 적을 공격했을 때
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon)
    {
        Attack currentAttackData = normalAttacks[comboIndex];

        Vector3 hitPoint = targetCollider.ClosestPoint(weaponCollider.transform.position);
        Vector3 hitDirection = transform.forward;

        DamageInfo result = new DamageInfo
        {
            finalDamage = currentAttackData.damage,
            knockbackForce = currentAttackData.knockbackPower,
            knockbackDuration = currentAttackData.knockbackDuration,
            hitPoint = hitPoint,
            hitDirection = hitDirection,
            wasGuarded = false,
            wasParried = false,
        };

        target.OnDamage(result);
    }

    //데미지 받았을때 반응
    private void HandleDamageReaction(DamageInfo result)
    {
        if (player.Stats.dead) return;

        if (result.wasParried)
        {

            player.animatorManager.PlayTargetActionAnimation("Parry");
            SoundManager.Instance.PlaySFX("Parry");
        }
        else if (result.wasGuarded)
        {

            player.animatorManager.PlayTargetActionAnimation("GuardHit");
        }
        else if (result.finalDamage > 0)
        {

            player.animatorManager.PlayTargetActionAnimation("Hit");
            SoundManager.Instance.PlaySFX("Hit");
            Quaternion effectRotation = Quaternion.LookRotation(result.hitDirection);
            EffectManager.Instance.PlayEffect("Blood", result.hitPoint, effectRotation);
            OnAttackEnd?.Invoke();
        }
    }




    public void AE_playerAttackStart()
    {
        player.Motor.canRotate = false;
        weapon.EnableWeaponCollider();
        SoundManager.Instance.PlaySFX("Attack");
    }
    public void AE_playerAttackEnd()
    {
        weapon.DisableWeaponCollider();
        OnAttackEnd?.Invoke();
    }


}
