using UnityEngine;

/// <summary>
/// 체간 붕괴 상태
/// </summary>
public class PlayerStunState : PlayerState
{
    public PlayerStunState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    private float stateTimer = 0f;
    private const float StunTime = 1f;

    public override void Enter()
    {
        stateTimer = 0f;
        player.Motor.SetMovement(Vector3.zero);

        player.Motor.StopKnockback();
        player.Combat.ForceResetAttackState();
        player.AnimatorController.PlayAction(AnimHash.Stun);
        
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;
        if(stateTimer >= StunTime)
        {
            stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
        }
    }

    public override void Exit()
    {
        stateTimer = 0f;
        player.Stats.ResetPosture();
    }

}
