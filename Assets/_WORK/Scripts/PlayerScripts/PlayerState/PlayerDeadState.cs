using UnityEngine;

public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Motor.SetMovement(Vector3.zero);
        player.Combat.ForceResetAttackState();
        player.AnimatorController.PlayAction(AnimHash.Death);
    }

}
