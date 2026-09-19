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




    private void BeginHitReaction()
    {
        player.Motor.SetMovement(Vector3.zero);
        player.Combat.ForceResetAttackState();

        KnockbackSpec knockback = KnockBackPolicy.DefenderKnockBack(currentHitData);

        player.Motor.StartKnockback(currentHitData.HitDirection, knockback);

        int targetAnim = DecideHitReaction(currentHitData);
        if (targetAnim != 0)
        {
            player.AnimatorController.PlayReaction(targetAnim, 0.05f);
        }
    }

    /// <summary>
    /// 피격 시 애니메이션 결정
    /// </summary>
    private int DecideHitReaction(in DamageResult result)
    {
        if (result.DefenseType == DefenseType.Parry) return AnimHash.Parry;
        if (result.DefenseType == DefenseType.NormalGuard) return AnimHash.GuardHit;

        float hitAngle = Vector3.SignedAngle(player.transform.forward, result.HitDirection, Vector3.up);

        if (Mathf.Abs(hitAngle) <= 45f)
            return Random.Range(0, 2) == 0 ? AnimHash.BackHit1 : AnimHash.BackHit2;

        if (hitAngle > 45f && hitAngle <= 135f)
            return AnimHash.HitLeft;

        if (hitAngle >= -135f && hitAngle < -45f)
            return AnimHash.HitRight;

        return AnimHash.HitFront;
    }
}
