using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UniversalGraph.Tests
{
    public sealed class DialogueManagerInstanceTests
    {
        private readonly List<DialogueContainer> createdContainers = new();
        private readonly List<DialogueManager> createdManagers = new();

        [TearDown]
        public void TearDown()
        {
            foreach (DialogueManager manager in createdManagers)
            {
                manager.CancelConversation();
            }
            createdManagers.Clear();
            foreach (DialogueContainer container in createdContainers)
            {
                Object.DestroyImmediate(container);
            }
            createdContainers.Clear();
        }

        [Test]
        public void Managers_KeepLineChoiceEventsAndCompletionIndependent()
        {
            DialogueManager first = CreateManager();
            DialogueManager second = CreateManager();
            var line = new DialogueLineNodeData { Guid = "line", DialogueText = "Shared line" };
            var choice = new DialogueChoiceData { PortName = "select", ChoiceText = "Continue" };
            var choices = new DialogueChoiceNodeData { Guid = "choices" };
            choices.Choices.Add(choice);
            DialogueContainer graph = CreateGraph(line, choices, new DialogueEndNodeData { Guid = "end" });
            Connect(graph, "line", DialoguePortNames.Next, "choices");
            Connect(graph, "choices", "select", "end");
            int firstLines = 0;
            int secondLines = 0;
            int firstChoices = 0;
            int secondChoices = 0;
            int firstCompleted = 0;
            int secondCompleted = 0;
            first.ShowLine += _ => firstLines++;
            second.ShowLine += _ => secondLines++;
            first.ShowChoices += _ => firstChoices++;
            second.ShowChoices += _ => secondChoices++;

            Assert.That(first.StartConversation(new DialogueEntryPoint(graph, "Default"),
                onComplete: () => firstCompleted++), Is.True);
            Assert.That(second.StartConversation(new DialogueEntryPoint(graph, "Default"),
                onComplete: () => secondCompleted++), Is.True);
            int secondLinePrompt = second.CurrentPromptId;

            Assert.That(first.ContinueDialogue(first.CurrentPromptId), Is.True);
            Assert.That(first.IsWaitingForChoice, Is.True);
            Assert.That(first.CurrentChoices, Is.EqualTo(new[] { choice }));
            Assert.That(second.CurrentLine, Is.SameAs(line));
            Assert.That(second.CurrentPromptId, Is.EqualTo(secondLinePrompt));
            Assert.That(second.CurrentChoices, Is.Empty);
            Assert.That(new[] { firstLines, secondLines, firstChoices, secondChoices }, Is.EqualTo(new[] { 1, 1, 1, 0 }));

            Assert.That(first.SelectChoice(first.CurrentPromptId, choice), Is.True);
            Assert.That(first.IsConversationActive, Is.False);
            Assert.That(first.LastEndReason, Is.EqualTo(DialogueEndReason.Completed));
            Assert.That(second.IsConversationActive, Is.True);
            Assert.That(second.CurrentLine, Is.SameAs(line));
            Assert.That(new[] { firstCompleted, secondCompleted }, Is.EqualTo(new[] { 1, 0 }));

            Assert.That(second.ContinueDialogue(secondLinePrompt), Is.True);
            Assert.That(second.SelectChoice(second.CurrentPromptId, choice), Is.True);
            Assert.That(second.LastEndReason, Is.EqualTo(DialogueEndReason.Completed));
            Assert.That(new[] { firstChoices, secondChoices, firstCompleted, secondCompleted }, Is.EqualTo(new[] { 1, 1, 1, 1 }));
        }

        [Test]
        public void Managers_ConsumeSignalsOnlyOnTheAddressedInstance()
        {
            DialogueManager first = CreateManager();
            DialogueManager second = CreateManager();
            var wait = new DialogueWaitSignalNodeData { Guid = "wait", SignalKey = "ready" };
            var line = new DialogueLineNodeData { Guid = "line" };
            DialogueContainer graph = CreateGraph(wait, line, new DialogueEndNodeData { Guid = "end" });
            Connect(graph, "wait", DialoguePortNames.Next, "line");
            Connect(graph, "line", DialoguePortNames.Next, "end");

            Assert.That(first.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);
            Assert.That(second.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);
            first.Tick(100f, 100f);
            Assert.That(first.CurrentLine, Is.Null);
            Assert.That(first.SendSignal("ready"), Is.True);
            Assert.That(first.SendSignal("ready"), Is.False);
            Assert.That(first.CurrentLine, Is.SameAs(line));
            Assert.That(second.CurrentLine, Is.Null);
            Assert.That(second.CurrentPromptId, Is.Zero);

            first.CancelConversation();
            Assert.That(first.LastEndReason, Is.EqualTo(DialogueEndReason.Cancelled));
            Assert.That(second.IsConversationActive, Is.True);
            Assert.That(second.SendSignal("ready"), Is.True);
            Assert.That(second.CurrentLine, Is.SameAs(line));
            Assert.That(first.IsConversationActive, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Managers_AdvanceOnlyTheirOwnSelectedClock(bool useUnscaledTime)
        {
            DialogueManager first = CreateManager();
            DialogueManager second = CreateManager();
            DialogueContainer graph = CreateTimedGraph(useUnscaledTime, out DialogueLineNodeData line);
            Assert.That(first.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);
            Assert.That(second.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);
            float scaledDelta = useUnscaledTime ? 100f : 0.5f;
            float unscaledDelta = useUnscaledTime ? 0.5f : 100f;

            first.Tick(scaledDelta, unscaledDelta);
            Assert.That(first.CurrentLine, Is.Null);
            Assert.That(second.CurrentLine, Is.Null);
            first.Tick(scaledDelta, unscaledDelta);
            Assert.That(first.CurrentLine, Is.SameAs(line));
            Assert.That(second.CurrentLine, Is.Null);
            Assert.That(second.CurrentPromptId, Is.Zero);

            first.CancelConversation();
            second.Tick(scaledDelta, unscaledDelta);
            Assert.That(second.CurrentLine, Is.Null);
            second.Tick(scaledDelta, unscaledDelta);
            Assert.That(second.CurrentLine, Is.SameAs(line));
            Assert.That(first.IsConversationActive, Is.False);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Tick_IgnoresNonPositiveOrNonFiniteTime(float deltaTime)
        {
            DialogueManager manager = CreateManager();
            DialogueContainer graph = CreateTimedGraph(false, out DialogueLineNodeData line);
            Assert.That(manager.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);

            manager.Tick(deltaTime, deltaTime);
            Assert.That(manager.CurrentLine, Is.Null);
            manager.Tick(0.5f, 0.5f);
            Assert.That(manager.CurrentLine, Is.Null);
            manager.Tick(0.5f, 0.5f);
            Assert.That(manager.CurrentLine, Is.SameAs(line));
        }

        [Test]
        public void Tick_DoesNotCarryExcessTimeIntoTheNextWait()
        {
            DialogueManager manager = CreateManager();
            var firstWait = new DialogueWaitNodeData { Guid = "first-wait", DurationSeconds = 1f };
            var secondWait = new DialogueWaitNodeData { Guid = "second-wait", DurationSeconds = 2f };
            var line = new DialogueLineNodeData { Guid = "line" };
            DialogueContainer graph = CreateGraph(firstWait, secondWait, line);
            Connect(graph, "first-wait", DialoguePortNames.Next, "second-wait");
            Connect(graph, "second-wait", DialoguePortNames.Next, "line");
            Assert.That(manager.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);

            manager.Tick(100f, 100f);
            Assert.That(manager.CurrentLine, Is.Null);
            manager.Tick(1f, 1f);
            Assert.That(manager.CurrentLine, Is.Null);
            manager.Tick(1f, 1f);
            Assert.That(manager.CurrentLine, Is.SameAs(line));
        }

        [Test]
        public void Instance_RemainsStableAndUsesItsOwnExplicitClock()
        {
            DialogueManager defaultManager = DialogueManager.Instance;
            createdManagers.Add(defaultManager);
            DialogueManager independentManager = CreateManager();
            DialogueContainer graph = CreateTimedGraph(false, out DialogueLineNodeData line);
            Assert.That(DialogueManager.Instance, Is.SameAs(defaultManager));
            Assert.That(independentManager, Is.Not.SameAs(defaultManager));
            Assert.That(defaultManager.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);
            Assert.That(independentManager.StartConversation(new DialogueEntryPoint(graph, "Default")), Is.True);

            independentManager.Tick(1f, 1f);
            Assert.That(independentManager.CurrentLine, Is.SameAs(line));
            Assert.That(defaultManager.CurrentLine, Is.Null);
            defaultManager.Tick(1f, 1f);
            Assert.That(defaultManager.CurrentLine, Is.SameAs(line));
            independentManager.CancelConversation();
            Assert.That(defaultManager.IsConversationActive, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Guidance_CanDisplayThenHideAfterWaitWithoutQuestOrBlockingNpcDialogue(bool withNpcDialogue)
        {
            DialogueManager guidance = CreateManager();
            DialogueManager npc = CreateManager();
            var guideLine = new DialogueLineNodeData { Guid = "guide", DialogueText = "Find the key." };
            var wait = new DialogueWaitNodeData { Guid = "wait", DurationSeconds = 2f };
            DialogueContainer guideGraph = CreateGraph(guideLine, wait, new DialogueEndNodeData { Guid = "end" });
            Connect(guideGraph, "guide", DialoguePortNames.Next, "wait");
            Connect(guideGraph, "wait", DialoguePortNames.Next, "end");

            var npcLine = new DialogueLineNodeData { Guid = "npc", DialogueText = "Hello." };
            if (withNpcDialogue)
            {
                DialogueContainer npcGraph = CreateGraph(npcLine, new DialogueEndNodeData { Guid = "end" });
                Connect(npcGraph, "npc", DialoguePortNames.Next, "end");
                Assert.That(npc.StartConversation(new DialogueEntryPoint(npcGraph, "Default")), Is.True);
            }

            string displayedText = null;
            guidance.ShowLine += data =>
            {
                displayedText = data.DialogueText;
                // 안내 UI는 클릭을 기다리지 않고 표시 직후 다음 Wait로 진행시킵니다.
                Assert.That(guidance.ContinueDialogue(guidance.CurrentPromptId), Is.True);
            };
            guidance.ConversationEnd += _ => displayedText = null;

            Assert.That(guidance.StartConversation(new DialogueEntryPoint(guideGraph, "Default")), Is.True);
            Assert.That(displayedText, Is.EqualTo("Find the key."));
            guidance.Tick(1f, 1f);
            Assert.That(displayedText, Is.Not.Null);
            guidance.Tick(1f, 1f);
            Assert.That(displayedText, Is.Null);
            Assert.That(guidance.LastEndReason, Is.EqualTo(DialogueEndReason.Completed));
            Assert.That(npc.IsConversationActive, Is.EqualTo(withNpcDialogue));
            if (withNpcDialogue)
            {
                Assert.That(npc.CurrentLine, Is.SameAs(npcLine));
            }
        }

        private DialogueManager CreateManager()
        {
            var manager = new DialogueManager();
            createdManagers.Add(manager);
            return manager;
        }

        private DialogueContainer CreateTimedGraph(bool useUnscaledTime, out DialogueLineNodeData line)
        {
            var wait = new DialogueWaitNodeData { Guid = "wait", DurationSeconds = 1f, UseUnscaledTime = useUnscaledTime };
            line = new DialogueLineNodeData { Guid = "line" };
            DialogueContainer graph = CreateGraph(wait, line);
            Connect(graph, "wait", DialoguePortNames.Next, "line");
            return graph;
        }

        private DialogueContainer CreateGraph(params NodeBaseData[] nodes)
        {
            DialogueContainer graph = ScriptableObject.CreateInstance<DialogueContainer>();
            createdContainers.Add(graph);
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" });
            foreach (NodeBaseData node in nodes)
            {
                graph.Nodes.Add(node);
            }
            Connect(graph, "entry", DialoguePortNames.Next, nodes[0].Guid);
            return graph;
        }

        private static void Connect(DialogueContainer graph, string source, string port, string target)
        {
            graph.NodeLinks.Add(new NodeLinkData
            {
                StartNodeGuid = source,
                StartPortName = port,
                TargetNodeGuid = target,
                TargetPortName = "Input"
            });
        }
    }
}
