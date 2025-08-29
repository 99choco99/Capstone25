using DG.Tweening;
using System;
using UnityEngine;

public class PlayerConversationState : StateMachineBehaviour
{

    public PlayerController player;
    public CameraMovement cameraMovement;
    public DialogueManager dialogueManager;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.GetComponent<PlayerController>();
            cameraMovement = player.playerCamera.GetComponentInParent<CameraMovement>();
            dialogueManager = animator.GetComponentInChildren<DialogueManager>();
        }
        player.interactRange = 0;
        player.interaction = false; // 바로 다음으로 넘어가는거 방지
        cameraMovement.maxDistance = cameraMovement.minDistance;
        player.OpenUI(UIPanelType.Dialogue);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Vector3 midPosition = (player.currentTalkingNPC.transform.position + player.transform.position) / 2f + new Vector3(0, 1.3f, 0);
        cameraMovement.objectToFollow.transform.position = Vector3.Lerp(cameraMovement.objectToFollow.transform.position, midPosition, 2 * Time.deltaTime);
        if (player.interaction)
        {
            bool isEnd = dialogueManager.NextDialog();
            if (isEnd)
            {
                cameraMovement.objectToFollow.transform.DOMove(player.transform.position + new Vector3(0, 1.3f, 0), 0.5f).OnComplete(() =>
                {
                    cameraMovement.maxDistance = cameraMovement.RevertDistance;
                });
            }
            player.interaction = false;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player.OpenUI(UIPanelType.Dialogue);
        player.interactRange = 3;
        animator.SetBool("Talk", false);
    }
}
