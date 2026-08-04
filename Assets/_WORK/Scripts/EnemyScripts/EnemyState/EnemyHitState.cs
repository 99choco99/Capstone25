using UnityEngine;

public class EnemyHitState : EnemyState
{
    private const float hitTime = 0.5f;
    private const float knockBackDuration = 0.2f;

    private DamageEvent currentHitData;
    private Vector3 knockbackDir;
    private float knockbackForce;

    private float stateTimer = 0f;



    public EnemyHitState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public void SetHitData(DamageEvent damageEvent)
    {
        currentHitData = damageEvent;
        knockbackDir = damageEvent.hitDirection;
        knockbackForce = damageEvent.currentKnockbackForce;
    }

    public override void Enter()
    {
        stateTimer = 0f;
        enemy.Motor.Stop();

        int animHash = enemy.Combat.EvaluateHitReaction(ref currentHitData);
        if(animHash != 0)
        {
            enemy.AnimationController.PlayAction(animHash);
        }
    }

    public override void Update()
    {
        //³Ë¹é
        stateTimer += Time.deltaTime;


        if(stateTimer <= knockBackDuration)
        {
            float progress = stateTimer / knockBackDuration;
            progress = Mathf.Clamp01(progress);
            float deceleration = (1f - progress) * (1f - progress);
            Vector3 velocity = knockbackDir * (knockbackForce * deceleration);
            enemy.Motor.ApplyForce(velocity);
        }
        if (stateTimer >= hitTime)
        {
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
        }
    }

    public override void Exit() {
        stateTimer = 0f;
    }
}
