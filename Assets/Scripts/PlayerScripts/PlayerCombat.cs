using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using static UnityEngine.EventSystems.EventTrigger;



public class PlayerCombat : MonoBehaviour,IWeaponOwner
{
    private Player player;
    public float parryDuration = 0.2f;
    [SerializeField] private Attack[] normalAttacks; // 일반 공격 콤보 데이터

    [SerializeField] private Weapon weapon;
    [SerializeField] private Collider weaponCollider;

    public PlayableDirector deathblowDirector;
    [SerializeField] private PlayableAsset FrontdeathblowTimelineAsset; // 인스펙터에서 타임라인 에셋을 직접 할당
    [SerializeField] private PlayableAsset BehindDeathblowTimelineAsset; // 인스펙터에서 타임라인 에셋을 직접 할당

    private int comboIndex = 0;
    public event Action OnAttackEnd;

    private void Awake()
    {
        player = GetComponent<Player>();
        weapon = GetComponentInChildren<Weapon>();
        weaponCollider = weapon.GetComponent<Collider>();
        deathblowDirector = GetComponent<PlayableDirector>();
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
            EffectManager.Instance.PlayEffect("Parry", result.hitPoint, Quaternion.identity, transform);
        }
        else if (result.wasGuarded)
        {
            player.animatorManager.PlayTargetActionAnimation("GuardHit");
            EffectManager.Instance.PlayEffect("GuardHit", result.hitPoint, Quaternion.identity,transform);
        }
        else if (result.finalDamage > 0)
        {
            if (Vector3.Dot(result.hitDirection, transform.forward) > 0)
            {
                player.animatorManager.PlayTargetActionAnimation("BackHit");
            }
            else
            {
                player.animatorManager.PlayTargetActionAnimation("Hit");

            }
            SoundManager.Instance.PlaySFX("Hit");
            SoundManager.Instance.PlaySFX("Cutting flesh");
            EffectManager.Instance.PlayEffect("Blood", result.hitPoint, Quaternion.identity, transform);


            OnAttackEnd?.Invoke();
        }
    }

    public void AttemptDeathblow(Enemy enemy)
    {
        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        if (distance > 2f) return; // 너무 멀면 취소

        if(Vector3.Dot(enemy.transform.forward,transform.forward) < 0)
        {
            PlayFrontDeathblowTimeline(enemy);
        }
        else
        {
            PlayBehindDeathblowTimeline(enemy);
        }

    }

    // 실제 인살
    private void PlayFrontDeathblowTimeline(Enemy enemy)
    {
        // --- 1. 타임라인 에셋 및 바인딩 설정 ---
        deathblowDirector.playableAsset = FrontdeathblowTimelineAsset;

        // --- 2. 준비 단계: 타임라인 시작 전 상태 변경 ---
        // (이 부분은 시그널로 옮겨도 됩니다. 취향에 따라 선택)
        player.Motor.canMove = false;
        player.Motor.canRotate = false;
        enemy.Motor.Stop();


        Vector3 playerTargetPosition = enemy.transform.position + enemy.transform.forward * 0.9f;
        transform.position = playerTargetPosition;

        Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(directionToEnemy);

        Vector3 directionToPlayer = (transform.position - enemy.transform.position).normalized;
        enemy.transform.rotation = Quaternion.LookRotation(directionToPlayer);

        // 타임라인 트랙에 플레이어와 적을 동적으로 할당(바인딩)
        // 타임라인 에디터의 트랙 순서와 일치해야 합니다.
        // 예: 0번=플레이어 애니메이션, 1번=적 애니메이션, 2번=플레이어 시그널
        var outputs = FrontdeathblowTimelineAsset.outputs;
        deathblowDirector.SetGenericBinding(outputs.ElementAt(1).sourceObject, player.gameObject);
        deathblowDirector.SetGenericBinding(outputs.ElementAt(2).sourceObject, enemy.gameObject);
        deathblowDirector.SetGenericBinding(outputs.ElementAt(3).sourceObject, player.gameObject);
        deathblowDirector.SetGenericBinding(outputs.ElementAt(4).sourceObject, enemy.gameObject);



        // --- 3. 타임라인 재생 ---
        deathblowDirector.Play();
        SoundManager.Instance.PlaySFX("ExecuteBGM"); // BGM은 타임라인 시작과 함께 바로 재생
    }

    private void PlayBehindDeathblowTimeline(Enemy enemy)
    {
        // --- 1. 타임라인 에셋 및 바인딩 설정 ---
        deathblowDirector.playableAsset = BehindDeathblowTimelineAsset;

        player.Motor.canMove = false;
        player.Motor.canRotate = false;
        enemy.Motor.Stop();

        Vector3 playerTargetPosition = enemy.transform.position - enemy.transform.forward * 0.9f;
        transform.position = playerTargetPosition;

        Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(directionToEnemy);

        var outputs = BehindDeathblowTimelineAsset.outputs;
        deathblowDirector.SetGenericBinding(outputs.ElementAt(1).sourceObject, player.gameObject);
        deathblowDirector.SetGenericBinding(outputs.ElementAt(2).sourceObject, enemy.gameObject);

        // --- 3. 타임라인 재생 ---
        deathblowDirector.Play();
        SoundManager.Instance.PlaySFX("ExecuteBGM"); // BGM은 타임라인 시작과 함께 바로 재생
    }

    public void SIG_ExcutedEnd()
    {
        player.Motor.canMove = true;
        player.Motor.canRotate = true;
        player.StateMachine.TransitionTo(player.StateMachine.PlayerIdleState);
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
