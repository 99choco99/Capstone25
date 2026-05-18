using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    Player player;

    public event Action OnConversationStart;  //대화가 시작됨을 알림
    public event Action OnConversationEnd;    //대화가 끝났음을 알림
    public event Action<DialogueLine> OnShowLine;//


    private void Awake()
    {
        Player.OnLocalPlayerSpawned += Init;
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= Init;
    }
    public void Init(Transform playerTransform)
    {
        Player localPlayer = playerTransform.GetComponent<Player>();
        if (localPlayer != null)
        {
            player = localPlayer;
        }
    }

    public void StartConversation(string dialogueKey)
    {
        
    }

    public void ShowNextLine()
    {

    }

    private void EndConversation()
    {

        player.InputHandler.UseInteractionInput();
        OnConversationEnd?.Invoke();
    }

    private void HandleConversationStart()
    {
        player.StateMachine.TransitionTo(player.StateMachine.ConversationState);
    }

    private void HandleConversationEnd()
    {
        if (player.StateMachine.CurrentState is ConversationState)
        {
            player.StateMachine.TransitionTo(player.StateMachine.PlayerGroundedState);
        }
    }
}
