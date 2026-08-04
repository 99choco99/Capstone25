using UnityEngine;

public class EnemyGuardState : EnemyState
{
    private float guardDuration;
    private float stateTimer;

    public EnemyGuardState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        stateTimer = 0f;
        guardDuration = Random.Range(1.5f, 3.0f);

        enemy.Motor.Stop();
        enemy.AnimationController.PlayAction(AnimHash.Guard);
        enemy.Combat.CurrentDefenseType = DefenseType.NormalGuard;
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        if (enemy.Sense.CurrentTarget != null)
        {
            Vector3 dir = (enemy.Sense.CurrentTarget.position - enemy.transform.position).normalized;
            enemy.Motor.RotationToDirect(dir);
        }

        if (stateTimer >= guardDuration)
        {
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
        }
    }

    public override void Exit()
    {
        enemy.Combat.CurrentDefenseType = DefenseType.None;
    }
}