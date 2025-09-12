using DG.Tweening;
using System;
using UnityEngine;

public class ConversationState : State
{
    public ConversationState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Motor.StopMovement();
    }

    public override void Update()
    {
        if (player.InputHandler.InteractionInput)
        {
            player.InputHandler.UseInteractionInput();
            DialogueManager.instance.NextDialog();
        }
    }
    public override void Exit() { }

}
