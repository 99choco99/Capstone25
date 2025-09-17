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
    Queue<DialogueLine> currentDialogueList = new();


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
        Debug.Log(data);
        foreach (var _ in data)
        {
            DialogueData.Add(_.dialogueID, _.lines);
        }

    }

    public void StartConversation(string dialogueKey)
    {
        if (dialogueKey == null) {
            dialogueKey = "BORGUS_DEFAULT";
        }
        Debug.Log(dialogueKey);
        if(DialogueData.TryGetValue(dialogueKey, out List<DialogueLine> lines))
        {
            foreach (var dialog in lines)
            {
                currentDialogueList.Enqueue(dialog);
            }
            NextDialog();
            OnConversationStart?.Invoke();
        }

    }

    public void NextDialog()
    {
        if(currentDialogueList.TryDequeue(out var line))
        {
            OnShowLine?.Invoke(line);
        }
        else
        {
            OnConversationEnd?.Invoke();
        }
    }
}
