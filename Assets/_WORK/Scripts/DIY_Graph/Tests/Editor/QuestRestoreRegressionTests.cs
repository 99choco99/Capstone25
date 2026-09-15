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
        private readonly List<QuestContainer> createdContainers = new();
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
            Assert.That(QuestMethodDescriptorFactory.CreateDescriptor(
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
            QuestManager.Initialize(Array.Empty<QuestContainer>());
            foreach (QuestContainer container in createdContainers)
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
            createdContainers.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestoreSaveData_RejectsNullWithoutChangingExistingProgress(bool replaceExisting)
        {
            QuestContainer container = CreateObjectiveQuest(1);
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            QuestProgress original = controller.QuestProgress[container.QuestId];
            controller.Notifications.Clear();

            Assert.That(QuestManager.RestoreSaveData(controller, null, replaceExisting, out string error), Is.False);

            Assert.That(error, Does.Contain("저장 데이터"));
            Assert.That(controller.QuestProgress.Count, Is.EqualTo(1));
            Assert.That(controller.QuestProgress[container.QuestId], Is.SameAs(original));
            Assert.That(original.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(original.ActiveNodeGuids, Is.EqualTo(new[] { "objective" }));
            Assert.That(controller.Notifications, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NullQuestList_IsRejectedWithoutChangingExistingProgress(bool replaceExisting)
        {
            QuestContainer container = CreateObjectiveQuest(1);
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            QuestProgress original = controller.QuestProgress[container.QuestId];
            controller.Notifications.Clear();
            var save = new QuestSaveData { QuestList = null };

            Assert.That(QuestManager.RestoreSaveData(controller, save, replaceExisting, out string error), Is.False);

            Assert.That(error, Does.Contain("QuestList"));
            Assert.That(controller.QuestProgress.Count, Is.EqualTo(1));
            Assert.That(controller.QuestProgress[container.QuestId], Is.SameAs(original));
            Assert.That(original.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(original.ActiveNodeGuids, Is.EqualTo(new[] { "objective" }));
            Assert.That(controller.Notifications, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SaveData_EmptyQuestListSupportsMergeAndReplace(bool replaceExisting)
        {
            QuestContainer container = CreateObjectiveQuest(1);
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            QuestProgress original = controller.QuestProgress[container.QuestId];
            controller.Notifications.Clear();
            var save = new QuestSaveData();

            Assert.That(save.QuestList, Is.Not.Null.And.Empty);
            Assert.That(QuestManager.RestoreSaveData(controller, save, replaceExisting, out string restoreError), Is.True, restoreError);

            Assert.That(controller.QuestProgress.Count, Is.EqualTo(replaceExisting ? 0 : 1));
            if (!replaceExisting)
            {
                Assert.That(controller.QuestProgress[container.QuestId], Is.SameAs(original));
            }
            Assert.That(controller.Notifications, Is.Empty);
        }

        [Test]
        public void MergeSave_WaitsUntilExplicitResumeAndDoesNotRepeatTheAction()
        {
            QuestContainer targetContainer = CreateObjectiveQuest(2);
            QuestContainer waitingContainer = CreateWaitingQuest(1, targetContainer.QuestId, QuestState.TurnedIn);
            QuestManager.Initialize(new[] { waitingContainer, targetContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);
            QuestProgress originalWaiting = controller.QuestProgress[waitingContainer.QuestId];
            controller.Notifications.Clear();
            QuestSaveData save = CreateCompletedSave(targetContainer);

            Assert.That(QuestManager.RestoreSaveData(controller, save, shouldClear: false, out string error), Is.True, error);

            Assert.That(controller.QuestProgress[waitingContainer.QuestId], Is.SameAs(originalWaiting));
            Assert.That(originalWaiting.ActiveNodeGuids, Is.EqualTo(new[] { "wait" }));
            Assert.That(controller.ActionCount, Is.Zero);
            Assert.That(controller.Notifications, Is.Empty);

            QuestManager.ResumeRestoredQuests(controller);

            Assert.That(originalWaiting.ActiveNodeGuids, Is.EqualTo(new[] { "after-wait" }));
            Assert.That(controller.ActionCount, Is.EqualTo(1));
            Assert.That(originalWaiting.CompletedNodeGuids, Is.EquivalentTo(new[] { "wait", "action" }));

            QuestManager.ResumeRestoredQuests(controller);

            Assert.That(controller.ActionCount, Is.EqualTo(1));
            Assert.That(originalWaiting.ActiveNodeGuids, Is.EqualTo(new[] { "after-wait" }));
            Assert.That(originalWaiting.ObjectiveAmounts["after-wait"], Is.Zero);
        }

        [Test]
        public void FullRestore_ReevaluatesNotStartedWhenTheTargetHasNoSavedRecord()
        {
            QuestContainer targetContainer = CreateObjectiveQuest(2);
            QuestContainer waitingContainer = CreateWaitingQuest(1, targetContainer.QuestId, QuestState.NotStarted);
            QuestManager.Initialize(new[] { waitingContainer, targetContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);
            QuestSaveData save = QuestManager.CaptureSaveData(controller);
            save.QuestList.RemoveAll(progress => progress.questId != waitingContainer.QuestId);
            controller.Notifications.Clear();

            Assert.That(QuestManager.RestoreSaveData(controller, save, shouldClear: true, out string error), Is.True, error);
            Assert.That(controller.QuestProgress.ContainsKey(targetContainer.QuestId), Is.False);
            Assert.That(controller.QuestProgress[waitingContainer.QuestId].ActiveNodeGuids, Is.EqualTo(new[] { "wait" }));
            Assert.That(controller.Notifications, Is.Empty);

            QuestManager.ResumeRestoredQuests(controller);

            Assert.That(controller.QuestProgress[waitingContainer.QuestId].ActiveNodeGuids,
                Is.EqualTo(new[] { "after-wait" }));
            Assert.That(controller.ActionCount, Is.EqualTo(1));
        }

        [Test]
        public void Resume_ReportsTheRestoredStateBeforeTheFinalTurnedInState()
        {
            QuestContainer targetContainer = CreateObjectiveQuest(2);
            QuestContainer waitingContainer = CreateGraph(1);
            waitingContainer.Nodes.Add(new QuestStateWaitNodeData
            {
                Guid = "wait", TargetQuestId = targetContainer.QuestId, RequiredState = QuestState.TurnedIn
            });
            waitingContainer.Nodes.Add(new QuestFlowEndNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(waitingContainer, "start", "wait");
            Connect(waitingContainer, "wait", "end");
            QuestManager.Initialize(new[] { waitingContainer, targetContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);
            QuestSaveData save = CreateCompletedSave(targetContainer);
            Assert.That(QuestManager.RestoreSaveData(controller, save, shouldClear: false, out string error), Is.True, error);
            controller.Notifications.Clear();

            QuestManager.ResumeRestoredQuests(controller);

            Assert.That(controller.QuestProgress[waitingContainer.QuestId].state, Is.EqualTo(QuestState.TurnedIn));
            Assert.That(controller.Notifications.Where(item => item.QuestId == waitingContainer.QuestId)
                .Select(item => item.State), Is.EqualTo(new[] { QuestState.InProgress, QuestState.TurnedIn }));
        }

        [Test]
        public void Resume_UsesTheCurrentProgressWhenAnEarlierNotificationReplacesIt()
        {
            QuestContainer firstContainer = CreateObjectiveQuest(1);
            QuestContainer secondContainer = CreateObjectiveQuest(2);
            QuestManager.Initialize(new[] { firstContainer, secondContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, firstContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, secondContainer.QuestId), Is.True);
            QuestProgress original = controller.QuestProgress[secondContainer.QuestId];
            var replacement = new QuestProgress(secondContainer) { state = QuestState.CanComplete };
            controller.Notifications.Clear();
            controller.ProgressChanged = (_, progress) =>
            {
                if (progress.questId == firstContainer.QuestId)
                {
                    controller.QuestProgress[secondContainer.QuestId] = replacement;
                }
            };

            QuestManager.ResumeRestoredQuests(controller);

            Assert.That(controller.Notifications.Any(item => ReferenceEquals(item.Progress, original)), Is.False);
            Assert.That(controller.Notifications.Single(item => item.QuestId == secondContainer.QuestId).Progress,
                Is.SameAs(replacement));
        }

        [Test]
        public void StateChange_ResumesMoreThan256WaitsActivatedAcrossSeparateSteps()
        {
            const int count = 257;
            QuestContainer targetContainer = CreateObjectiveQuest(2);
            QuestContainer waitingContainer = CreateGraph(1);
            waitingContainer.Nodes.Add(new QuestAndGateNodeData { Guid = "join" });
            waitingContainer.Nodes.Add(new QuestFlowEndNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(waitingContainer, "join", "end");
            for (int i = 0; i < count; i++)
            {
                waitingContainer.Nodes.Add(new QuestObjectiveNodeData { Guid = $"step-{i}", EventKey = "step" });
                waitingContainer.Nodes.Add(new QuestStateWaitNodeData
                {
                    Guid = $"wait-{i}", TargetQuestId = targetContainer.QuestId, RequiredState = QuestState.TurnedIn
                });
                waitingContainer.Nodes.Add(new QuestObjectiveNodeData { Guid = $"after-{i}", EventKey = "after" });
                Connect(waitingContainer, $"step-{i}", $"wait-{i}");
                Connect(waitingContainer, $"wait-{i}", $"after-{i}");
                Connect(waitingContainer, $"after-{i}", "join");
                if (i + 1 < count)
                {
                    Connect(waitingContainer, $"step-{i}", $"step-{i + 1}");
                }
            }
            Connect(waitingContainer, "start", "step-0");
            QuestManager.Initialize(new[] { waitingContainer, targetContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);

            // 한 번에 257개를 실행하지 않고, 개별 게임 입력으로 대기를 차례로 활성화합니다.
            for (int i = 0; i < count; i++)
            {
                Assert.That(QuestManager.ProcessObjectiveByGuid(controller, waitingContainer.QuestId, $"step-{i}"), Is.True);
            }
            QuestProgress progress = controller.QuestProgress[waitingContainer.QuestId];
            Assert.That(progress.ActiveNodeGuids.Count, Is.EqualTo(count));

            Assert.That(QuestManager.SetQuestState(controller, targetContainer.QuestId, QuestState.TurnedIn), Is.True);

            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.ActiveNodeGuids,
                Is.EquivalentTo(Enumerable.Range(0, count).Select(i => $"after-{i}")));
            Assert.That(progress.CompletedNodeGuids.Count(guid => guid.StartsWith("wait-")), Is.EqualTo(count));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StateChange_ProcessesWaitsBeforeGameNotificationRestartsTarget(bool completeThroughNode)
        {
            QuestContainer targetContainer = CreateObjectiveQuest(2);
            QuestContainer waitingContainer = CreateWaitingQuest(1, targetContainer.QuestId, QuestState.TurnedIn);
            QuestManager.Initialize(new[] { waitingContainer, targetContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);
            controller.ProgressChanged = (_, progress) =>
            {
                if (progress.questId == targetContainer.QuestId && progress.state == QuestState.TurnedIn)
                {
                    Assert.That(QuestManager.ResetQuest(controller, targetContainer.QuestId), Is.True);
                    Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
                }
            };

            bool result = completeThroughNode
                ? QuestManager.ProcessObjectiveByGuid(controller, targetContainer.QuestId, "objective")
                : QuestManager.SetQuestState(controller, targetContainer.QuestId, QuestState.TurnedIn);

            Assert.That(result, Is.True);
            Assert.That(controller.QuestProgress[targetContainer.QuestId].state, Is.EqualTo(QuestState.InProgress));
            Assert.That(controller.QuestProgress[waitingContainer.QuestId].ActiveNodeGuids,
                Is.EqualTo(new[] { "after-wait" }));
            Assert.That(controller.ActionCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_ProcessesNotStartedWaitsBeforeGameNotificationRestartsTarget()
        {
            QuestContainer targetContainer = CreateObjectiveQuest(2);
            QuestContainer waitingContainer = CreateWaitingQuest(1, targetContainer.QuestId, QuestState.NotStarted);
            QuestManager.Initialize(new[] { waitingContainer, targetContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);
            controller.ProgressChanged = (_, progress) =>
            {
                if (progress.questId == targetContainer.QuestId && progress.state == QuestState.NotStarted)
                {
                    Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
                }
            };

            Assert.That(QuestManager.ResetQuest(controller, targetContainer.QuestId), Is.True);

            Assert.That(controller.QuestProgress[targetContainer.QuestId].state, Is.EqualTo(QuestState.InProgress));
            Assert.That(controller.QuestProgress[waitingContainer.QuestId].ActiveNodeGuids,
                Is.EqualTo(new[] { "after-wait" }));
            Assert.That(controller.ActionCount, Is.EqualTo(1));
        }

        [TestCase(QuestState.NotStarted, false)]
        [TestCase(QuestState.NotStarted, true)]
        [TestCase(QuestState.TurnedIn, false)]
        [TestCase(QuestState.TurnedIn, true)]
        public void StateChange_DoesNotNotifyOldRunAfterDependentActionChangesIt(
            QuestState state, bool replaceProgress)
        {
            QuestContainer targetContainer = CreateObjectiveQuest(2);
            QuestContainer waitingContainer = CreateWaitingQuest(1, targetContainer.QuestId, state);
            QuestManager.Initialize(new[] { waitingContainer, targetContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);
            QuestProgress original = controller.QuestProgress[targetContainer.QuestId];
            var replacement = new QuestProgress(targetContainer) { state = QuestState.CanComplete };
            controller.Notifications.Clear();
            controller.QuestAction = () =>
            {
                if (replaceProgress)
                {
                    controller.QuestProgress[targetContainer.QuestId] = replacement;
                }
                else
                {
                    Assert.That(QuestManager.ResetQuest(controller, targetContainer.QuestId), Is.True);
                    Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
                }
            };

            Assert.That(QuestManager.SetQuestState(controller, targetContainer.QuestId, state), Is.True);

            if (replaceProgress)
            {
                Assert.That(controller.QuestProgress[targetContainer.QuestId], Is.SameAs(replacement));
                Assert.That(controller.Notifications.Any(item => ReferenceEquals(item.Progress, original)), Is.False);
            }
            else
            {
                Assert.That(controller.QuestProgress[targetContainer.QuestId].state, Is.EqualTo(QuestState.InProgress));
                Assert.That(controller.Notifications.Where(item => item.QuestId == targetContainer.QuestId)
                    .Select(item => item.State), Is.EqualTo(new[] { QuestState.NotStarted, QuestState.InProgress }));
            }
            Assert.That(controller.QuestProgress[waitingContainer.QuestId].ActiveNodeGuids,
                Is.EqualTo(new[] { "after-wait" }));
            Assert.That(controller.ActionCount, Is.EqualTo(1));
        }

        [Test]
        public void ObjectiveProgress_NotifiesGameWithoutCompletingStateWaits()
        {
            QuestContainer targetContainer = CreateObjectiveQuest(2);
            targetContainer.Nodes.OfType<QuestObjectiveNodeData>().Single().RequiredAmount = 2;
            QuestContainer waitingContainer = CreateWaitingQuest(1, targetContainer.QuestId, QuestState.TurnedIn);
            QuestManager.Initialize(new[] { waitingContainer, targetContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, targetContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);
            controller.Notifications.Clear();

            Assert.That(QuestManager.ProcessObjectiveByGuid(controller, targetContainer.QuestId, "objective"), Is.True);

            QuestProgress progress = controller.QuestProgress[targetContainer.QuestId];
            Assert.That(progress.ObjectiveAmounts["objective"], Is.EqualTo(1));
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(controller.Notifications, Has.Count.EqualTo(1));
            Assert.That(controller.Notifications[0].Progress, Is.SameAs(progress));
            Assert.That(controller.QuestProgress[waitingContainer.QuestId].ActiveNodeGuids,
                Is.EqualTo(new[] { "wait" }));
            Assert.That(controller.ActionCount, Is.Zero);
        }

        private QuestContainer CreateGraph(int questId)
        {
            var container = ScriptableObject.CreateInstance<QuestContainer>();
            container.QuestId = questId;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            createdContainers.Add(container);
            return container;
        }

        private QuestContainer CreateObjectiveQuest(int questId)
        {
            QuestContainer container = CreateGraph(questId);
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "objective", EventKey = "work" });
            container.Nodes.Add(new QuestFlowEndNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(container, "start", "objective");
            Connect(container, "objective", "end");
            return container;
        }

        private QuestContainer CreateWaitingQuest(int questId, int targetQuestId, QuestState requiredState)
        {
            QuestContainer container = CreateGraph(questId);
            container.Nodes.Add(new QuestStateWaitNodeData
            {
                Guid = "wait", TargetQuestId = targetQuestId, RequiredState = requiredState
            });
            container.Nodes.Add(new QuestActionNodeData
            {
                Guid = "action", Action = new MethodBindingData { Key = ResumeActionKey }
            });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "after-wait", EventKey = "after" });
            container.Nodes.Add(new QuestFlowEndNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(container, "start", "wait");
            Connect(container, "wait", "action");
            Connect(container, "action", "after-wait");
            Connect(container, "after-wait", "end");
            return container;
        }

        private static QuestSaveData CreateCompletedSave(QuestContainer targetContainer)
        {
            var source = new TestController();
            Assert.That(QuestManager.StartQuest(source, targetContainer.QuestId), Is.True);
            Assert.That(QuestManager.ProcessObjectiveByGuid(source, targetContainer.QuestId, "objective"), Is.True);
            return QuestManager.CaptureSaveData(source);
        }

        private static void Connect(QuestContainer container, string source, string target)
        {
            container.NodeLinks.Add(new NodeLinkData
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
            public Action<QuestContainer, QuestProgress> ProgressChanged;
            public Action QuestAction;
            public int ActionCount;

            [QuestAction(ResumeActionKey, Owner = QuestMethodOwner.Controller)]
            private void RecordResume()
            {
                ActionCount++;
                QuestAction?.Invoke();
            }

            public void OnQuestProgressChanged(QuestContainer container, QuestProgress progress)
            {
                Notifications.Add((progress.questId, progress.state, progress));
                ProgressChanged?.Invoke(container, progress);
            }
        }
    }
}
