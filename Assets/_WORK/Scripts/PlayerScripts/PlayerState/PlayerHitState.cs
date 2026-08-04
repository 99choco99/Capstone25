using UnityEngine;

public class PlayerHitState : PlayerState
{
    private const float knockBackDuration = 0.2f;

    public override bool UseRootMotion => false;
    public PlayerHitState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private DamageEvent currentHitData;
    private Vector3 knockbackDir;

    private float stateTimer = 0f;
    private float hitAnimTime = 0f;

    public void SetHitData(DamageEvent data)
    {
        currentHitData = data;
        stateTimer = 0f;
        hitAnimTime = 0f;
        knockbackDir = data.hitDirection * data.currentKnockbackForce;
    }

    public override void Enter()
    {
        player.Motor.SetMovement(Vector3.zero);
        player.Combat.ForceResetAttackState();

        int targetAnim = player.Combat.EvaluateHitReaction(ref currentHitData);
        if (targetAnim != 0)
        {
            hitAnimTime = player.AnimatorController.GetAnimationLength(targetAnim);
            player.AnimatorController.PlayAction(targetAnim);
        }
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer <= knockBackDuration)
        {
            // 시간에 따른 감속 (Easing Out)
            float progress = (stateTimer / knockBackDuration);
            progress = Mathf.Clamp01(progress);
            float deceleration = (1f-progress) * (1f-progress);
            player.Motor.SetKnockbackVelocity(knockbackDir * deceleration);
        }
        else
        {
            player.Motor.SetKnockbackVelocity(Vector3.zero);
        }

        if (stateTimer > hitAnimTime)
        {
            if (player.InputHandler.GuardInput)
            {
                stateMachine.TransitionTo(stateMachine.PlayerGuardState);
            }
            else
            {
                stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
            }
        }
    }

    public override void Exit()
    {
        currentHitData = default;
        stateTimer = 0f;
        hitAnimTime = 0f;
        player.Motor.SetKnockbackVelocity(Vector3.zero);
    }
}
