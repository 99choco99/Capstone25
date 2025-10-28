using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DialogueLine
{
    public string speakerName; // 화자 이름
    public string sentence;    // 대사 내용
}

public class Dialogue
{
    public string dialogueID;        //대화 ID
    public List<DialogueLine> lines; //대화 내용들
}

public class DialogueManager : MonoBehaviour
{
    Player player;

    public event Action OnConversationStart;
    public event Action OnConversationEnd;
    public event Action<DialogueLine> OnShowLine;

    Dictionary<string, List<DialogueLine>> DialogueData = new();
    Queue<DialogueLine> currentDialogueQueue = new();

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        PublicAPIManager.Instance.Dialogue.OnGetDialogue += GenerateData;
        PublicAPIManager.Instance.Dialogue.RequestGetDialogue();
    }

    private void OnDestroy()
    {
        if(PublicAPIManager.Instance != null)
        {
            PublicAPIManager.Instance.Dialogue.OnGetDialogue -= GenerateData;
        }

    }

    void GenerateData(Dialogue[] data)
    {
        DialogueData.Clear();
        foreach (var s in data)
        {
            DialogueData.Add(s.dialogueID, s.lines);
        }
    }

    public void StartConversation(QuestInteractionInfo interactionInfo)
    {
        if (interactionInfo != null)
        {
            switch (interactionInfo.Type)
            {
                case QuestInteractionType.Start:
                    player.Quest.StartQuest(interactionInfo.QuestId);
                    break;
                case QuestInteractionType.Complete:
                    player.Quest.TurnInQuest(interactionInfo.QuestId);
                    break;
                case QuestInteractionType.Talk:
                    player.Quest.ReportTalkToNPC(interactionInfo.NpcId);
                    break;
            }
        }
        else
        {
            return;
        }
        if (DialogueData.TryGetValue(interactionInfo.DialogueKey, out List<DialogueLine> lines))
        {
            currentDialogueQueue.Clear();
            foreach (var dialog in lines)
            {
                currentDialogueQueue.Enqueue(dialog);
            }
            OnConversationStart?.Invoke();
            ShowNextLine();
        }

    }

    public void ShowNextLine()
    {
        if(currentDialogueQueue.TryDequeue(out var line))
        {
            OnShowLine?.Invoke(line);
        }
        else
        {
            EndConversation();
        }
    }

    private void EndConversation()
    {
        player.InputHandler.UseInteractionInput();
        OnConversationEnd?.Invoke();
    }
}
