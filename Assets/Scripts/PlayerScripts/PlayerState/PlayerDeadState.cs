using UnityEditor;
using UnityEngine;

public class PlayerDeadState : State
{
    public PlayerDeadState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }


    public override void Enter()
    {
        player.Motor.CanMove = false;
        player.Motor.CanRotate = false;
        player.Combat.OnAnimationPlayerAttackEnd();
    }

}
