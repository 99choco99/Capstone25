using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace UniversalGraph.Tests
{
    public sealed class QuestRestoreRegressionTests
    {
        private const string ResumeActionKey = "tests.quest.restore-resume";
        private readonly List<QuestContainer> createdGraphs = new();
        private IDictionary<string, QuestMethodDescriptor> actionRegistry;
        private QuestMethodDescriptor previousDescriptor;

        [SetUp]
        public void RegisterResumeAction()
        {
            // Editor 테스트 메서드는 운영 검색에서 제외되므로 이 테스트의 키만 직접 등록합니다.
            QuestMethodInvoker.Initialize();
            actionRegistry = (IDictionary<string, QuestMethodDescriptor>)typeof(QuestMethodInvoker)
                .GetField("actionRegistry", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            actionRegistry.TryGetValue(ResumeActionKey, out previousDescriptor);
            actionRegistry.Remove(ResumeActionKey);
            MethodInfo method = typeof(TestController).GetMethod("RecordResume", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(QuestMethodDescriptorFactory.TryCreateDescriptor(
                method, MethodKind.Action, ResumeActionKey, QuestMethodOwner.Controller,
                out QuestMethodDescriptor descriptor, out string error), Is.True, error);
            typeof(QuestMethodInvoker).GetMethod("RegisterDescriptor", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { descriptor });
        }

        [TearDown]
        public void TearDown()
        {
            if (previousDescriptor == null)
            {
                actionRegistry.Remove(ResumeActionKey);
            }
            else
            {
                actionRegistry[ResumeActionKey] = previousDescriptor;
            }
            QuestDefinitionRegistry.Initialize(Array.Empty<QuestContainer>());
            foreach (QuestContainer graph in createdGraphs)
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
            createdGraphs.Clear();
        }

        [Test]
        public void MergeSave_WaitsUntilExplicitResumeAndDoesNotRepeatTheAction()
        {
            QuestContainer target = CreateObjectiveQuest(2);
            QuestContainer waiting = CreateWaitingQuest(1, target.QuestId, QuestState.TurnedIn);
            QuestDefinitionRegistry.Initialize(new[] { waiting, target });
            var controller = new TestController();
            Assert.That(QuestRunner.StartQuest(controller, target.QuestId), Is.True);
            Assert.That(QuestRunner.StartQuest(controller, waiting.QuestId), Is.True);
            QuestProgress originalWaiting = controller.QuestProgress[waiting.QuestId];
            controller.Notifications.Clear();
            QuestSaveData save = CreateCompletedSave(target);

            Assert.That(save.TryApplyTo(controller, replaceExisting: false, out string error), Is.True, error);

            Assert.That(controller.QuestProgress[waiting.QuestId], Is.SameAs(originalWaiting));
            Assert.That(originalWaiting.activeNodeGuids, Is.EqualTo(new[] { "wait" }));
            Assert.That(controller.ActionCount, Is.Zero);
            Assert.That(controller.Notifications, Is.Empty);

            QuestRunner.ResumeRestoredQuests(controller);

            Assert.That(originalWaiting.activeNodeGuids, Is.EqualTo(new[] { "after-wait" }));
            Assert.That(controller.ActionCount, Is.EqualTo(1));
            Assert.That(originalWaiting.completedNodeGuids, Is.EquivalentTo(new[] { "wait", "action" }));

            QuestRunner.ResumeRestoredQuests(controller);

            Assert.That(controller.ActionCount, Is.EqualTo(1));
            Assert.That(originalWaiting.activeNodeGuids, Is.EqualTo(new[] { "after-wait" }));
            Assert.That(originalWaiting.nodeProgressCounts["after-wait"], Is.Zero);
        }

        [Test]
        public void FullRestore_ReevaluatesNotStartedWhenTheTargetHasNoSavedRecord()
        {
            QuestContainer target = CreateObjectiveQuest(2);
            QuestContainer waiting = CreateWaitingQuest(1, target.QuestId, QuestState.NotStarted);
            QuestDefinitionRegistry.Initialize(new[] { waiting, target });
            var controller = new TestController();
            Assert.That(QuestRunner.StartQuest(controller, target.QuestId), Is.True);
            Assert.That(QuestRunner.StartQuest(controller, waiting.QuestId), Is.True);
            var save = new QuestSaveData
            {
                quests = new List<QuestProgressSaveData>
                {
                    QuestProgressSaveData.Capture(controller.QuestProgress[waiting.QuestId])
                }
            };
            controller.Notifications.Clear();

            Assert.That(save.TryApplyTo(controller, replaceExisting: true, out string error), Is.True, error);
            Assert.That(controller.QuestProgress.ContainsKey(target.QuestId), Is.False);
            Assert.That(controller.QuestProgress[waiting.QuestId].activeNodeGuids, Is.EqualTo(new[] { "wait" }));
            Assert.That(controller.Notifications, Is.Empty);

            QuestRunner.ResumeRestoredQuests(controller);

            Assert.That(controller.QuestProgress[waiting.QuestId].activeNodeGuids,
                Is.EqualTo(new[] { "after-wait" }));
            Assert.That(controller.ActionCount, Is.EqualTo(1));
        }

        [Test]
        public void Resume_ReportsTheRestoredStateBeforeTheFinalTurnedInState()
        {
            QuestContainer target = CreateObjectiveQuest(2);
            QuestContainer waiting = CreateGraph(1);
            waiting.Nodes.Add(new QuestWaitForQuestNodeData
            {
                Guid = "wait", TargetQuestId = target.QuestId, RequiredState = QuestState.TurnedIn
            });
            waiting.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(waiting, "start", "wait");
            Connect(waiting, "wait", "end");
            QuestDefinitionRegistry.Initialize(new[] { waiting, target });
            var controller = new TestController();
            Assert.That(QuestRunner.StartQuest(controller, target.QuestId), Is.True);
            Assert.That(QuestRunner.StartQuest(controller, waiting.QuestId), Is.True);
            QuestSaveData save = CreateCompletedSave(target);
            Assert.That(save.TryApplyTo(controller, replaceExisting: false, out string error), Is.True, error);
            controller.Notifications.Clear();

            QuestRunner.ResumeRestoredQuests(controller);

            Assert.That(controller.QuestProgress[waiting.QuestId].state, Is.EqualTo(QuestState.TurnedIn));
            Assert.That(controller.Notifications.Where(item => item.QuestId == waiting.QuestId)
                .Select(item => item.State), Is.EqualTo(new[] { QuestState.InProgress, QuestState.TurnedIn }));
        }

        [Test]
        public void Resume_UsesTheCurrentProgressWhenAnEarlierNotificationReplacesIt()
        {
            QuestContainer first = CreateObjectiveQuest(1);
            QuestContainer second = CreateObjectiveQuest(2);
            QuestDefinitionRegistry.Initialize(new[] { first, second });
            var controller = new TestController();
            Assert.That(QuestRunner.StartQuest(controller, first.QuestId), Is.True);
            Assert.That(QuestRunner.StartQuest(controller, second.QuestId), Is.True);
            QuestProgress original = controller.QuestProgress[second.QuestId];
            var replacement = new QuestProgress(second) { state = QuestState.CanComplete };
            controller.Notifications.Clear();
            controller.OnStatusChanged = (_, progress) =>
            {
                if (progress.questId == first.QuestId)
                {
                    controller.QuestProgress[second.QuestId] = replacement;
                }
            };

            QuestRunner.ResumeRestoredQuests(controller);

            Assert.That(controller.Notifications.Any(item => ReferenceEquals(item.Progress, original)), Is.False);
            Assert.That(controller.Notifications.Single(item => item.QuestId == second.QuestId).Progress,
                Is.SameAs(replacement));
        }

        [Test]
        public void StateChange_ResumesMoreThan256WaitsActivatedAcrossSeparateSteps()
        {
            const int count = 257;
            QuestContainer target = CreateObjectiveQuest(2);
            QuestContainer waiting = CreateGraph(1);
            waiting.Nodes.Add(new QuestAndGateNodeData { Guid = "join" });
            waiting.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(waiting, "join", "end");
            for (int i = 0; i < count; i++)
            {
                waiting.Nodes.Add(new QuestObjectiveNodeData { Guid = $"step-{i}", EventKey = "step" });
                waiting.Nodes.Add(new QuestWaitForQuestNodeData
                {
                    Guid = $"wait-{i}", TargetQuestId = target.QuestId, RequiredState = QuestState.TurnedIn
                });
                waiting.Nodes.Add(new QuestObjectiveNodeData { Guid = $"after-{i}", EventKey = "after" });
                Connect(waiting, $"step-{i}", $"wait-{i}");
                Connect(waiting, $"wait-{i}", $"after-{i}");
                Connect(waiting, $"after-{i}", "join");
                if (i + 1 < count)
                {
                    Connect(waiting, $"step-{i}", $"step-{i + 1}");
                }
            }
            Connect(waiting, "start", "step-0");
            QuestDefinitionRegistry.Initialize(new[] { waiting, target });
            var controller = new TestController();
            Assert.That(QuestRunner.StartQuest(controller, target.QuestId), Is.True);
            Assert.That(QuestRunner.StartQuest(controller, waiting.QuestId), Is.True);

            // 한 번에 257개를 실행하지 않고, 개별 게임 입력으로 대기를 차례로 활성화합니다.
            for (int i = 0; i < count; i++)
            {
                Assert.That(QuestRunner.AdvanceObjective(controller, waiting.QuestId, $"step-{i}"), Is.True);
            }
            QuestProgress progress = controller.QuestProgress[waiting.QuestId];
            Assert.That(progress.activeNodeGuids.Count, Is.EqualTo(count));

            Assert.That(QuestRunner.SetQuestState(controller, target.QuestId, QuestState.TurnedIn), Is.True);

            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.activeNodeGuids,
                Is.EquivalentTo(Enumerable.Range(0, count).Select(i => $"after-{i}")));
            Assert.That(progress.completedNodeGuids.Count(guid => guid.StartsWith("wait-")), Is.EqualTo(count));
        }

        private QuestContainer CreateGraph(int questId)
        {
            var graph = ScriptableObject.CreateInstance<QuestContainer>();
            graph.QuestId = questId;
            graph.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            createdGraphs.Add(graph);
            return graph;
        }

        private QuestContainer CreateObjectiveQuest(int questId)
        {
            QuestContainer graph = CreateGraph(questId);
            graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "objective", EventKey = "work" });
            graph.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(graph, "start", "objective");
            Connect(graph, "objective", "end");
            return graph;
        }

        private QuestContainer CreateWaitingQuest(int questId, int targetQuestId, QuestState requiredState)
        {
            QuestContainer graph = CreateGraph(questId);
            graph.Nodes.Add(new QuestWaitForQuestNodeData
            {
                Guid = "wait", TargetQuestId = targetQuestId, RequiredState = requiredState
            });
            graph.Nodes.Add(new QuestActionNodeData
            {
                Guid = "action", Action = new MethodBindingData { Key = ResumeActionKey }
            });
            graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "after-wait", EventKey = "after" });
            graph.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(graph, "start", "wait");
            Connect(graph, "wait", "action");
            Connect(graph, "action", "after-wait");
            Connect(graph, "after-wait", "end");
            return graph;
        }

        private static QuestSaveData CreateCompletedSave(QuestContainer target)
        {
            var source = new TestController();
            Assert.That(QuestRunner.StartQuest(source, target.QuestId), Is.True);
            Assert.That(QuestRunner.AdvanceObjective(source, target.QuestId, "objective"), Is.True);
            return QuestSaveData.Capture(source);
        }

        private static void Connect(QuestContainer graph, string source, string target)
        {
            graph.NodeLinks.Add(new NodeLinkData
            {
                StartNodeGuid = source, StartPortName = QuestPortNames.Next,
                TargetNodeGuid = target, TargetPortName = QuestPortNames.Input
            });
        }

        private sealed class TestController : IQuestController
        {
            // 알림 중 교체 테스트에서 Quest 1이 Quest 2보다 먼저 통지되도록 순서를 고정합니다.
            public IDictionary<int, QuestProgress> QuestProgress { get; } = new SortedDictionary<int, QuestProgress>();
            public List<(int QuestId, QuestState State, QuestProgress Progress)> Notifications { get; } = new();
            public Action<QuestContainer, QuestProgress> OnStatusChanged;
            public int ActionCount;

            [QuestAction(ResumeActionKey, Owner = QuestMethodOwner.Controller)]
            private void RecordResume()
            {
                ActionCount++;
            }

            public void InvokeStatusChanged(QuestContainer container, QuestProgress progress)
            {
                Notifications.Add((progress.questId, progress.state, progress));
                OnStatusChanged?.Invoke(container, progress);
            }
        }
    }
}
