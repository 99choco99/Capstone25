using UnityEngine;

public class EnemyGuardState : EnemyState
{
    private float GuardDuration => CombatSettings.Current.EnemyGuardDuration;
    private float GuardHoldPadding => CombatSettings.Current.EnemyGuardHoldPadding;
    private float GuardHitReactionDuration => enemy.Stats.Data.attackPolicy != null
        ? enemy.Stats.Data.attackPolicy.guardReactionDuration : CombatSettings.Current.GuardRecoveryDuration;
    private float ParryReactionDuration => enemy.Stats.Data.attackPolicy != null
        ? enemy.Stats.Data.attackPolicy.parryReactionDuration : CombatSettings.Current.EnemyParryReactionDuration;

    private int lastAttackVersion;
    private float guardHoldTime;            //가드 유지 시간
    private float guardAnimTime;            //가드 애니메이션 시간. 즉, 최소 가드 유지 시간
    private float pendingDefenseStartTime;  //가드 판정 시작시간

    private DefenseType pendingDefense;
    private bool hasPendingDefense;
    private bool isGuardHitReaction;
    private bool isWaitingForAttackContinuation;
    private bool recoveringFromHit;
    private bool isRaisingGuard;

    public EnemyGuardState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }
    public override PostureRecoveryMode PostureRecoveryMode => isRaisingGuard || isGuardHitReaction && Time.time < guardAnimTime
        ? PostureRecoveryMode.Disabled : PostureRecoveryMode.Normal;

    public void PrepareHitRecovery()
    {
        recoveringFromHit = true;
    }

    public override void Enter()
    {
        isRaisingGuard = recoveringFromHit;
        recoveringFromHit = false;
        isGuardHitReaction = false;
        hasPendingDefense = false;
        isWaitingForAttackContinuation = isRaisingGuard;
        enemy.Motor.Stop();

        enemy.AnimationController.PlayReaction(AnimHash.Guard, AnimationController.GuardTransitionDuration);

        EnemyAttackObserver observer = enemy.AttackObserver;
        lastAttackVersion = observer.curAttackVersion;

        guardAnimTime = Time.time + GuardDuration;
        guardHoldTime = guardAnimTime;

        if (isRaisingGuard)
        {
            enemy.Combat.ClearDefense();
            pendingDefenseStartTime = Time.time + Mathf.Max(
                AnimationController.GuardTransitionDuration,
                enemy.Stats.Data.hitGuardRecoveryDuration);
            guardHoldTime = Mathf.Max(guardHoldTime, pendingDefenseStartTime + GuardHoldPadding);
        }

        if (observer.IsPlayerAttacking)
            guardHoldTime = Mathf.Max(guardHoldTime, observer.ExpectedActiveTime + GuardHoldPadding);
    }

    public override void Update()
    {
        EnemyAttackObserver observer = enemy.AttackObserver;

        bool hasCurrentThreat = observer.IsAttackInRange();

        if (isRaisingGuard && Time.time >= pendingDefenseStartTime)
        {
            isRaisingGuard = false;
            enemy.Combat.SetDefense(DefenseType.NormalGuard);
            lastAttackVersion = observer.curAttackVersion;
            guardAnimTime = Mathf.Max(guardAnimTime, Time.time + GuardHoldPadding);
            guardHoldTime = Mathf.Max(guardHoldTime, guardAnimTime);
        }

        //가드 준비
        if (!isRaisingGuard
            && !enemy.AIController.canCounter
            && observer.curAttackVersion != lastAttackVersion
            && hasCurrentThreat)
        {
            lastAttackVersion = observer.curAttackVersion;
            pendingDefense = enemy.AIController.GetDefenseDecision(lastAttackVersion);
            hasPendingDefense = true;
            pendingDefenseStartTime = Mathf.Max(observer.DefenseStartTime, enemy.AIController.DefenseDecisionReadyTime);
            guardHoldTime = Mathf.Max(guardHoldTime, observer.ExpectedActiveTime + GuardHoldPadding);
        }

        //플레이어가 공격을 멈췄을 때
        if (!hasCurrentThreat)
        {
            if (hasPendingDefense)
                lastAttackVersion = -1;

            hasPendingDefense = false;
            if (!isRaisingGuard)
                guardHoldTime = guardAnimTime;
        }

        //가드판정 on
        if (hasPendingDefense && Time.time >= pendingDefenseStartTime)
        {
            hasPendingDefense = false;

            if (pendingDefense == DefenseType.None)
            {
                enemy.Combat.ClearDefense();
                // 방어 실패는 피격될 빈틈입니다. 진행 중인 연타 사이에 공격으로 빠져나가지 않습니다.
                isWaitingForAttackContinuation = true;
                return;
            }

            enemy.Combat.SetDefense(pendingDefense);
            if (!enemy.AnimationController.IsPlayingGuardReaction)
            {
                isGuardHitReaction = false;
                enemy.AnimationController.PlayReaction(AnimHash.Guard, AnimationController.GuardTransitionDuration);
                guardAnimTime = Time.time + GuardDuration;
            }
            guardHoldTime = Mathf.Max(guardHoldTime, guardAnimTime);
        }

        EnemyTargetInfo perception = enemy.Sense.CurrentTargetInfo;
        if (perception.HasTarget)
            enemy.Motor.RotateTowards(perception.TargetPosition - enemy.transform.position);

        bool hasAttackContinuation = !enemy.AIController.canCounter
            && (hasCurrentThreat || isWaitingForAttackContinuation && observer.IsAttackContinuationInRange());
        if (hasAttackContinuation
            && isGuardHitReaction
            && Time.time >= guardAnimTime
            && !enemy.AnimationController.IsPlayingGuardReaction)
        {
            isGuardHitReaction = false;
            enemy.AnimationController.PlayReaction(AnimHash.Guard, AnimationController.GuardTransitionDuration);
        }

        if (!isRaisingGuard && !hasPendingDefense && !hasAttackContinuation && Time.time >= guardHoldTime)
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
    }

    /// <summary>
    /// 가드상태에서 맞았을 때
    /// </summary>
    public override void OnHit(in DamageResult result)
    {
        // 가드를 올리는 짧은 동작 중에도 피해는 받되, 정면의 일반 연타로 준비 시간이 초기화되지는 않습니다.
        if (isRaisingGuard && result.DefenseType == DefenseType.None && result.Request.CanGuard
            && result.Request.KnockBackLevel != KnockBackLevel.Heavy
            && DefenseResolver.IsAttackFromFront(
                enemy.transform.forward,
                result.HitDirection,
                enemy.Stats.Data.guardAngle))
            return;

        isGuardHitReaction = result.DefenseType != DefenseType.None;
        isWaitingForAttackContinuation = result.DefenseType == DefenseType.NormalGuard
            || result.DefenseType == DefenseType.Parry && result.Request.DeflectResponse == DeflectResponse.Continue;
        if (result.DefenseType == DefenseType.Parry)
        {
            hasPendingDefense = false;
            enemy.AnimationController.PlayReaction(AnimHash.Parry, AnimationController.GuardTransitionDuration);
            enemy.Combat.ClearDefense();
            enemy.AIController.canCounter = false;
            if (result.Request.DeflectResponse == DeflectResponse.Rebound)
                enemy.AIController.DecideCounterAttack();
            guardAnimTime = Time.time + ParryReactionDuration;
            guardHoldTime = guardAnimTime;
            return;
        }
        if (result.DefenseType == DefenseType.None)
        {
            base.OnHit(result);
            return;
        }

        int guardAnim = AnimationController.GetGuardReactionHash(result);
        enemy.AnimationController.PlayReaction(guardAnim, AnimationController.GuardTransitionDuration);
        hasPendingDefense = false;
        enemy.Combat.ClearDefense();
        guardAnimTime = Time.time + GuardHitReactionDuration;
        guardHoldTime = guardAnimTime;
    }

    public override void Exit()
    {
        recoveringFromHit = false;
        isRaisingGuard = false;
        isGuardHitReaction = false;
        isWaitingForAttackContinuation = false;
        enemy.Combat.ClearDefense();
    }
}
