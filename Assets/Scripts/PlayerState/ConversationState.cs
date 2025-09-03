using DG.Tweening;
using System;
using UnityEngine;

public class ConversationState : StateMachineBehaviour
{
    NPC self;
    PlayerController player;
    CameraMovement cameraMovement;
    DialogueManager dialogueManager;
    bool isEnd = false;



    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (self == null || player == null)
        {
            self = animator.GetComponentInParent<NPC>();
            player = self.currentTalkingPlayer;
            cameraMovement = player.playerCamera.GetComponentInParent<CameraMovement>();
            dialogueManager = player.GetComponentInChildren<DialogueManager>();
        }
        isEnd = false;
        player.interactRange = 0;
        player.interaction = false; // 바로 다음으로 넘어가는거 방지
        cameraMovement.maxDistance = cameraMovement.minDistance;
        player.OpenUI(UIPanelType.Dialogue);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null || isEnd) { return; }
        Vector3 midPosition = (self.transform.position + player.transform.position) / 2f + new Vector3(0, 1.3f, 0);
        cameraMovement.objectToFollow.transform.position = Vector3.Lerp(cameraMovement.objectToFollow.transform.position, midPosition, 2 * Time.deltaTime);
        if (player.interaction)
        {
            isEnd = dialogueManager.NextDialog();
            if (isEnd)
            {
                cameraMovement.objectToFollow.transform.DOMove(player.transform.position + new Vector3(0, 1.3f, 0), 0.5f).OnComplete(() =>
                {
                    animator.SetBool("Talk", false);
                });
            }
            player.interaction = false;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player.CloseUI(UIPanelType.Dialogue);
        player.interactRange = 3;
        cameraMovement.maxDistance = cameraMovement.RevertDistance;
    }

}
