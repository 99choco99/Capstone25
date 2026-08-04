using UnityEngine;

/// <summary>
/// AI가 선택한 공격 데이터를 소비 후 WindUp -> Active -> Recovery 순으로 실행
/// </summary>
public class EnemyAttackState : EnemyState
{
    private const float ReboundDuration = 0.35f;

    private AttackData currentAttack;
    private AttackPhase currentPhase;
    private float stateTimer;
    private float reboundTimer;
    private bool isRebounding;

    public override bool UseRootMotion => !isRebounding;

    /// <summary>
    /// 일반 공격은 끊김. 특수 공격은 windup에서 끊김
    /// </summary>
    public override bool CanInterrupted => isRebounding || currentAttack == null || currentAttack.Type == AttackType.Normal || currentPhase != AttackPhase.Active;

    public EnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        enemy.Motor.Stop();
        currentAttack = stateMachine.GetRequestedAttack();

        if (currentAttack == null)
        {
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
            return;
        }

        ReadyAttack(currentAttack);
    }

    public override void Update()
    {
        if (currentAttack == null) return;

        if (isRebounding)
        {
            Rebounding();
            return;
        }

        stateTimer += Time.deltaTime;

        // 공격 준비 구간에 마지막 목격 위치를 향해 회전
        if (currentPhase == AttackPhase.WindUp && enemy.Sense.CurrentTargetInfo.HasTarget)
        {
            Vector3 direction = enemy.Sense.CurrentTargetInfo.TargetPosition - enemy.transform.position;
            enemy.Motor.RotateTowards(direction);
        }

        float duration = currentAttack.DurationInSeconds > 0f? currentAttack.DurationInSeconds: 1f;
        float normalizedTime = stateTimer / duration;

        if (currentPhase == AttackPhase.WindUp && normalizedTime >= currentAttack.ActiveStartTime)
        {
            if (currentAttack.HasHitWindow)
            {
                currentPhase = AttackPhase.Active;
                enemy.Combat.OpenAttackHitWindow();
            }
            else
            {
                currentPhase = AttackPhase.Recovery;
            }
        }
        else if (currentPhase == AttackPhase.Active && normalizedTime >= currentAttack.RecoveryStartTime)
        {
            currentPhase = AttackPhase.Recovery;
            enemy.Combat.CloseAttackHitWindow();
        }
        else if (currentPhase == AttackPhase.Recovery && normalizedTime >= 1f)
        {
            if (currentAttack.NextComboAttack != null)
                ReadyAttack(currentAttack.NextComboAttack);
            else
                stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
        }
    }

    /// <summary>
    /// 공격 준비 완료
    /// </summary>
    private void ReadyAttack(AttackData attack)
    {
        currentAttack = attack;
        stateTimer = 0f;
        isRebounding = false;
        reboundTimer = 0f;
        currentPhase = AttackPhase.WindUp;

        enemy.AnimationController.PlayAttack(currentAttack.AnimationHash, currentAttack.AnimationSpeed);
        enemy.Combat.SetAttackData(currentAttack);

        if (currentAttack.Type == AttackType.Special && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFXAtPoint(SfxKeys.SpecialAttackWarning, enemy.transform.position);
    }

    /// <summary>공격이 상대에게 닿아 결과가 확정됐을 때</summary>
    public override void HandleAttackAccepted(in DamageResult result)
    {
        if (!ShouldRebound(result))
            return;

        BeginRebound(result);
    }

    /// <summary>
    /// 현재 공격이 패링되었고, 반동하기로 되어 있는지?
    /// </summary>
    private bool ShouldRebound(in DamageResult result)
    {
        return result.IsAccepted
            && result.DefenseType == DefenseType.Parry
            && currentAttack != null
            && currentAttack.DeflectResponse == DeflectResponse.Rebound;
    }

    /// <summary>
    /// 현재 공격을 중단하고 패링 반동 상태로 전환
    /// </summary>
    private void BeginRebound(in DamageResult result)
    {
        isRebounding = true;
        reboundTimer = 0f;
        currentPhase = AttackPhase.Recovery;

        enemy.Combat.CancelAttack();
        enemy.Motor.Stop();

        //넉백
        KnockbackSpec knockback = KnockBackPolicy.AttackerKnockBack(result);
        enemy.Motor.StartKnockback(-result.HitDirection, knockback);

        enemy.AnimationController.PlayReaction(AnimHash.AttackRebound,0.05f);
    }

    /// <summary>
    /// 반동 당했을 때 ReboundDuration 동안 공격 불가
    /// </summary>
    private void Rebounding()
    {
        reboundTimer += Time.deltaTime;

        if (reboundTimer >= ReboundDuration)
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
    }

    public override void Exit()
    {
        enemy.Combat.CancelAttack();
        enemy.AIController.NotifyActtackCompleted();
        currentAttack = null;
        currentPhase = AttackPhase.WindUp;
        isRebounding = false;
        reboundTimer = 0f;
    }
}
