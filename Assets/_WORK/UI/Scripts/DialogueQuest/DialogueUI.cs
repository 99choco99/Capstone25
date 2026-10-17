using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniversalGraph;

public class DialogueUI : UIBase
{
    [SerializeField] TextMeshProUGUI speaker;
    [SerializeField] TextMeshProUGUI sentence;
    private DialogueManager dialogueManager;
    private PlayerInputHandler inputHandler;
    private PlayerInteraction playerInteraction;
    private readonly List<PromptUIItem> choicePool = new();
    private int linePromptId;
    private DialogueContainer currentDialogue;
    private readonly HashSet<DialogueLineNodeData> linesBeforeChoices = new();

    public PromptUIItem prompt;
    [SerializeField] private Transform choicePanel;
    [SerializeField] private RectTransform choiceContent;
    [SerializeField] private Button nextButton;

    // 대화 매니저는 UI가 준비될 때 한 번만 연결합니다.
    public override void Init()
    {
        dialogueManager = DialogueManager.Instance;
        dialogueManager.ConversationStart += OpenDialogue;
        dialogueManager.ShowLine += DisplayLine;
        dialogueManager.ShowChoices += DisplayChoices;
        dialogueManager.ConversationEnd += CloseDialogue;

        nextButton.onClick.AddListener(NextDialogue);
        nextButton.interactable = false;
        ClearChoices();
    }

    private void Update()
    {
        if (IsOpen)
            dialogueManager.Tick(Time.deltaTime, Time.unscaledDeltaTime);
    }

    // 플레이어가 재생성되면 입력만 새 플레이어에 다시 연결합니다.
    public override void SetUp(Player player)
    {
        if (inputHandler != null)
            inputHandler.OnUIInteractionPressed -= NextDialogue;
        dialogueManager.CancelConversation();

        inputHandler = player.InputHandler;
        playerInteraction = player.Interaction;

        inputHandler.OnUIInteractionPressed += NextDialogue;
    }

    private void OnDestroy()
    {
        if (inputHandler != null)
            inputHandler.OnUIInteractionPressed -= NextDialogue;
        if (dialogueManager == null)
            return;

        dialogueManager.ConversationStart -= OpenDialogue;
        dialogueManager.ShowLine -= DisplayLine;
        dialogueManager.ShowChoices -= DisplayChoices;
        dialogueManager.ConversationEnd -= CloseDialogue;

        nextButton.onClick.RemoveListener(NextDialogue);
    }

    public bool StartConversation(DialogueEntryPoint entryPoint, DialogueExecutionContext context)
    {
        if (dialogueManager.IsConversationActive)
            return false;

        currentDialogue = entryPoint.Container;
        bool started = dialogueManager.StartConversation(entryPoint, context);
        if (!started)
            currentDialogue = null;
        return started;
    }

    private void OpenDialogue()
    {
        DialogueChoiceLookup.Fill(currentDialogue, linesBeforeChoices);
        speaker.text = string.Empty;
        sentence.text = string.Empty;
        nextButton.interactable = false;

        ClearChoices();
        if (playerInteraction != null)
        {
            playerInteraction.IsDetectionPaused = true;
            playerInteraction.ClearInteraction();
        }

        MainUIManager.Instance.OpenUI(UIPanelType.Dialogue);
    }

    private void DisplayLine(DialogueLineNodeData data)
    {
        ClearChoices();
        speaker.text = data.SpeakerName;
        sentence.text = data.DialogueText;
        linePromptId = dialogueManager.CurrentPromptId;
        bool showChoicesImmediately = linesBeforeChoices.Contains(data);
        nextButton.interactable = !showChoicesImmediately;
        if (showChoicesImmediately && ReferenceEquals(dialogueManager.CurrentLine, data))
            dialogueManager.ContinueDialogue(linePromptId);
    }

    private void NextDialogue()
    {
        if (!IsOpen || !nextButton.interactable)
            return;

        nextButton.interactable = false;
        dialogueManager.ContinueDialogue(linePromptId);
    }

    private void DisplayChoices(IReadOnlyList<DialogueChoiceData> choices)
    {
        ClearChoices();
        nextButton.interactable = false;
        int promptId = dialogueManager.CurrentPromptId;

        for (int i = 0; i < choices.Count; i++)
        {
            if (i >= choicePool.Count)
                choicePool.Add(Instantiate(prompt, choiceContent));

            DialogueChoiceData choice = choices[i];
            PromptUIItem ui = choicePool[i];
            ui.SetText(choice.ChoiceText);
            ui.SetClickAction(() =>
            {
                if (!dialogueManager.IsWaitingForChoice || dialogueManager.CurrentPromptId != promptId)
                    return;
                ClearChoices();
                dialogueManager.SelectChoice(promptId, choice);
            });
            ui.gameObject.SetActive(true);
        }

        choicePanel.gameObject.SetActive(true);
    }

    private void ClearChoices()
    {
        foreach (PromptUIItem ui in choicePool)
        {
            ui.SetClickAction(null);
            ui.gameObject.SetActive(false);
        }

        choicePanel.gameObject.SetActive(false);
    }

    // 종료 이벤트를 받으면 UI와 조작만 복구합니다. 대화를 다시 종료하지 않습니다.
    private void CloseDialogue(DialogueEndReason reason)
    {
        currentDialogue = null;
        linesBeforeChoices.Clear();
        ClearChoices();
        nextButton.interactable = false;
        if (playerInteraction != null)
            playerInteraction.IsDetectionPaused = false;
        MainUIManager.Instance.CloseUI(UIPanelType.Dialogue);
    }
}

internal static class DialogueChoiceLookup
{
    public static void Fill(DialogueContainer currentDialogue, HashSet<DialogueLineNodeData> linesBeforeChoices)
    {
        linesBeforeChoices.Clear();
        if (currentDialogue == null)
            return;

        var linesByGuid = new Dictionary<string, DialogueLineNodeData>();
        var choiceGuids = new HashSet<string>();
        foreach (NodeBaseData node in currentDialogue.Nodes)
        {
            if (node is DialogueLineNodeData line)
                linesByGuid.Add(line.Guid, line);
            else if (node is DialogueChoiceNodeData choice)
                choiceGuids.Add(choice.Guid);
        }

        foreach (NodeLinkData link in currentDialogue.NodeLinks)
        {
            if (link.StartPortName == DialoguePortNames.Next
                && choiceGuids.Contains(link.TargetNodeGuid)
                && linesByGuid.TryGetValue(link.StartNodeGuid, out DialogueLineNodeData line))
                linesBeforeChoices.Add(line);
        }
    }
}
