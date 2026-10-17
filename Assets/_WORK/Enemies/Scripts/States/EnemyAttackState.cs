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
    private float reboundTimer;
    private bool isRebounding;
    private bool isCounterOpening;
    private bool finishedRecovery;
    private bool hasAcceptedContact;
    private bool hasParriedContact;
    private DefenseType lastContactDefense;
    private float lastContactTime;
    private float recoveryStartTime;
    private bool hasRecordedRecovery;
    private bool playedLandingImpact;
    private readonly CombatFeedback feedback;
    private int comboAttackCount;

    public override bool UseRootMotion => !isRebounding;
    public bool CanApplyAttackPressure => !isRebounding && currentPhase == AttackPhase.Active;
    public bool CanMaintainAttackPressure => !isRebounding && currentAttack != null && currentAttack.HasHitWindow;
    public bool CanAssistAttackReach => !isRebounding
        && currentAttack != null
        && currentAttack.HasHitWindow
        && enemy.AnimationController.TryGetAttackProgress(currentAttack.AnimationHash, out float frame, out _)
        && frame < currentAttack.RecoveryStartFrame;

    /// <summary>
    /// 일반 WindUp은 피격으로 끊기며, 명시된 준비 저항과 패링으로 얻은 반격 진입은 유지합니다.
    /// Recovery는 마지막 연계에서 피격으로 끊깁니다.
    /// </summary>
    public override bool CanInterrupted => isRebounding
        || currentAttack == null
        || (currentPhase == AttackPhase.Recovery
        && nextComboAttack == null)
        || (currentPhase == AttackPhase.WindUp
        && !isCounterOpening
        && !HasWindUpHitResistance);

    private bool HasWindUpHitResistance => currentAttack.ResistWindUpHit
        || currentAttack.WindUpResistanceStartFrame > 0f
            && enemy.AnimationController.TryGetAttackProgress(currentAttack.AnimationHash, out float frame, out _)
            && frame >= currentAttack.WindUpResistanceStartFrame;

    public EnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        feedback = enemy.GetComponent<CombatFeedback>();
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

        ReadyAttack(attack, startsPattern: true);
        isCounterOpening = enemy.AIController.IsCounterAttack;
    }

    public override void Update()
    {
        if (currentAttack == null)
            return;

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
            || !enemy.AnimationController.TryGetAttackProgress(
                currentAttack.AnimationHash,
                out float currentFrame,
                out attackProgress))
            return;

        if (!playedLandingImpact
            && currentAttack.LandingImpactFrame > 0f
            && currentFrame >= currentAttack.LandingImpactFrame)
        {
            playedLandingImpact = true;
            if (feedback != null)
                feedback.PlayLandingImpact(currentAttack, hasAcceptedContact);
        }

        if (currentPhase == AttackPhase.WindUp && currentFrame >= currentAttack.ActiveStartFrame)
        {
            if (currentAttack.HasHitWindow)
            {
                currentPhase = AttackPhase.Active;
                enemy.Combat.OpenAttackHitWindow();
            }
            else
            {
                currentPhase = AttackPhase.Recovery;
                recoveryStartTime = Time.time;
            }
        }
        if (currentPhase == AttackPhase.Active)
            enemy.Combat.UpdateAttackHitWindows(currentFrame);
        if (currentPhase == AttackPhase.Active && currentFrame >= currentAttack.RecoveryStartFrame)
        {
            currentPhase = AttackPhase.Recovery;
            recoveryStartTime = Time.time;
            enemy.Combat.CloseAttackHitWindow(preservePressure: CanMaintainAttackPressure);
        }
        if (currentPhase == AttackPhase.Recovery)
        {
            EnemyTargetInfo target = enemy.Sense.CurrentTargetInfo;
            bool isRetreat = currentAttack == enemy.Stats.Data.retreatAttack;
            float transitionFrames = AttackData.FrameRate * enemy.AnimationController.Animator.speed * AnimationController.AttackTransitionDuration;
            float comboFrame = Mathf.Max(
                currentAttack.RecoveryStartFrame,
                currentAttack.NextComboStartFrame > 0f
                ? currentAttack.NextComboStartFrame
                : currentAttack.totalFrames - transitionFrames);
            if (nextComboAttack != null && currentFrame >= comboFrame)
            {
                EnemyAttackData nextAttack = nextComboAttack;
                nextComboAttack = null;
                bool isCommittedSpecialAttack = currentAttack.Type != AttackType.Normal && !currentAttack.HasHitWindow
                    && nextAttack.Type != AttackType.Normal && nextAttack.HasHitWindow;

                if (isRetreat && enemy.AIController.TryQueueRetreatReentry(currentAttack, nextAttack, target))
                {
                    enemy.Motor.StopAttackPressure();
                }
                else if ((!currentAttack.RequireContactForCombo
                    || hasAcceptedContact)
                    && target.HasTarget
                    && (isCommittedSpecialAttack
                    || target.CanSeeTarget)
                    && (isCommittedSpecialAttack
                    || !nextAttack.HasHitWindow
                    || target.Distance <= nextAttack.MaxDistance + enemy.Stats.Data.distanceTolerance
                    || enemy.Motor.CanAssistNextAttack(nextAttack))
                    && (!isRetreat
                    || !nextAttack.HasHitWindow
                    || nextAttack.IsInStartRange(target.Distance))
                    && (!isRetreat
                    || enemy.AIController.CanStartPreparedAttack(nextAttack)))
                {
                    bool startsPreparedPattern = isRetreat && enemy.AIController.IsPreparingRetreat;
                    if (isRetreat)
                        enemy.AIController.BeginPreparedAttack(nextAttack);
                    ReadyAttack(nextAttack, startsPattern: startsPreparedPattern);
                    return;
                }

                enemy.Motor.StopAttackPressure();
            }
            if (nextComboAttack == null && !hasRecordedRecovery)
            {
                hasRecordedRecovery = true;
                enemy.AIController.BeginAttackRecovery(
                    currentAttack,
                    comboAttackCount,
                    hasAcceptedContact,
                    lastContactDefense,
                    recoveryStartTime,
                    lastContactTime);
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
    private void ReadyAttack(EnemyAttackData attack, bool startsPattern = false)
    {
        enemy.Motor.BeginAttackReach(attack);
        bool playWarning = attack.Type != AttackType.Normal
            && (currentAttack == null
            || currentAttack.Type == AttackType.Normal);
        currentAttack = attack;
        if (attack.HasHitWindow)
            comboAttackCount++;
        nextComboAttack = enemy.AIController.GetPreparedRetreatAttack(attack) ?? attack.NextComboAttack;
        attackProgress = 0f;
        isRebounding = false;
        isCounterOpening = false;
        currentPhase = AttackPhase.WindUp;
        hasAcceptedContact = false;
        hasParriedContact = false;
        lastContactDefense = DefenseType.None;
        lastContactTime = float.NegativeInfinity;
        recoveryStartTime = 0f;
        hasRecordedRecovery = false;
        playedLandingImpact = false;

        enemy.AnimationController.PlayAttack(
            currentAttack.AnimationHash,
            currentAttack.AnimationSpeed * currentAttack.GetPlaybackSpeed(0f));
        enemy.Combat.SetAttackData(currentAttack);
        if (startsPattern)
            enemy.AIController.RecordAttackStarted(currentAttack);

        if (playWarning && SoundManager.Instance != null)
            SoundManager.Instance.Play(SfxKeys.SpecialAttackWarning, enemy.transform.position);
    }

    /// <summary>공격이 상대에게 닿아 결과가 확정됐을 때</summary>
    public override void HandleAttackAccepted(in DamageResult result)
    {
        if (!result.IsAccepted)
            return;

        hasAcceptedContact = true;
        lastContactDefense = result.DefenseType;
        lastContactTime = Time.time;
        enemy.AIController.RecordAttackContact(result.DefenseType,
            Vector3.Dot(enemy.transform.right, result.HitPoint - enemy.transform.position));
        if (ShouldRebound(result))
        {
            BeginRebound();
            return;
        }

        hasParriedContact |= result.DefenseType == DefenseType.Parry;
        if (hasParriedContact)
        {
            // 같은 모션의 다음 무기 접촉이 가드됐어도 이미 선택한 패링 분기는 유지합니다.
            nextComboAttack = currentAttack.ParryComboAttack ?? currentAttack.NextComboAttack;
        }
        else if (result.DefenseType == DefenseType.NormalGuard && currentAttack.GuardComboAttack != null)
        {
            nextComboAttack = currentAttack.GuardComboAttack;
        }
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
        ReboundDuration = GetReboundRecoveryDuration();
        isRebounding = true;
        nextComboAttack = null;
        reboundTimer = 0f;
        currentPhase = AttackPhase.Recovery;

        enemy.Combat.CancelAttack();
        enemy.Motor.ClearAttackReach();
        enemy.Motor.Stop();

        enemy.AnimationController.PlayReaction(AnimHash.AttackRebound);
    }

    private float GetReboundRecoveryDuration()
    {
        float duration = CombatSettings.Current.ParriedRecoveryDuration;
        if (enemy.Stats.Data.attackPolicy != null)
            return Mathf.Max(duration, enemy.Stats.Data.attackPolicy.reboundOpportunityDuration);
        if (currentAttack != null
            && currentAttack.GetEndResponse(EnemyAttackContact.Deflected) != EnemyAttackEndResponse.Automatic)
            return Mathf.Max(duration, currentAttack.ResponseOpportunityDuration);
        PlayerAttackData counterAttack = Player.LocalPlayer?.Combat?.FirstAttackData;
        if (counterAttack != null && counterAttack.HasHitWindow)
            duration = Mathf.Max(duration, CombatSettings.Current.PlayerParryRecoveryDuration
                + counterAttack.RecoveryStartFrame / counterAttack.FramesPerSecond + AnimationController.AttackTransitionDuration);
        return duration;
    }

    /// <summary>
    /// 반동의 최소 대기와 동작 진행도를 만족하면 행동 복귀
    /// </summary>
    private void Rebounding()
    {
        reboundTimer += Time.deltaTime;

        if (reboundTimer >= ReboundDuration
            && enemy.AnimationController.TryGetAttackProgress(
            AnimHash.AttackRebound,
            out _,
            out float reboundProgress)
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
        EnemyAttackContact finalContact = !hasAcceptedContact ? EnemyAttackContact.Miss
            : lastContactDefense == DefenseType.Parry ? EnemyAttackContact.Deflected
            : lastContactDefense == DefenseType.NormalGuard ? EnemyAttackContact.Guarded : EnemyAttackContact.Hit;
        enemy.AIController.FinishAttack(
            finishedRecovery,
            currentAttack,
            comboAttackCount,
            isRebounding,
            enemy.Sense.CurrentTargetInfo,
            finalContact);
        currentAttack = null;
        nextComboAttack = null;
        currentPhase = AttackPhase.WindUp;
        isRebounding = false;
        isCounterOpening = false;
        finishedRecovery = false;
        hasAcceptedContact = false;
        hasParriedContact = false;
        lastContactDefense = DefenseType.None;
        lastContactTime = float.NegativeInfinity;
        recoveryStartTime = 0f;
        hasRecordedRecovery = false;
        comboAttackCount = 0;
        playedLandingImpact = false;
    }
}
