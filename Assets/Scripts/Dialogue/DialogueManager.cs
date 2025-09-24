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
    public static DialogueManager instance;


    public event Action OnConversationStart;
    public event Action OnConversationEnd;
    public event Action<DialogueLine> OnShowLine;

    Dictionary<string, List<DialogueLine>> DialogueData = new();
    Queue<DialogueLine> currentDialogueQueue = new();
    private QuestInteractionInfo currentInteraction; // 현재 대화의 목적과 정보를 담는 변수

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        APIEvents.OnGetDialogue += GenerateData;
        APIManager.Instance.Dialogue.RequestGetDialogue();
    }

    private void OnDestroy()
    {
        APIEvents.OnGetDialogue -= GenerateData;
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
                    QuestManager.Instance.StartQuest(interactionInfo.QuestId);
                    break;
                case QuestInteractionType.Complete:
                    QuestManager.Instance.TurnInQuest(interactionInfo.QuestId);
                    break;
                case QuestInteractionType.Talk:
                    QuestManager.Instance.ReportTalkToNPC(currentInteraction.NpcId);
                    break;
            }
        }
        if (DialogueData.TryGetValue(interactionInfo.DialogueKey, out List<DialogueLine> lines))
        {
            currentInteraction = interactionInfo;
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

        currentInteraction = null;
        OnConversationEnd?.Invoke();

    }
}
