using UnityEngine;

public class PlayerHitState : State
{
    public override bool UseRootMotion => false;
    public PlayerHitState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Motor.StopMovement();

        int targetAnim = stateMachine.RequestedHitAnimHash;
        player.AnimatorController.PlayAction(targetAnim);

        stateMachine.RequestedHitAnimHash = targetAnim;
    }

    public override void OnAnimationEnd()
    {
        stateMachine.TransitionTo(stateMachine.PlayerGroundedState);
    }
    public override void Exit() { }
}
