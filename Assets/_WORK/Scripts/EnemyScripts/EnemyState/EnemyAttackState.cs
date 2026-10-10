using UnityEngine;

using SoundEffectManager;
/// <summary>
/// AI가 선택한 공격 데이터를 소비 후 WindUp -> Active -> Recovery 순으로 실행
/// </summary>
public class EnemyAttackState : EnemyState
{
    private float ReboundDuration;

    private EnemyAttackData currentAttack;
    private EnemyAttackData nextComboAttack;
    private AttackPhase currentPhase;
    private float attackProgress;
    private float nextInterruptTime;
    private float reboundTimer;
    private bool isRebounding;
    private bool isCounterOpening;
    private bool finishedRecovery;
    private bool hasAcceptedContact;
    private int comboAttackCount;

    public override bool UseRootMotion => !isRebounding;
    public bool CanApplyAttackPressure => !isRebounding && currentPhase == AttackPhase.Active;
    public bool CanMaintainAttackPressure => !isRebounding && currentAttack != null && currentAttack.HasHitWindow;
    public bool CanAssistAttackReach => !isRebounding && currentAttack != null && currentAttack.HasHitWindow
        && enemy.AnimationController.TryGetAttackProgress(currentAttack.AnimationHash, out float frame, out _) && frame < currentAttack.recoveryStartFrame;

    /// <summary>
    /// WindUp은 공격 중단 쿨타임이 끝났을 때, Recovery는 마지막 연계에서 피격으로 끊김
    /// </summary>
    public override bool CanInterrupted => isRebounding || currentAttack == null || (currentPhase == AttackPhase.Recovery && nextComboAttack == null)
        || (currentPhase == AttackPhase.WindUp && !currentAttack.ResistWindUpHit
            && !isCounterOpening && Time.time >= nextInterruptTime);

    public EnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void OnHit(in DamageResult result)
    {
        bool wasWindUp = currentAttack != null && currentPhase == AttackPhase.WindUp;
        base.OnHit(result);

        if (wasWindUp && stateMachine.CurrentState == stateMachine.EnemyHitState)
            nextInterruptTime = Time.time + CombatSettings.Current.EnemyAttackInterruptCooldown;
    }

    public override void Enter()
    {
        enemy.Motor.Stop();
        enemy.Motor.ClearAttackReach();
        finishedRecovery = false;
        comboAttackCount = 0;
        EnemyAttackData attack = stateMachine.GetRequestedAttack();

        if (attack == null)
        {
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
            return;
        }

        ReadyAttack(attack);
        isCounterOpening = enemy.AIController.IsCounterAttack;
    }

    public override void Update()
    {
        if (currentAttack == null) return;

        if (isRebounding)
        {
            Rebounding();
            return;
        }

        float speed = currentAttack.GetPlaybackSpeed(attackProgress);
        enemy.AnimationController.Animator.speed = currentAttack.AnimationSpeed * speed;

        // 공격 준비 구간에 마지막 목격 위치를 향해 회전
        if (currentPhase == AttackPhase.WindUp && enemy.Sense.CurrentTargetInfo.HasTarget)
        {
            Vector3 direction = enemy.Sense.CurrentTargetInfo.TargetPosition - enemy.transform.position;
            enemy.Motor.RotateTowards(direction);
        }
    }

    public void UpdateAnimation()
    {
        if (isRebounding || currentAttack == null
            || !enemy.AnimationController.TryGetAttackProgress(currentAttack.AnimationHash, out float currentFrame, out attackProgress))
            return;

        if (currentPhase == AttackPhase.WindUp && currentFrame >= currentAttack.activeStartFrame)
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
        if (currentPhase == AttackPhase.Active && currentFrame >= currentAttack.recoveryStartFrame)
        {
            currentPhase = AttackPhase.Recovery;
            enemy.Combat.CloseAttackHitWindow(preservePressure: CanMaintainAttackPressure);
        }
        if (currentPhase == AttackPhase.Recovery)
        {
            EnemyTargetInfo target = enemy.Sense.CurrentTargetInfo;
            bool isRetreat = currentAttack == enemy.Stats.Data.retreatAttack;
            float transitionFrames = AttackData.FrameRate * enemy.AnimationController.Animator.speed * AnimationController.AttackTransitionDuration;
            float comboFrame = Mathf.Max(currentAttack.recoveryStartFrame, currentAttack.NextComboStartFrame > 0f ? currentAttack.NextComboStartFrame : currentAttack.totalFrames - transitionFrames);
            if (nextComboAttack != null && currentFrame >= comboFrame)
            {
                EnemyAttackData nextAttack = nextComboAttack;
                nextComboAttack = null;

                if ((!currentAttack.RequireContactForCombo || hasAcceptedContact) && target.HasTarget && target.CanSeeTarget
                    && (!nextAttack.HasHitWindow || target.Distance <= nextAttack.MaxDistance + enemy.Stats.Data.distanceTolerance || enemy.Motor.CanAssistNextAttack(nextAttack))
                    && (!isRetreat || !nextAttack.HasHitWindow || nextAttack.IsInStartRange(target.Distance)))
                {
                    ReadyAttack(nextAttack);
                    return;
                }

                enemy.Motor.StopAttackPressure();
            }
            if (currentFrame >= currentAttack.totalFrames)
            {
                finishedRecovery = true;
                stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
            }
        }
    }

    /// <summary>
    /// 공격 준비 완료
    /// </summary>
    private void ReadyAttack(EnemyAttackData attack)
    {
        enemy.Motor.BeginAttackReach(attack);
        bool playWarning = attack.Type != AttackType.Normal && (currentAttack == null || currentAttack.Type == AttackType.Normal);
        currentAttack = attack;
        if (attack.HasHitWindow)
            comboAttackCount++;
        nextComboAttack = attack.NextComboAttack;
        attackProgress = 0f;
        isRebounding = false;
        isCounterOpening = false;
        reboundTimer = 0f;
        currentPhase = AttackPhase.WindUp;
        hasAcceptedContact = false;

        enemy.AnimationController.PlayAttack(currentAttack.AnimationHash, currentAttack.AnimationSpeed * currentAttack.GetPlaybackSpeed(0f));
        enemy.Combat.SetAttackData(currentAttack);

        if (playWarning && SoundManager.Instance != null)
            SoundManager.Instance.Play(SfxKeys.SpecialAttackWarning, enemy.transform.position);
    }

    /// <summary>공격이 상대에게 닿아 결과가 확정됐을 때</summary>
    public override void HandleAttackAccepted(in DamageResult result)
    {
        if (!result.IsAccepted) return;

        hasAcceptedContact = true;
        if (ShouldRebound(result))
        {
            BeginRebound();
            return;
        }

        EnemyAttackData branch = result.DefenseType switch
        {
            DefenseType.NormalGuard => currentAttack.GuardComboAttack,
            DefenseType.Parry => currentAttack.ParryComboAttack,
            _ => null
        };
        if (branch != null)
            nextComboAttack = branch;
    }

    /// <summary>
    /// 현재 공격이 패링되었고, 반동하기로 되어 있는지?
    /// </summary>
    private bool ShouldRebound(in DamageResult result)
    {
        return result.DefenseType == DefenseType.Parry
            && currentAttack != null && currentAttack.DeflectResponse == DeflectResponse.Rebound;
    }

    /// <summary>
    /// 현재 공격을 중단하고 패링 반동 상태로 전환
    /// </summary>
    private void BeginRebound()
    {
        ReboundDuration = CombatSettings.Current.ParriedRecoveryDuration;
        isRebounding = true;
        nextComboAttack = null;
        reboundTimer = 0f;
        currentPhase = AttackPhase.Recovery;

        enemy.Combat.CancelAttack();
        enemy.Motor.ClearAttackReach();
        enemy.Motor.Stop();

        enemy.AnimationController.PlayReaction(AnimHash.AttackRebound);
    }

    /// <summary>
    /// 반동의 최소 대기와 동작 진행도를 만족하면 행동 복귀
    /// </summary>
    private void Rebounding()
    {
        reboundTimer += Time.deltaTime;

        if (reboundTimer >= ReboundDuration && enemy.AnimationController.TryGetAttackProgress(AnimHash.AttackRebound, out _, out float reboundProgress)
            && reboundProgress >= CombatSettings.Current.EnemyReboundExitProgress)
        {
            finishedRecovery = true;
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
        }
    }

    public override void Exit()
    {
        enemy.Combat.CancelAttack();
        enemy.Motor.ClearAttackReach();
        enemy.AnimationController.Animator.speed = 1f;
        bool completedLongCombo = comboAttackCount >= 3;
        bool finishedHeavyAttack = currentAttack != null && currentAttack.HasHitWindow
            && (currentAttack.ImpactLevel == KnockBackLevel.Heavy || currentAttack.Type != AttackType.Normal);
        enemy.AIController.FinishAttack(finishedRecovery, completedLongCombo || finishedHeavyAttack, isRebounding, enemy.Sense.CurrentTargetInfo);
        currentAttack = null;
        nextComboAttack = null;
        currentPhase = AttackPhase.WindUp;
        isRebounding = false;
        isCounterOpening = false;
        reboundTimer = 0f;
        finishedRecovery = false;
        hasAcceptedContact = false;
        comboAttackCount = 0;
    }
}
