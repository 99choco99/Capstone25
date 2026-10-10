using UnityEngine;

/// <summary>
/// EnemyAIController가 무엇을 할지 정하고 여기서 어떻게 실행할지 담당
/// </summary>
public class EnemyGroundedState : EnemyState
{
    private EnemyIntentType preIntentType;
    private bool isKeepingReaction;

    public EnemyGroundedState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }
    public override PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.Normal;

    public override void Enter()
    {
        preIntentType = EnemyIntentType.HoldPosition;
        enemy.Motor.Stop();
        isKeepingReaction = enemy.AnimationController.IsPlayingReaction;
        if (!isKeepingReaction)
            enemy.AnimationController.PlayAction(AnimHash.Locomotion);
    }

    public override void Update()
    {
        EnemyTargetInfo perception = enemy.Sense.CurrentTargetInfo;
        EnemyIntent intent = enemy.AIController.SelectIntent(perception, enemy.Motor.HasPatrolPoints, !enemy.AnimationController.IsPlayingReaction);

        if (ExecuteIntent(intent)) return;

        if (intent.Type == EnemyIntentType.HoldPosition && perception.HasTarget)
            enemy.Motor.RotateTowards(perception.TargetPosition - enemy.transform.position);

        Vector2 currentVelocity = enemy.Motor.GetNormalizedVelocity();
        float vertical = intent.Type == EnemyIntentType.Chase && currentVelocity.y > 0f ? currentVelocity.y * 2f : currentVelocity.y;
        enemy.AnimationController.UpdateLocomotion(currentVelocity.x, vertical, false);
    }

    /// <returns>이번 의도가 다른 행동 State로 전환되었다면 true. </returns>
    private bool ExecuteIntent(in EnemyIntent intent)
    {
        switch (intent.Type)
        {
            case EnemyIntentType.HoldPosition:
                // 연속 stop 방지
                if (preIntentType != EnemyIntentType.HoldPosition)
                    enemy.Motor.Stop();
                if (!enemy.AnimationController.IsPlayingReaction)
                    ResumeLocomotion();
                break;

            case EnemyIntentType.Patrol:
                ResumeLocomotion();
                enemy.Motor.Patrol(preIntentType != EnemyIntentType.Patrol);
                break;

            case EnemyIntentType.Chase:
                ResumeLocomotion();
                enemy.Motor.Chase(intent.TargetPosition, enemy.AIController.ChaseStoppingDistance);
                break;

            case EnemyIntentType.Space:
            case EnemyIntentType.Strafe:
                ResumeLocomotion();
                if (preIntentType != intent.Type)
                    enemy.Motor.Stop();
                enemy.Motor.Strafe(intent.TargetPosition, enemy.AIController.StrafeDistance, intent.Type == EnemyIntentType.Strafe);
                break;

            case EnemyIntentType.Attack:
                if (intent.AttackData == null)
                {
                    preIntentType = EnemyIntentType.HoldPosition;
                    return false;
                }

                stateMachine.RequestAttack(intent.AttackData);
                stateMachine.TransitionTo(stateMachine.EnemyAttackState);
                return true;

            case EnemyIntentType.Guard:
                enemy.Combat.SetDefense(intent.Defense);
                stateMachine.TransitionTo(stateMachine.EnemyGuardState);
                return true;

            default:
                return false;
        }

        preIntentType = intent.Type;
        return false;
    }

    private void ResumeLocomotion()
    {
        if (!isKeepingReaction) return;

        isKeepingReaction = false;
        enemy.AnimationController.PlayAction(AnimHash.Locomotion);
    }

    public override void Exit()
    {
        enemy.Motor.Stop();
        enemy.AnimationController.ForceStopLocomotion();
    }
}
