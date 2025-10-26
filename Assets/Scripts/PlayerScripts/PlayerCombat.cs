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

    [SerializeField] private Weapon weapon;
    [SerializeField] private Collider weaponCollider;

    public PlayableDirector deathblowDirector;
    [SerializeField] private PlayableAsset FrontdeathblowTimelineAsset; // 앞에서 찌르기
    [SerializeField] private PlayableAsset BehindDeathblowTimelineAsset; // 뒤에서 찌르기
    public event Action<Player> OnExecuteEnd;

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
    public bool StartAttack()
    {
        if(player.animatorManager.isPerformingAction || normalAttacks.Length <= 0) { return false; }
        player.Anim.SetTrigger("Attack");
        Debug.Log("Attack 호출 수 ");

        // 다음 공격을 위해 콤보 인덱스 증가
        comboIndex = (comboIndex + 1) % normalAttacks.Length;
        return true;
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
            player = player,
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


        Quaternion effectRotation = Quaternion.LookRotation(result.hitDirection);
        if (result.attackType != AttackType.Heavy && result.wasParried)
        {
            player.animatorManager.PlayTargetActionAnimation("Parry");
            EffectManager.Instance.PlayEffect("Parry", result.hitPoint, effectRotation, transform);
        }
        else if (result.attackType != AttackType.Heavy && result.wasGuarded)
        {
            EffectManager.Instance.PlayEffect("GuardHit", result.hitPoint, effectRotation, transform);
            player.animatorManager.PlayTargetActionAnimation("GuardHit");
            SoundManager.Instance.PlaySFX("GuardHit");
        }
        else if (result.finalDamage >= 0)
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
        }

        weapon.DisableWeaponCollider();
        OnAttackEnd?.Invoke();
    }

    public void AttemptDeathblow(Enemy enemy)
    {
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

        deathblowDirector.playableAsset = FrontdeathblowTimelineAsset;

        player.Motor.canMove = false;
        player.Motor.canRotate = false;
        player.InputHandler.enabled = false;
        enemy.Motor.Stop();


        Vector3 playerTargetPosition = enemy.transform.position + enemy.transform.forward * 0.9f;
        transform.position = playerTargetPosition;

        Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(directionToEnemy);

        Vector3 directionToPlayer = (transform.position - enemy.transform.position).normalized;
        enemy.transform.rotation = Quaternion.LookRotation(directionToPlayer);

        var outputs = FrontdeathblowTimelineAsset.outputs;
        deathblowDirector.SetGenericBinding(outputs.ElementAt(1).sourceObject, enemy.gameObject);
        deathblowDirector.SetGenericBinding(outputs.ElementAt(2).sourceObject, player.gameObject);
        deathblowDirector.SetGenericBinding(outputs.ElementAt(3).sourceObject, PlayerCamera.Instance.cameraPivotTransform.gameObject);

        OnExecuteEnd += enemy.Stats.DeathBlowProcess;

        deathblowDirector.Play();
        SoundManager.Instance.PlaySFX("ExecuteBGM");
    }

    private void PlayBehindDeathblowTimeline(Enemy enemy)
    {
        deathblowDirector.playableAsset = BehindDeathblowTimelineAsset;

        player.Motor.canMove = false;
        player.Motor.canRotate = false;
        player.InputHandler.enabled = false;
        enemy.Motor.Stop();

        Vector3 playerTargetPosition = enemy.transform.position - enemy.transform.forward * 0.9f;
        transform.position = playerTargetPosition;

        Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(directionToEnemy);

        var outputs = BehindDeathblowTimelineAsset.outputs;
        deathblowDirector.SetGenericBinding(outputs.ElementAt(1).sourceObject, player.gameObject);
        deathblowDirector.SetGenericBinding(outputs.ElementAt(2).sourceObject, enemy.gameObject);
        deathblowDirector.SetGenericBinding(outputs.ElementAt(3).sourceObject, PlayerCamera.Instance.cameraPivotTransform.gameObject);

        OnExecuteEnd += enemy.Stats.DeathBlowProcess;

        deathblowDirector.Play();
        SoundManager.Instance.PlaySFX("ExecuteBGM");
    }

    public void SIG_ExcutedEnd()
    {
        player.Motor.canMove = true;
        player.Motor.canRotate = true;
        player.InputHandler.enabled = true;
        player.StateMachine.TransitionTo(player.StateMachine.PlayerIdleState);
        OnExecuteEnd?.Invoke(player);
        OnExecuteEnd = null; // 구독해제 되나?
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
