using UnityEngine;

public class EnemyGuardState : EnemyState
{
    private const float GuardDuration = 0.3f;
    private const float GuardHitReactionDuration = 0.2f;
    private const float ParryReactionDuration = 0.22f;

    private int lastAttackVersion;
    private float guardHoldTime;            //가드 유지 시간
    private float guardAnimTime;            //가드 애니메이션 시간. 즉, 최소 가드 유지 시간
    private float pendingDefenseStartTime;  //가드 판정 시작시간

    private DefenseType pendingDefense;
    private bool hasPendingDefense;


    public EnemyGuardState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }
    public override PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.Normal;


    public override void Enter()
    {
        hasPendingDefense = false;
        enemy.Motor.StopKnockback();
        enemy.Motor.Stop();

        enemy.AnimationController.PlayReaction(AnimHash.Guard, 0.03f);

        EnemyAttackObserver observer = enemy.AttackObserver;
        lastAttackVersion = observer.curAttackVersion;

        guardAnimTime = Time.time + GuardDuration;
        guardHoldTime = guardAnimTime;

        if (observer != null && observer.IsPlayerAttacking)
            guardHoldTime = Mathf.Max(guardHoldTime, observer.ExpectedActiveTime + 0.1f);
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
            pendingDefenseStartTime = Mathf.Max(Time.time, observer.ExpectedActiveTime - enemy.AIController.GuardLeadTime);
            guardHoldTime = Mathf.Max(guardHoldTime, observer.ExpectedActiveTime + 0.1f);
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
            guardAnimTime = Time.time + GuardDuration;
            guardHoldTime = Mathf.Max(guardHoldTime, guardAnimTime);
        }

        EnemyTargetInfo perception = enemy.Sense.CurrentTargetInfo;
        if (perception.HasTarget)
            enemy.Motor.RotateTowards(perception.TargetPosition - enemy.transform.position);

        if (!hasPendingDefense && Time.time >= guardHoldTime)
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
    }


    /// <summary>
    /// 가드상태에서 맞았을 때
    /// </summary>
    public override void OnHit(in DamageResult result)
    {
        if (result.DefenseType == DefenseType.Parry)
        {
            enemy.AIController.canCounter = true;
            enemy.AnimationController.PlayReaction(AnimHash.Parry, 0.03f);
            enemy.Motor.StopKnockback();
            enemy.Combat.ClearDefense();
            enemy.AIController.DecideCounterAttack();
            guardAnimTime = Time.time + ParryReactionDuration;
            guardHoldTime = guardAnimTime;
            return;
        }

        enemy.AnimationController.PlayReaction(AnimHash.GuardHit, 0.03f);
        GuardKnockBack(result);
        hasPendingDefense = false;
        enemy.Combat.ClearDefense();
        guardAnimTime = Time.time + GuardHitReactionDuration;
        guardHoldTime = guardAnimTime;
    }

    private void GuardKnockBack(in DamageResult result)
    {
        KnockbackSpec knockback = KnockBackPolicy.DefenderKnockBack(result);

        enemy.Motor.StartKnockback(result.HitDirection, knockback);
    }

    public override void Exit()
    {
        enemy.Motor.StopKnockback();
        enemy.Combat.ClearDefense();
    }
}
