using UniversalGraph;

using System;
using UnityEngine;

public class ConversationState : PlayerState
{
    public ConversationState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Motor.SetMovement(Vector3.zero);
        player.AnimatorController.ForceStopLocomotion();
        player.AnimatorController.PlayAction(AnimHash.Locomotion);

        player.InputHandler.OnInteractionPressed += OnNextLinePressed;

        player.Interaction.IsDetectionPaused = true;

        player.Interaction.ClearInteraction();
        player.SetInvincible(true);
    }

    public void OnNextLinePressed()
    {
        DialogueManager.Instance.ContinueNextLine();
    }


    public override void Exit() {
        player.InputHandler.OnInteractionPressed -= OnNextLinePressed;
        player.SetInvincible(false);
        player.Interaction.IsDetectionPaused = false;
    }

}


