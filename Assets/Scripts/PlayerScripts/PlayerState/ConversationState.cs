using DG.Tweening;
using System;
using UnityEngine;

public class ConversationState : State
{
    public ConversationState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }
    [SerializeField] private DialogueManager Dialogue;

    public override void Enter()
    {
        player.Motor.StopMovement();
        player.Motor.CanMove = false;
        player.Motor.CanRotate = false;
    }

    public override void Update()
    {
        if (player.InputHandler.InteractionInput)
        {
            player.InputHandler.UseInteractionInput();
            Dialogue.ShowNextLine();
        }
    }
    public override void Exit() {
        player.Motor.CanMove = true;
        player.Motor.CanRotate = true;
    }

}
