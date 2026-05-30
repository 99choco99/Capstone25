using UnityEngine;

public class EnemyGroundedState : EnemyState
{
    public EnemyGroundedState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }


    public override void Enter()
    {

    }

    public override void Update()
    {
        enemy.AIController.Tick();

        Vector2 currentVelocity = enemy.Motor.GetNormalizedVelocity();
        enemy.AnimationController.UpdateLocomotion(currentVelocity.x, currentVelocity.y, false);
    }


    public override void Exit() {
        enemy.Motor.Stop();
        enemy.AnimationController.ForceStopLocomotion();
    }



}
