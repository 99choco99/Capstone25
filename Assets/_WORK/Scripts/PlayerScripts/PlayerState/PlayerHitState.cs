using UnityEngine;

public class PlayerHitState : PlayerState
{
    private const float HitRecoveryDuration = 0.5f;

    public override bool UseRootMotion => false;
    public PlayerHitState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private DamageResult currentHitData;

    private float stateTimer = 0f;


    public void SetHitData(DamageResult data)
    {
        currentHitData = data;
        stateTimer = 0f;
    }

    public void RestartHit(DamageResult data)
    {
        SetHitData(data);
        BeginHitReaction();
    }

    public override void Enter()
    {
        BeginHitReaction();
    }

    private void BeginHitReaction()
    {
        player.Motor.SetMovement(Vector3.zero);
        player.Combat.ForceResetAttackState();

        // 공격 등급과 방어 결과를 한 번 해석해 최종 거리·시간을 Motor에 전달합니다.
        KnockbackSpec knockback = KnockBackPolicy.DefenderKnockBack(currentHitData);

        player.Motor.StartKnockback(
            currentHitData.HitDirection,
            knockback);

        int targetAnim = player.Combat.DecideHitReaction(currentHitData);
        if (targetAnim != 0)
        {
            player.AnimatorController.PlayReaction(targetAnim, 0.05f);
        }
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer > HitRecoveryDuration)
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
        player.Motor.StopKnockback();
    }
}
