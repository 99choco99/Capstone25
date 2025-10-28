using UnityEditor;
using UnityEngine;

public class PlayerDeadState : State
{
    public PlayerDeadState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }


    public override void Enter()
    {
        player.Motor.canMove = false;
        player.Motor.canRotate = false;
    }

}
