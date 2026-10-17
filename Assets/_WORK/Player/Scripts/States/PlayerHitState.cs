using UnityEngine;

public class PlayerHitState : PlayerState
{
    private float hitRecoveryDuration;
    private float guardRecoveryDuration;

    public PlayerHitState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override PostureRecoveryMode PostureRecoveryMode => PostureRecoveryMode.HitReaction;

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

        if (stateTimer > hitRecoveryDuration)
        {
            stateMachine.TransitionTo(player.InputHandler.GuardInput
                ? stateMachine.PlayerGuardState
                : stateMachine.PlayerGroundedState);
            return;
        }

        if (stateTimer < guardRecoveryDuration)
            return;

        ActionCommand command = player.InputBuffer.PeekValidCommand();
        if (!player.InputHandler.GuardInput && command != ActionCommand.Guard)
            return;
        if (command == ActionCommand.Guard)
            player.InputBuffer.ConsumeCurrentCommand();

        float remainingRecovery = hitRecoveryDuration - stateTimer;
        stateMachine.TransitionTo(stateMachine.PlayerGuardState);
        stateMachine.PlayerGuardState.SetHitRecovery(remainingRecovery);
    }

    public override void Exit()
    {
        currentHitData = default;
        stateTimer = 0f;
    }

    private void BeginHitReaction()
    {
        // 넉백 이동과 조작 불가 시간은 별개입니다. 강한 공격일수록 늦게 회복합니다.
        CombatSettings.Reaction reaction = CombatSettings.Current.GetReaction(currentHitData.Request);
        hitRecoveryDuration = reaction.HitRecoveryDuration;
        guardRecoveryDuration = Mathf.Clamp(reaction.HitGuardRecoveryDuration, 0f, hitRecoveryDuration);
        player.Motor.SetMovement(Vector3.zero);
        player.Combat.ForceResetAttackState();

        int targetAnim = DecideHitReaction(currentHitData);
        if (targetAnim != 0)
        {
            player.AnimatorController.PlayReaction(targetAnim);
        }
    }

    /// <summary>
    /// 피격 시 애니메이션 결정
    /// </summary>
    private int DecideHitReaction(in DamageResult result)
    {
        int reactionHash = AnimationController.GetHitReactionHash(player.transform.forward, result);
        if (reactionHash != AnimHash.BackHit)
            return reactionHash;
        return Random.Range(0, 2) == 0 ? AnimHash.BackHit1 : AnimHash.BackHit2;
    }
}
