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
        player = GetComponentInParent<Player>();
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
}
