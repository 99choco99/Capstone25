using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;



public class PlayerCombat : MonoBehaviour,IWeaponOwner
{
    private Player player;
    public float parryDuration = 0.2f;
    [SerializeField] private Attack[] normalAttacks; // �Ϲ� ���� �޺� ������

    [SerializeField] private Weapon weapon;
    [SerializeField] private Collider weaponCollider;

    public PlayableDirector deathblowDirector;
    public bool IsPlayingDirector;
    [SerializeField] private PlayableAsset FrontdeathblowTimelineAsset; // �տ��� ���
    [SerializeField] private PlayableAsset BehindDeathblowTimelineAsset; // �ڿ��� ���
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
        if (!player.IsLocalPlayer) { return; }
        player.Stats.OnDamaged += HandleDamageReaction;
        player.Stats.OnDeath += AE_playerAttackEnd;

        if (deathblowDirector != null)
        {
            deathblowDirector.stopped += OnDeathblowTimelineFinished;
        }
    }
    private void OnDestroy()
    {

        if (player != null && player.Stats != null)
        {
            if (!player.IsLocalPlayer) { return; }
            player.Stats.OnDamaged -= HandleDamageReaction;
            player.Stats.OnDeath -= AE_playerAttackEnd;
            if (deathblowDirector != null)
            {
                deathblowDirector.stopped -= OnDeathblowTimelineFinished;
            }
        }
    }


    //���� ����
    public bool StartAttack()
    {
        if(player.AnimatorManager.isPerformingAction || normalAttacks.Length <= 0) { return false; }
        player.Anim.SetTrigger("Attack");

        // ���� ������ ���� �޺� �ε��� ����
        comboIndex = (comboIndex + 1) % normalAttacks.Length;
        return true;
    }

    //�޺� ����
    public void ResetCombo()
    {
        comboIndex = 0;
    }


    //�÷��̾ ���� �������� ��
    public void OnWeaponHit(IDamageable target, Collider targetCollider, Weapon weapon)
    {
        Attack currentAttackData = normalAttacks[comboIndex];
        Vector3 hitPoint = targetCollider.ClosestPoint(weaponCollider.transform.position);
        Vector3 hitDirection = transform.forward;

        DamageInfo result = new DamageInfo
        {
            player = player,
            damage = currentAttackData.damage,
            knockbackForce = currentAttackData.knockbackPower,
            knockbackDuration = currentAttackData.knockbackDuration,
            hitPoint = hitPoint,
            hitDirection = hitDirection,
            wasGuarded = false,
            wasParried = false,
        };

        target.OnDamage(result);
    }

    //������ �޾����� ����
    private void HandleDamageReaction(DamageInfo result)
    {
        if (player.Stats.dead) return;


        Quaternion effectRotation = Quaternion.LookRotation(result.hitDirection);
        if (result.attackType != AttackType.Heavy && result.wasParried)
        {
            player.AnimatorManager.PlayTargetActionAnimation("Parry");
            EffectManager.Instance.PlayEffect("Parry", result.hitPoint, effectRotation, transform);
        }
        else if (result.attackType != AttackType.Heavy && result.wasGuarded)
        {
            EffectManager.Instance.PlayEffect("GuardHit", result.hitPoint, effectRotation, transform);
            player.AnimatorManager.PlayTargetActionAnimation("GuardHit");
            SoundManager.Instance.PlaySFX("GuardHit");
        }
        else if (result.damage >= 0)
        {
            if (Vector3.Dot(result.hitDirection, transform.forward) > 0)
            {
                player.AnimatorManager.PlayTargetActionAnimation("BackHit");
            }
            else
            {
                player.AnimatorManager.PlayTargetActionAnimation("Hit");

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
        weapon.DisableWeaponCollider();
        player.TargetingSystem.DeselectTarget();
    }

    // ���� �λ�
    private void PlayFrontDeathblowTimeline(Enemy enemy)
    {
        IsPlayingDirector = true;
        deathblowDirector.playableAsset = FrontdeathblowTimelineAsset;


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
        deathblowDirector.SetGenericBinding(outputs.ElementAt(3).sourceObject, Camear.Instance.gameObject);

        enemy.Stats.isDeflecting = false;
        enemy.Stats.IsPlayingDeathBlow = true;
        OnExecuteEnd += enemy.Stats.DeathBlowProcess;

        deathblowDirector.Play();
        SoundManager.Instance.PlaySFX("ExecuteBGM");
    }

    private void PlayBehindDeathblowTimeline(Enemy enemy)
    {
        IsPlayingDirector = true;
        deathblowDirector.playableAsset = BehindDeathblowTimelineAsset;

        enemy.Motor.Stop();

        Vector3 playerTargetPosition = enemy.transform.position - enemy.transform.forward * 0.9f;
        transform.position = playerTargetPosition;

        Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(directionToEnemy);

        var outputs = BehindDeathblowTimelineAsset.outputs;
        deathblowDirector.SetGenericBinding(outputs.ElementAt(1).sourceObject, player.gameObject);
        deathblowDirector.SetGenericBinding(outputs.ElementAt(2).sourceObject, enemy.gameObject);
        deathblowDirector.SetGenericBinding(outputs.ElementAt(3).sourceObject, Camear.Instance.gameObject);

        OnExecuteEnd += enemy.Stats.DeathBlowProcess;

        deathblowDirector.Play();
        SoundManager.Instance.PlaySFX("ExecuteBGM");
    }

    public void OnDeathblowTimelineFinished(PlayableDirector director)
    {
        OnExecuteEnd?.Invoke(player);
        OnExecuteEnd = null; // ��������

        player.StateMachine.TransitionTo(player.StateMachine.PlayerIdleState);
        player.Combat.IsPlayingDirector = false;
        player.Stats.isInvincible = false;
        player.Motor.canMove = true;
        player.Motor.canRotate = true;
        player.InputHandler.enabled = true;
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
