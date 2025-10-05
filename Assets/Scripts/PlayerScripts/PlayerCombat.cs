using System;
using Unity.VisualScripting;
using UnityEngine;


public class PlayerCombat : MonoBehaviour
{
    private Player player;
    public float parryDuration = 0.2f;
    [SerializeField] private Attack[] normalAttacks; // 일반 공격 콤보 데이터

    [SerializeField] private Collider weaponCollider;

    private int comboIndex = 0;

    public event Action OnAttackEnd;

    private void Awake()
    {
        player = GetComponent<Player>();


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
    public void OnWeaponHit(IDamageable target, Collider targetCollider)
    {
        Attack currentAttackData = normalAttacks[comboIndex];

        Vector3 hitPoint = targetCollider.ClosestPoint(weaponCollider.transform.position);
        Vector3 hitDirection = (targetCollider.transform.position - transform.position).normalized;
        hitDirection.y = 0;

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
            // 패링 성공 시 연출
            Debug.Log("PlayerCombat: 패링 연출 실행!");
            // TODO: 패링 성공 사운드, 이펙트, 애니메이션 트리거
            player.animatorManager.PlayTargetActionAnimation("Parry");
        }
        else if (result.wasGuarded)
        {
            // 가드 성공 시 연출
            Debug.Log("PlayerCombat: 가드 연출 실행!");
            // TODO: 가드 성공 사운드, 이펙트, 애니메이션 트리거

            player.animatorManager.PlayTargetActionAnimation("GuardHit");
        }
        else if (result.finalDamage > 0)
        {
            // 실제 데미지를 입었을 때 연출
            Debug.Log($"PlayerCombat: 피격 연출 실행! 데미지: {result.finalDamage}");

            player.animatorManager.PlayTargetActionAnimation("Hit");
        }
        player.Motor.StartKnockBack(result.hitDirection, result.knockbackForce, result.knockbackDuration); // 넉백 실행
    }




    public void AE_playerAttackStart()
    {
        player.Motor.canRotate = false;
        weaponCollider.enabled = true;

    }
    public void AE_playerAttackEnd()
    {
        OnAttackEnd?.Invoke();

        weaponCollider.enabled = false;
    }



}
