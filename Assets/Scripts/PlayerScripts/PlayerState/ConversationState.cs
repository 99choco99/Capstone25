using DG.Tweening;
using System;
using UnityEngine;

public class ConversationState : State
{
    public ConversationState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Motor.SetTargetVelocity(0);
        player.AnimatorController.UpdateLocomotion(0, 0);

        player.Interaction.enabled = false;
        player.Stats.IsInvincible = true;
    }

    public override void Update()
    {
        if (player.InputHandler.InteractionInput)
        {
            player.InputHandler.UseInteractionInput();
            //Dialogue.ShowNextLine();
        }
    }
    public override void Exit() {
        player.Interaction.enabled = true;
        player.Stats.IsInvincible = false;
    }

}
