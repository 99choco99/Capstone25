using UnityEngine;

/// <summary>
/// EnemyAIController가 무엇을 할지 정하고 여기서 어떻게 실행할지 담당
/// </summary>
public class EnemyGroundedState : EnemyState
{
    private EnemyIntentType preIntentType;

    public EnemyGroundedState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }
    public override PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.Normal;


    public override void Enter()
    {
        preIntentType = EnemyIntentType.HoldPosition;
        enemy.Motor.Stop();
        enemy.AnimationController.PlayAction(AnimHash.Locomotion);
    }

    public override void Update()
    {
        EnemyTargetInfo perception = enemy.Sense.CurrentTargetInfo;
        EnemyIntent intent = enemy.AIController.SelectIntent(perception);

        if (ExecuteIntent(intent)) return;

        Vector2 currentVelocity = enemy.Motor.GetNormalizedVelocity();
        enemy.AnimationController.UpdateLocomotion(currentVelocity.x, currentVelocity.y, false);
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
                preIntentType = intent.Type;
                return false;

            case EnemyIntentType.Chase:
                enemy.Motor.Chase(intent.TargetPosition, enemy.AIController.StrafeDistance);
                preIntentType = intent.Type;
                return false;

            case EnemyIntentType.Strafe:
                enemy.Motor.Strafe(intent.TargetPosition, enemy.AIController.StrafeDistance);
                preIntentType = intent.Type;
                return false;

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
    }

    public override void Exit()
    {
        enemy.Motor.Stop();
        enemy.AnimationController.ForceStopLocomotion();
    }
}
