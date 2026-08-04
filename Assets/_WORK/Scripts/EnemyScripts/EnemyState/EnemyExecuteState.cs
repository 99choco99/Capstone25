using UnityEngine;

public class EnemyExecuteState : EnemyState
{
    private float groggyDuration = 7.0f;
    private float stateTimer = 0f;
    public EnemyExecuteState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }


    public override void Enter()
    {
        stateTimer = 0f;

        //움직임 멈춤
        enemy.Motor.Stop();
        enemy.AnimationController.PlayAction(AnimHash.GuardBreak);
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer > groggyDuration)
        {
            //체간회복
            //부활 애니메이션 실행.
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
        }
    }

    public override void Exit()
    {
        stateTimer = 0f;
    }
}
