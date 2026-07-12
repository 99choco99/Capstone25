using UnityEngine;

public class EnemyAttackState : EnemyState
{
    private AttackData currentAttackData;
    private AttackPhase currentPhase;
    private float stateTimer;

    public override bool UseRootMotion => true;

    public EnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }


    public override void Enter()
    {
        enemy.Motor.Stop();

        currentAttackData = stateMachine.RequestedAttackData;
        stateMachine.RequestedAttackData = null;

        ExecuteAttack();
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        if(currentPhase == AttackPhase.WindUp && enemy.Sense.CurrentTarget != null)
        {
            Vector3 dir = (enemy.Sense.CurrentTarget.position - enemy.transform.position).normalized;
            enemy.Motor.RotationToDirect(dir);
        }

        float duration = currentAttackData.DurationInSeconds > 0f ? currentAttackData.DurationInSeconds : 1f;
        float nTime = stateTimer / duration;

        if (currentPhase == AttackPhase.WindUp && nTime >= currentAttackData.ActiveStartTime)
        {
            currentPhase = AttackPhase.Active;
            enemy.Combat.EnemyAttackStart();
        }
        else if (currentPhase == AttackPhase.Active && nTime >= currentAttackData.RecoveryStartTime) {
            currentPhase = AttackPhase.Recovery;
            enemy.Combat.EnemyAttackEnd();
        }
        else if (currentPhase == AttackPhase.Recovery && nTime >= 1.0f) { 
            if(currentAttackData.NextComboAttack != null)
            {
                currentAttackData = currentAttackData.NextComboAttack;
                ExecuteAttack();
            }
            else
            {
                stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
            }
        }
        

    }


    private void ExecuteAttack()
    {
        stateTimer = 0f;
        currentPhase = AttackPhase.WindUp;
        enemy.AnimationController.PlayAction(currentAttackData.AnimationHash);
        enemy.Combat.SetCurrentAttackData(currentAttackData);
    }

    public override void Exit() {
        enemy.Combat.ForceResetAttackState();
        enemy.AIController.OnAttackFinished();
        currentPhase = AttackPhase.WindUp;

    }
}
