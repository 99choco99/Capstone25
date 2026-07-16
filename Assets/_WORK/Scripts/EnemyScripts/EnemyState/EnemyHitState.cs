using UnityEngine;

public class EnemyHitState : EnemyState
{

    private const float knockBackDuration = 0.2f;

    private DamageEvent currentHitData;
    private Vector3 knockbackDir;
    private float knockbackForce;

    private float stateTimer = 0f;
    private float hitAnimationLength = 0f;


    public EnemyHitState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public void SetHitData(DamageEvent damageEvent)
    {
        knockbackDir = damageEvent.hitDirection;
        knockbackForce = damageEvent.currentKnockbackForce;
    }

    public override void Enter()
    {
        stateTimer = 0f;
        enemy.Motor.Stop();

        //애니메이션 불러오기

        int animHash = enemy.Combat.EvaluateHitReaction(ref currentHitData);
        if(animHash != 0)
        {
            hitAnimationLength = enemy.AnimationController.GetAnimationLength(animHash);
            enemy.AnimationController.PlayAction(animHash);
        }
    }

    public override void Update()
    {
        //넉백
        stateTimer += Time.deltaTime;


        if(stateTimer <= knockBackDuration)
        {
            float progress = stateTimer / knockBackDuration;
            progress = Mathf.Clamp01(progress);
            float deceleration = (1f - progress) * (1f - progress);
            Vector3 velocity = knockbackDir * (knockbackForce * deceleration);
            enemy.Motor.ApplyForce(velocity);
        }
        if (stateTimer >= hitAnimationLength)
        {
            stateMachine.TransitionTo(stateMachine.EnemyGroundedState);
        }
    }

    public override void Exit() {
        
    }
}
