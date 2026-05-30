using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class DialogueManager
{
    public static DialogueManager Instance { get; private set; }

    public Dictionary<string, List<DialogueLine>> dialogueRegistry;

    private List<DialogueLine> currentLines = new List<DialogueLine>();
    private Dictionary<string, Action> actionRegistry = new();
    private int currentLineIndex = 0;

    private Action npcPendingTask;               //대화 종료 후 진행 될 함수 실행용
    public bool IsWaitingForChoice { get; private set; } = false;

    public event Action OnConversationStart;     //대화가 시작됨을 알림
    public event Action OnConversationEnd;       //대화가 끝났음을 알림
    public event Action<DialogueLine> OnShowLine;//대화창 띄우기
    public event Action<List<DialogueChoice>> OnShowChoices;

    private DialogueManager()
    {
        dialogueRegistry = new();
    }

    public static void Init(string jsonString)
    {
        if (Instance != null) return;
        Instance = new();
        Instance.LoadData(jsonString);
    }

    public void LoadData(string jsonString)
    {
        dialogueRegistry.Clear();
        var parsedData = JsonConvert.DeserializeObject<Dictionary<string, List<DialogueLine>>>(jsonString);

        if (parsedData == null) { return; }
        dialogueRegistry = parsedData;
    }

    //대화 시작
    public void StartConversation(string dialogueKey, Action onComplete)
    {
        if(dialogueRegistry.TryGetValue(dialogueKey, out var lines))
        {
            currentLines = lines;
            npcPendingTask = onComplete;
            ExecuteConversationFlow();
        }
    }

    public void StartConversation(DialogueLine dynamicLine)
    {
        currentLines = new List<DialogueLine> { dynamicLine };
        npcPendingTask = null;
        ExecuteConversationFlow();
    }

    private void ExecuteConversationFlow()
    {
        currentLineIndex = 0;
        IsWaitingForChoice = false;

        OnConversationStart?.Invoke();
        ShowNextLine();
    }


    //다음 대화로 넘기기
    public void ShowNextLine()
    {
        if (IsWaitingForChoice) { return; }

        if (currentLineIndex < currentLines.Count)
        {
            DialogueLine currentLine = currentLines[currentLineIndex];
            OnShowLine?.Invoke(currentLine);
            if (!string.IsNullOrEmpty(currentLine.eventKey))
            {
                ExecuteAction(currentLine.eventKey);
            }

            if (currentLine.choices != null && currentLine.choices.Count > 0)
            {
                IsWaitingForChoice = true;
                OnShowChoices?.Invoke(currentLine.choices);
            }
            currentLineIndex++;
        }
        else
        {
            EndConversation();
        }

    }

    //대화하는 도중 발생할 이벤트 등록
    public void RegisterAction(string actionKey, Action actionMethod)
    {
        if (string.IsNullOrEmpty(actionKey)) return;
        if (!actionRegistry.ContainsKey(actionKey))
        {
            actionRegistry.Add(actionKey, actionMethod);
        }
    }

    //등록해둔 이벤트 실행
    public void ExecuteAction(string actionKey)
    {
        if (actionRegistry.TryGetValue(actionKey, out var executeAction))
        {
            executeAction?.Invoke();
        }
    }

    //선택지를 눌렀을 때
    public void OnSelectChoice(DialogueChoice choice)
    {
        IsWaitingForChoice = false;

        if (choice.onChoiceSelected_Runtime != null)
        {
            choice.onChoiceSelected_Runtime.Invoke();
            return;
        }

        // 선택의 결과 실행
        if (!string.IsNullOrEmpty(choice.choiceEventKey))
        {
            if (choice.choiceEventKey == "ACCEPT")
            {
                npcPendingTask?.Invoke();
                npcPendingTask = null;
            }
            else if (choice.choiceEventKey == "DECLINE")
            {
                npcPendingTask = null;
            }
            else
            {
                ExecuteAction(choice.choiceEventKey); // 상점 열기 등
            }
        }

        //다음 대화가 있는지?
        if (!string.IsNullOrEmpty(choice.nextDialogueKey))
        {
            StartConversation(choice.nextDialogueKey, npcPendingTask);
        }
        else
        {
            EndConversation();
        }
    }

    private void EndConversation()
    {
        OnConversationEnd?.Invoke();

        npcPendingTask?.Invoke();
        npcPendingTask = null;
        currentLineIndex = 0;
    }
}
