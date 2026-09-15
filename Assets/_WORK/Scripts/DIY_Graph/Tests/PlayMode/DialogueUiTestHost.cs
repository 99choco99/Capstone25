using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalGraph.Tests
{
    /// <summary>PlayMode 테스트에서만 사용하는 명시적 Update와 UI 이벤트 연결 호스트입니다.</summary>
    [AddComponentMenu("")]
    [RequireComponent(typeof(UIDocument))]
    public sealed class DialogueUiTestHost : MonoBehaviour
    {
        private readonly List<(Button Button, Action Handler)> choiceBindings = new();
        private bool isBound;
        private int displayedPromptId;

        public DialogueManager Manager { get; } = new DialogueManager();
        public bool AutoContinue { get; set; }
        public VisualElement View { get; private set; }
        public Label LineLabel { get; private set; }
        public Button ContinueButton { get; private set; }
        public VisualElement Choices { get; private set; }
        public int UpdateCount { get; private set; }
        public int LineNotificationCount { get; private set; }
        public int EndNotificationCount { get; private set; }
        public int ContinueInputCount { get; private set; }
        public int ChoiceInputCount { get; private set; }

        private void OnEnable()
        {
            if (View == null)
            {
                View = new VisualElement { name = "dialogue-test-view" };
                View.style.width = 360f;
                LineLabel = new Label { name = "line" };
                ContinueButton = new Button { name = "continue", text = "Continue" };
                Choices = new VisualElement { name = "choices" };
                View.Add(LineLabel);
                View.Add(ContinueButton);
                View.Add(Choices);
            }

            GetComponent<UIDocument>().rootVisualElement.Add(View);
            HideView();
            Manager.ShowLine += ShowLine;
            Manager.ShowChoices += ShowChoices;
            Manager.ConversationEnd += EndConversation;
            ContinueButton.clicked += ContinueDialogue;
            isBound = true;
        }

        private void Update()
        {
            UpdateCount++;
            Manager.Tick(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            if (!isBound)
            {
                return;
            }

            // 소유 호스트를 끌 때는 진행을 취소하고 UI 및 구독도 함께 정리합니다.
            Manager.CancelConversation();
            Manager.ShowLine -= ShowLine;
            Manager.ShowChoices -= ShowChoices;
            Manager.ConversationEnd -= EndConversation;
            ContinueButton.clicked -= ContinueDialogue;
            HideView();
            View.RemoveFromHierarchy();
            isBound = false;
        }

        private void ShowLine(DialogueLineNodeData line)
        {
            LineNotificationCount++;
            ClearChoices();
            displayedPromptId = Manager.CurrentPromptId;
            LineLabel.text = line.DialogueText;
            View.style.display = DisplayStyle.Flex;
            ContinueButton.style.display = AutoContinue ? DisplayStyle.None : DisplayStyle.Flex;
            if (AutoContinue)
            {
                Manager.ContinueDialogue(displayedPromptId);
            }
        }

        private void ContinueDialogue()
        {
            ContinueInputCount++;
            Manager.ContinueDialogue(displayedPromptId);
        }

        private void ShowChoices(IReadOnlyList<DialogueChoiceData> choices)
        {
            ClearChoices();
            View.style.display = DisplayStyle.Flex;
            ContinueButton.style.display = DisplayStyle.None;
            int promptId = Manager.CurrentPromptId;
            foreach (DialogueChoiceData choice in choices)
            {
                var button = new Button { text = choice.ChoiceText };
                Action select = () =>
                {
                    ChoiceInputCount++;
                    Manager.SelectChoice(promptId, choice);
                };
                button.clicked += select;
                choiceBindings.Add((button, select));
                Choices.Add(button);
            }
        }

        private void EndConversation(DialogueEndReason reason)
        {
            EndNotificationCount++;
            HideView();
        }

        private void HideView()
        {
            displayedPromptId = 0;
            LineLabel.text = string.Empty;
            View.style.display = DisplayStyle.None;
            ClearChoices();
        }

        private void ClearChoices()
        {
            foreach (var binding in choiceBindings)
            {
                binding.Button.clicked -= binding.Handler;
            }
            choiceBindings.Clear();
            Choices.Clear();
        }
    }
}
