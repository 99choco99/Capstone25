using UnityEngine;

public class EnemyStunState : EnemyState
{
    private float groggyDuration = 1.2f;
    private float stateTimer = 0f;

    public EnemyStunState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        stateTimer = 0f;
        enemy.Motor.Stop();
        enemy.AnimationController.PlayAction(AnimHash.Stun); //½ºÅÏ
    }


    public override void Update()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= groggyDuration)
        {
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
        }
    }

    public override void Exit()
    {
        stateTimer = 0f;
    }
}
