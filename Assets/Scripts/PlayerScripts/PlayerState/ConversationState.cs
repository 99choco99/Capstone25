using DG.Tweening;
using System;
using UnityEngine;

public class ConversationState : State
{
    public ConversationState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Motor.StopMovement();
        player.Motor.canMove = false;
        player.Motor.canRotate = false;
    }

    public override void Update()
    {
        if (player.InputHandler.InteractionInput)
        {
            player.InputHandler.UseInteractionInput();
            player.Dialogue.ShowNextLine();
        }
    }
    public override void Exit() {
        player.Motor.canMove = true;
        player.Motor.canRotate = true;
    }

}
