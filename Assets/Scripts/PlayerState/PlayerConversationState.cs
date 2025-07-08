using DG.Tweening;
using System;
using UnityEngine;

public class PlayerConversationState : IState
{
    bool isAction;

    private readonly PlayerController player;
    private CameraMovement cameraMovement;
    private PlayerUIManager playerUI;
    private DialogueManager dialogueManager;

    public PlayerConversationState(PlayerController player) { this.player = player; }

    public void Enter()
    {
        isAction = false;
        player.toggleCameraRotation = true;
        player.playerInteraction.interactRange = 0;
        player.anim.SetFloat("xDir",0);
        player.anim.SetFloat("zDir", 0);
        player.interaction = false; // 바로 다음으로 넘어가는거 방지

        cameraMovement = player.playerCamera.GetComponentInParent<CameraMovement>();
        playerUI = player.GetComponentInChildren<PlayerUIManager>();
        dialogueManager = player.GetComponentInChildren<DialogueManager>();

        cameraMovement.maxDistance = cameraMovement.minDistance;
        playerUI.ShowDialogUI();
    }
    public void Update()
    {
        Vector3 midPosition = (player.playerInteraction.select.transform.position + player.transform.position) / 2f + new Vector3(0, 1.3f, 0);
        cameraMovement.objectToFollow.transform.position = Vector3.Lerp(cameraMovement.objectToFollow.transform.position, midPosition, 2* Time.deltaTime);
        if (player.interaction)
        {
            bool isEnd = dialogueManager.NextDialog();
            if (isEnd)
            {
                cameraMovement.objectToFollow.transform.DOMove(player.transform.position + new Vector3(0, 1.3f, 0), 0.5f).OnComplete(() =>
                {
                    cameraMovement.maxDistance = cameraMovement.RevertDistance;
                    //player.playerStateMachine.TransitionTo(player.playerStateMachine.playerMoveState);
                });
            }
            player.interaction = false;
        }
    }
    public void Exit()
    {
        playerUI.HideDialogUI();
        player.toggleCameraRotation = false;
        player.playerInteraction.interactRange = 3;
    }
}
