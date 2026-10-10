using UnityEngine;

public class EnemyGuardState : EnemyState
{
    private float GuardDuration => CombatSettings.Current.EnemyGuardDuration;
    private float GuardHoldPadding => CombatSettings.Current.EnemyGuardHoldPadding;
    private float GuardHitReactionDuration => CombatSettings.Current.GuardRecoveryDuration;
    private float ParryReactionDuration => CombatSettings.Current.EnemyParryReactionDuration;

    private int lastAttackVersion;
    private float guardHoldTime;            //가드 유지 시간
    private float guardAnimTime;            //가드 애니메이션 시간. 즉, 최소 가드 유지 시간
    private float pendingDefenseStartTime;  //가드 판정 시작시간

    private DefenseType pendingDefense;
    private bool hasPendingDefense;
    private bool isGuardHitReaction;
    private bool isWaitingForAttackContinuation;

    public EnemyGuardState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }
    public override PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.Normal;

    public override void Enter()
    {
        isGuardHitReaction = false;
        hasPendingDefense = false;
        isWaitingForAttackContinuation = false;
        enemy.Motor.Stop();

        enemy.AnimationController.PlayReaction(AnimHash.Guard, AnimationController.GuardTransitionDuration);

        EnemyAttackObserver observer = enemy.AttackObserver;
        lastAttackVersion = observer.curAttackVersion;

        guardAnimTime = Time.time + GuardDuration;
        guardHoldTime = guardAnimTime;

        if (observer.IsPlayerAttacking)
            guardHoldTime = Mathf.Max(guardHoldTime, observer.ExpectedActiveTime + GuardHoldPadding);
    }

    public override void Update()
    {
        EnemyAttackObserver observer = enemy.AttackObserver;

        bool hasCurrentThreat = observer.IsAttackInRange();

        //가드 준비
        if (!enemy.AIController.canCounter && observer.curAttackVersion != lastAttackVersion && hasCurrentThreat)
        {
            lastAttackVersion = observer.curAttackVersion;
            pendingDefense = enemy.AIController.GetDefenseDecision(lastAttackVersion);
            hasPendingDefense = true;
            pendingDefenseStartTime = observer.DefenseStartTime;
            guardHoldTime = Mathf.Max(guardHoldTime, observer.ExpectedActiveTime + GuardHoldPadding);
        }

        //플레이어가 공격을 멈췄을 때
        if (!hasCurrentThreat)
        {
            if (hasPendingDefense)
                lastAttackVersion = -1;

            hasPendingDefense = false;
            guardHoldTime = guardAnimTime;
        }

        //가드판정 on
        if (hasPendingDefense && Time.time >= pendingDefenseStartTime)
        {
            hasPendingDefense = false;

            if (pendingDefense == DefenseType.None)
            {
                enemy.Combat.ClearDefense();
                stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
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

        bool hasAttackContinuation = isWaitingForAttackContinuation && !enemy.AIController.canCounter
            && (hasCurrentThreat || observer.IsAttackContinuationInRange());
        if (hasAttackContinuation && isGuardHitReaction && Time.time >= guardAnimTime && !enemy.AnimationController.IsPlayingGuardReaction)
        {
            isGuardHitReaction = false;
            enemy.AnimationController.PlayReaction(AnimHash.Guard, AnimationController.GuardTransitionDuration);
        }

        if (!hasPendingDefense && !hasAttackContinuation && Time.time >= guardHoldTime)
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
    }

    /// <summary>
    /// 가드상태에서 맞았을 때
    /// </summary>
    public override void OnHit(in DamageResult result)
    {
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
        isGuardHitReaction = false;
        isWaitingForAttackContinuation = false;
        enemy.Combat.ClearDefense();
    }
}
