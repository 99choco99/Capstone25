using UnityEngine;

public class PlayerDeadState : State
{
    public PlayerDeadState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }


    public override void Enter()
    {
        player.Motor.StopMovement();
        player.Combat.ForceResetAttackState();
    }

}
