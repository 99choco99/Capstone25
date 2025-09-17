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
        if(interactionInfo == null) { return; }
        if(DialogueData.TryGetValue(interactionInfo.DialogueKey, out List<DialogueLine> lines))
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
        var newInteraction = QuestManager.Instance.GetQuestInteractionForNpc(currentInteraction.NpcId);

        if (newInteraction != null)
        {
            switch (newInteraction.Type)
            {
                case QuestInteractionType.Start:
                    // [수정!] newInteraction의 올바른 QuestId를 사용합니다.
                    QuestManager.Instance.StartQuest(newInteraction.QuestId);
                    break;
                case QuestInteractionType.Complete:
                    QuestManager.Instance.TurnInQuest(newInteraction.QuestId);
                    break;
                case QuestInteractionType.Talk:
                    QuestManager.Instance.ReportTalkToNPC(newInteraction.NpcId);
                    break;
            }
        }

        // 2. 이어서 다른 대화를 보여줄지 결정하는 로직
        // 현재 대화(currentInteraction)와 새로 발견된 대화(newInteraction)가 다르고,
        // 새로 발견된 대화가 단순 정보성 대화(None)가 아닐 경우에만 대화를 이어갑니다.
        if (newInteraction != null && newInteraction.DialogueKey != currentInteraction.DialogueKey && newInteraction.Type != QuestInteractionType.None)
        {
            // 새로 발견된 퀘스트 대화를 바로 이어서 시작
            StartConversation(newInteraction);
        }
        else
        {
            // 이어갈 대화가 없으면 대화창을 닫습니다.
            currentInteraction = null;
            OnConversationEnd?.Invoke();
        }
    }
}
