using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UniversalGraph.Tests
{
    public sealed class UniversalGraphPlayModeIntegrationTests
    {
        private readonly List<GameObject> createdGameObjects = new();
        private readonly List<ScriptableObject> createdAssets = new();
        private float originalTimeScale;
        private bool questInitialized;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = originalTimeScale;
            foreach (GameObject gameObject in createdGameObjects)
            {
                if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
            }
            createdGameObjects.Clear();
            yield return null;

            if (questInitialized)
            {
                QuestManager.Initialize(Array.Empty<QuestContainer>());
                questInitialized = false;
            }
            foreach (ScriptableObject asset in createdAssets)
            {
                Object.Destroy(asset);
            }
            createdAssets.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TimedGuide_UpdateDisplaysThenHidesUiWhileScaledTimeIsPaused()
        {
            DialogueUiTestHost host = CreateHost("guide", autoContinue: true);
            DialogueContainer graph = CreateGuide(0.1f);
            yield return null;
            Assert.That(host.View.panel, Is.Not.Null, "UI must be attached to a runtime panel.");
            int previousUpdates = host.UpdateCount;
            Time.timeScale = 0f;

            Assert.That(host.Manager.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);
            Assert.That(host.LineLabel.text, Is.EqualTo("Find the key."));
            Assert.That(host.View.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(host.Manager.CurrentLine, Is.Null, "Auto-Continue must enter the Wait node.");

            yield return WaitForCompletion(host);

            Assert.That(host.UpdateCount, Is.GreaterThan(previousUpdates));
            Assert.That(host.Manager.LastEndReason, Is.EqualTo(DialogueEndReason.Completed));
            Assert.That(host.EndNotificationCount, Is.EqualTo(1));
            Assert.That(host.LineLabel.text, Is.Empty);
            Assert.That(host.View.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator TimedGuide_DoesNotAdvanceIndependentNpcLineOrChoiceUi()
        {
            DialogueUiTestHost guide = CreateHost("guide", autoContinue: true);
            DialogueUiTestHost npc = CreateHost("npc", autoContinue: false);
            DialogueContainer guideGraph = CreateGuide(0.05f);
            var npcLine = new DialogueLineNodeData { Guid = "line", DialogueText = "Hello." };
            var choiceNode = new DialogueChoiceNodeData { Guid = "choices" };
            choiceNode.Choices.Add(new DialogueChoiceData { PortName = "accept", ChoiceText = "Accept" });
            choiceNode.Choices.Add(new DialogueChoiceData { PortName = "decline", ChoiceText = "Decline" });
            var resultLine = new DialogueLineNodeData { Guid = "result", DialogueText = "Maybe later." };
            DialogueContainer npcGraph = CreateDialogue(npcLine, choiceNode, resultLine,
                new DialogueEndNodeData { Guid = "end" });
            Connect(npcGraph, "line", "Next", "choices");
            Connect(npcGraph, "choices", "accept", "end");
            Connect(npcGraph, "choices", "decline", "result");
            Connect(npcGraph, "result", "Next", "end");
            yield return null;
            Assert.That(npc.View.panel, Is.Not.Null);

            Assert.That(npc.Manager.StartConversation(new DialogueEntryPoint(npcGraph, "Default")), Is.True);
            int linePrompt = npc.Manager.CurrentPromptId;
            Assert.That(guide.Manager.StartConversation(new DialogueEntryPoint(guideGraph, "Default")), Is.True);
            yield return WaitForCompletion(guide);
            Assert.That(npc.Manager.CurrentLine, Is.SameAs(npcLine));
            Assert.That(npc.Manager.CurrentPromptId, Is.EqualTo(linePrompt));
            Assert.That(npc.LineLabel.text, Is.EqualTo("Hello."));

            Submit(npc.ContinueButton);
            Assert.That(npc.Manager.IsWaitingForChoice, Is.True);
            Assert.That(npc.ContinueInputCount, Is.EqualTo(1));
            List<Button> choices = npc.Choices.Query<Button>().ToList();
            Assert.That(choices, Has.Count.EqualTo(2));
            Assert.That(choices[1].text, Is.EqualTo("Decline"));
            int choicePrompt = npc.Manager.CurrentPromptId;
            Assert.That(guide.Manager.StartConversation(new DialogueEntryPoint(guideGraph, "Default")), Is.True);
            yield return WaitForCompletion(guide);
            Assert.That(npc.Manager.IsWaitingForChoice, Is.True);
            Assert.That(npc.Manager.CurrentPromptId, Is.EqualTo(choicePrompt));
            Assert.That(npc.LineLabel.text, Is.EqualTo("Hello."));

            Submit(choices[1]);
            Assert.That(npc.ChoiceInputCount, Is.EqualTo(1));
            Assert.That(npc.Manager.CurrentLine, Is.SameAs(resultLine));
            Assert.That(npc.LineLabel.text, Is.EqualTo("Maybe later."));
            Assert.That(npc.Choices.childCount, Is.Zero);
            Assert.That(guide.EndNotificationCount, Is.EqualTo(2));
            Assert.That(guide.View.style.display.value, Is.EqualTo(DisplayStyle.None));
            Submit(npc.ContinueButton);
            Assert.That(npc.Manager.LastEndReason, Is.EqualTo(DialogueEndReason.Completed));
            Assert.That(npc.View.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator HostDisable_CancelsWaitAndReenableBindsButtonsOnlyOnce()
        {
            DialogueUiTestHost host = CreateHost("lifecycle", autoContinue: true);
            DialogueContainer guide = CreateGuide(30f);
            yield return null;
            Assert.That(host.Manager.StartConversation(new DialogueEntryPoint(guide, "Default")), Is.True);
            host.enabled = false;
            Assert.That(host.Manager.IsConversationActive, Is.False);
            Assert.That(host.Manager.LastEndReason, Is.EqualTo(DialogueEndReason.Cancelled));
            Assert.That(host.EndNotificationCount, Is.EqualTo(1));
            Assert.That(host.LineLabel.text, Is.Empty);
            Assert.That(host.View.panel, Is.Null);
            int disabledUpdates = host.UpdateCount;
            yield return null;
            yield return null;
            Assert.That(host.UpdateCount, Is.EqualTo(disabledUpdates));

            host.AutoContinue = false;
            host.enabled = true;
            var firstLine = new DialogueLineNodeData { Guid = "first", DialogueText = "First" };
            var secondLine = new DialogueLineNodeData { Guid = "second", DialogueText = "Second" };
            DialogueContainer graph = CreateDialogue(firstLine, secondLine, new DialogueEndNodeData { Guid = "end" });
            Connect(graph, "first", "Next", "second");
            Connect(graph, "second", "Next", "end");
            yield return null;
            Assert.That(host.Manager.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);
            Assert.That(host.LineNotificationCount, Is.EqualTo(2));
            Submit(host.ContinueButton);
            Assert.That(host.ContinueInputCount, Is.EqualTo(1));
            Assert.That(host.Manager.CurrentLine, Is.SameAs(secondLine));
            Assert.That(host.LineNotificationCount, Is.EqualTo(3));

            host.gameObject.SetActive(false);
            Assert.That(host.Manager.IsConversationActive, Is.False);
            Assert.That(host.EndNotificationCount, Is.EqualTo(2));
            host.gameObject.SetActive(true);
            yield return null;
            Assert.That(host.View.panel, Is.Not.Null);
            Assert.That(host.Manager.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);
            Submit(host.ContinueButton);
            Assert.That(host.Manager.CurrentLine, Is.SameAs(secondLine));
            Assert.That(host.ContinueInputCount, Is.EqualTo(2));
            Assert.That(host.LineNotificationCount, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator QuestOnly_ProgressAndSaveRestoreWorkAcrossFramesWithoutDialogueHost()
        {
            QuestContainer graph = CreateAsset<QuestContainer>();
            graph.QuestId = 78001;
            graph.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            graph.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "goal", EventKey = "Collected", TargetId = 7, RequiredAmount = 2
            });
            graph.Nodes.Add(new QuestFlowEndNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(graph, "start", "Next", "goal");
            Connect(graph, "goal", "Next", "end");
            QuestManager.Initialize(new[] { graph });
            questInitialized = true;
            var original = new TestQuestController();
            Assert.That(QuestManager.StartQuest(original, graph.QuestId), Is.True);
            yield return null;

            Assert.That(QuestManager.ProcessObjectiveByGuid(original, graph.QuestId, "goal"), Is.True);
            QuestSaveData save = QuestManager.CaptureSaveData(original);
            Assert.That(save.QuestList[0].ObjectiveAmounts["goal"], Is.EqualTo(1));
            var restored = new TestQuestController();
            Assert.That(QuestManager.RestoreSaveData(restored, save, shouldClear: true, out string error), Is.True, error);
            Assert.That(restored.Notifications, Is.Zero, "Restore changes data without executing or notifying.");
            QuestManager.ResumeRestoredQuests(restored);
            Assert.That(restored.Notifications, Is.GreaterThan(0));
            yield return null;

            QuestManager.ProcessObjectivesByEvent(restored, "Collected", 7, 1);
            QuestSaveData completedSave = QuestManager.CaptureSaveData(restored);
            QuestProgress completed = restored.QuestProgress[graph.QuestId];
            Assert.That(completed.state, Is.EqualTo(QuestState.TurnedIn));
            Assert.That(completed.ActiveNodeGuids, Is.Empty);
            Assert.That(completed.ObjectiveAmounts["goal"], Is.EqualTo(2));
            Assert.That(original.QuestProgress[graph.QuestId].ObjectiveAmounts["goal"], Is.EqualTo(1));
            Assert.That(save.QuestList[0].ObjectiveAmounts["goal"], Is.EqualTo(1));
            Assert.That(completedSave.QuestList[0].state, Is.EqualTo(QuestState.TurnedIn),
                "Save after the Quest API has returned and node processing has finished.");
            Assert.That(createdGameObjects, Is.Empty, "Quest progression needs no Dialogue or UI host.");
        }

        private DialogueUiTestHost CreateHost(string name, bool autoContinue)
        {
            PanelSettings panelSettings = CreateAsset<PanelSettings>();
            panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DIYGraphPlayModeTheme");
            Assert.That(panelSettings.themeStyleSheet, Is.Not.Null);
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            var gameObject = new GameObject(name);
            createdGameObjects.Add(gameObject);
            gameObject.SetActive(false);
            gameObject.AddComponent<UIDocument>().panelSettings = panelSettings;
            DialogueUiTestHost host = gameObject.AddComponent<DialogueUiTestHost>();
            host.AutoContinue = autoContinue;
            gameObject.SetActive(true);
            return host;
        }

        private DialogueContainer CreateGuide(float duration)
        {
            DialogueContainer graph = CreateDialogue(
                new DialogueLineNodeData { Guid = "line", DialogueText = "Find the key." },
                new DialogueWaitNodeData { Guid = "wait", DurationSeconds = duration, UseUnscaledTime = true },
                new DialogueEndNodeData { Guid = "end" });
            Connect(graph, "line", "Next", "wait");
            Connect(graph, "wait", "Next", "end");
            return graph;
        }

        private DialogueContainer CreateDialogue(params NodeBaseData[] nodes)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" });
            foreach (NodeBaseData node in nodes)
            {
                graph.Nodes.Add(node);
            }
            Connect(graph, "entry", "Next", nodes[0].Guid);
            return graph;
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdAssets.Add(asset);
            return asset;
        }

        private static IEnumerator WaitForCompletion(DialogueUiTestHost host)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (host.Manager.IsConversationActive && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(host.Manager.IsConversationActive, Is.False, "The host's real Update must finish the timer.");
        }

        private static void Submit(Button button)
        {
            Assert.That(button.panel, Is.Not.Null);
            Assert.That(button.enabledInHierarchy, Is.True);
            button.Focus();
            // UI Toolkit의 실제 Submit 경로를 거쳐 Button.clicked에 연결한 입력을 검증합니다.
            using NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        private static void Connect(GraphContainer graph, string source, string port, string target)
        {
            graph.NodeLinks.Add(new NodeLinkData
            {
                StartNodeGuid = source, StartPortName = port,
                TargetNodeGuid = target, TargetPortName = "Input"
            });
        }

        private sealed class TestQuestController : IQuestController
        {
            public IDictionary<int, QuestProgress> QuestProgress { get; } = new Dictionary<int, QuestProgress>();
            public int Notifications { get; private set; }

            public void OnQuestProgressChanged(QuestContainer container, QuestProgress progress)
            {
                Notifications++;
            }
        }
    }
}
