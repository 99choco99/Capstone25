using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniversalGraph.Tests
{
    public sealed class QuestFlowRegressionTests
    {
        private const string FlowActionKey = "tests.quest.flow-action";
        private readonly List<QuestContainer> createdContainers = new();
        private static Action<QuestExecutionContext> flowAction;
        private IDictionary<string, QuestMethodDescriptor> actionRegistry;
        private QuestMethodDescriptor previousDescriptor;

        [SetUp]
        public void RegisterFlowAction()
        {
            // Editor 테스트 메서드는 운영 검색에서 제외되므로 이 테스트의 키만 직접 등록합니다.
            QuestMethodInvoker.Initialize();
            actionRegistry = (IDictionary<string, QuestMethodDescriptor>)typeof(QuestMethodInvoker)
                .GetField("actionRegistry", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            actionRegistry.TryGetValue(FlowActionKey, out previousDescriptor);
            actionRegistry.Remove(FlowActionKey);
            MethodInfo method = typeof(QuestFlowRegressionTests).GetMethod(nameof(InvokeFlowAction), BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(QuestMethodDescriptorFactory.CreateDescriptor(
                method, MethodKind.Action, FlowActionKey, QuestMethodOwner.Global,
                out QuestMethodDescriptor descriptor, out string error), Is.True, error);
            typeof(QuestMethodInvoker).GetMethod("RegisterDescriptor", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { descriptor });
        }

        [TearDown]
        public void TearDown()
        {
            flowAction = null;
            if (previousDescriptor == null)
            {
                actionRegistry.Remove(FlowActionKey);
            }
            else
            {
                actionRegistry[FlowActionKey] = previousDescriptor;
            }
            QuestManager.Initialize(Array.Empty<QuestContainer>());
            foreach (QuestContainer container in createdContainers)
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
            createdContainers.Clear();
        }

        [Test]
        public void Initialize_RegistersContainersInInputOrder()
        {
            QuestContainer firstContainer = CreateQuest();
            QuestContainer secondContainer = CreateQuest();
            var containers = new List<QuestContainer> { secondContainer, firstContainer };
            QuestManager.Initialize(containers);
            containers.Clear();

            Assert.Throws<NotSupportedException>(
                () => ((IList<QuestContainer>)QuestManager.RegisteredQuests).Clear());
            Assert.That(QuestManager.RegisteredQuests, Is.EqualTo(new[] { secondContainer, firstContainer }));
            Assert.That(QuestManager.GetQuest(firstContainer.QuestId, out QuestContainer foundContainer), Is.True);
            Assert.That(foundContainer, Is.SameAs(firstContainer));
            Assert.That(QuestManager.GetQuest(999, out QuestContainer missingContainer), Is.False);
            Assert.That(missingContainer, Is.Null);
        }

        [Test]
        public void Initialize_NullContainersLeaveThePreviousRegistrationIntact()
        {
            QuestContainer originalContainer = CreateQuest();
            QuestManager.Initialize(new[] { originalContainer });

            Assert.Throws<ArgumentNullException>(() => QuestManager.Initialize(null));

            Assert.That(QuestManager.RegisteredQuests, Is.EqualTo(new[] { originalContainer }));
            Assert.That(QuestManager.GetQuest(originalContainer.QuestId, out QuestContainer foundContainer), Is.True);
            Assert.That(foundContainer, Is.SameAs(originalContainer));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RegistryAccess_AfterStaticResetRequiresInitialization(bool hasProgress)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test" });
            Connect(container, "start", "goal");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            var progress = new QuestProgress(container);
            if (hasProgress)
            {
                controller.QuestProgress.Add(container.QuestId, progress);
            }

            // 플레이를 다시 시작할 때와 동일하게 등록부를 비웁니다.
            typeof(QuestManager).Assembly.GetType("UniversalGraph.QuestContainerRegistry")
                .GetMethod("ResetStaticState", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => QuestManager.GetQuest(container.QuestId, out _));
            Assert.That(error.Message, Does.Contain("QuestManager.Initialize"));
            Assert.Throws<InvalidOperationException>(() => { _ = QuestManager.RegisteredQuests; });
            Assert.Throws<InvalidOperationException>(() => QuestManager.StartQuest(controller, container.QuestId));
            Assert.Throws<InvalidOperationException>(() => QuestManager.GetCurrentObjectives(controller, container.QuestId));
            Assert.Throws<InvalidOperationException>(() => QuestManager.GetQuestSuggestions(controller, "npc"));
            Assert.Throws<InvalidOperationException>(() => QuestManager.GetDialogueCandidates(controller, "npc"));
            Assert.Throws<InvalidOperationException>(() => QuestManager.ReportObjectiveProgress(controller, "Test", 0, 1));
            Assert.Throws<InvalidOperationException>(() => QuestManager.AdvanceObjective(controller, container.QuestId, "goal"));
            Assert.Throws<InvalidOperationException>(() => QuestManager.ResetQuest(controller, container.QuestId));
            Assert.Throws<InvalidOperationException>(() => QuestManager.SetQuestState(controller, container.QuestId, QuestState.Failed));
            Assert.Throws<InvalidOperationException>(() => QuestManager.ResumeRestoredQuests(controller));
            Assert.Throws<InvalidOperationException>(() => QuestProgressSaveData.Capture(progress));
            Assert.Throws<InvalidOperationException>(() => new QuestSaveData().TryApplyTo(controller, true, out _));
            Assert.That(controller.QuestProgress.Count, Is.EqualTo(hasProgress ? 1 : 0));
            if (hasProgress)
            {
                Assert.That(controller.QuestProgress[container.QuestId], Is.SameAs(progress));
            }
            Assert.That(progress.state, Is.EqualTo(QuestState.NotStarted));

            QuestManager.Initialize(new[] { container });
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            Assert.That(controller.QuestProgress[container.QuestId].ActiveNodeGuids, Is.EqualTo(new[] { "goal" }));
        }

        [Test]
        public void Initialize_EmptyRegistryAllowsMissingQuestQueries()
        {
            QuestManager.Initialize(Array.Empty<QuestContainer>());
            var controller = new TestController();

            Assert.That(QuestManager.RegisteredQuests, Is.Empty);
            Assert.That(QuestManager.GetQuest(999, out QuestContainer container), Is.False);
            Assert.That(container, Is.Null);
            Assert.That(QuestManager.StartQuest(controller, 999), Is.False);
            Assert.That(controller.QuestProgress, Is.Empty);
        }

        [TestCase("invalid-id")]
        [TestCase("duplicate-id")]
        [TestCase("broken-link")]
        [TestCase("duplicate-link")]
        public void Initialize_InvalidContainersLeaveThePreviousRegistrationAndIndexIntact(string failure)
        {
            QuestContainer originalContainer = CreateQuest();
            originalContainer.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            originalContainer.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test" });
            Connect(originalContainer, "start", "goal");
            QuestManager.Initialize(new[] { originalContainer });
            QuestContainer replacementContainer = CreateQuest();
            QuestContainer invalidContainer = CreateQuest();
            if (failure == "broken-link")
            {
                invalidContainer.Nodes.Add(new QuestStartNodeData { Guid = "start" });
                Connect(invalidContainer, "start", "missing");
            }
            else if (failure == "duplicate-link")
            {
                invalidContainer.Nodes.Add(new QuestStartNodeData { Guid = "start" });
                invalidContainer.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test" });
                Connect(invalidContainer, "start", "goal");
                Connect(invalidContainer, "start", "goal");
            }
            else
            {
                invalidContainer.QuestId = failure == "invalid-id" ? 0 : replacementContainer.QuestId;
            }

            Assert.Throws<InvalidOperationException>(() => QuestManager.Initialize(new[] { replacementContainer, invalidContainer }));

            Assert.That(QuestManager.RegisteredQuests, Is.EqualTo(new[] { originalContainer }));
            Assert.That(QuestManager.GetQuest(originalContainer.QuestId, out QuestContainer foundContainer), Is.True);
            Assert.That(foundContainer, Is.SameAs(originalContainer));
            Assert.That(QuestManager.GetQuest(replacementContainer.QuestId, out _), Is.False);
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, originalContainer.QuestId), Is.True);
            Assert.That(controller.QuestProgress[originalContainer.QuestId].ActiveNodeGuids, Is.EqualTo(new[] { "goal" }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void Initialize_InvalidObjectiveAmountRejectsRegistrationWithoutChangingData(int requiredAmount)
        {
            QuestContainer originalContainer = CreateQuest();
            QuestManager.Initialize(new[] { originalContainer });
            QuestContainer invalidContainer = CreateQuest();
            var objective = new QuestObjectiveNodeData { Guid = "goal", RequiredAmount = requiredAmount };
            invalidContainer.Nodes.Add(objective);

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => QuestManager.Initialize(new[] { invalidContainer }));

            Assert.That(error.Message, Does.Contain("goal"));
            Assert.That(error.Message, Does.Contain("1 이상"));
            Assert.That(objective.RequiredAmount, Is.EqualTo(requiredAmount));
            Assert.That(QuestManager.RegisteredQuests, Is.EqualTo(new[] { originalContainer }));
            Assert.That(QuestManager.GetQuest(invalidContainer.QuestId, out _), Is.False);
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void Objective_PreservesRequiredAmountAndCapsProgressWithoutOverflow(int requiredAmount)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", RequiredAmount = requiredAmount });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "complete", NewState = QuestState.CanComplete });
            Connect(container, "start", "goal");
            Connect(container, "goal", "complete");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            QuestObjectiveProgress objective = QuestManager.GetCurrentObjectives(controller, container.QuestId)[0];
            Assert.That(objective.RequiredAmount, Is.EqualTo(requiredAmount));
            Assert.That(objective.CurrentAmount, Is.Zero);
            if (requiredAmount > 1)
            {
                Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "goal"), Is.True);
                Assert.That(QuestManager.GetCurrentObjectives(controller, container.QuestId)[0].CurrentAmount, Is.EqualTo(1));
            }

            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "goal", int.MaxValue), Is.True);
            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.NodeProgressCounts["goal"], Is.EqualTo(requiredAmount));
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
            AssertSaveRoundTrip(controller, progress);
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(999)]
        public void StartQuest_UnregisteredIdsDoNotCreateProgress(int questId)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();

            Assert.That(QuestManager.StartQuest(controller, questId), Is.False);
            Assert.That(controller.QuestProgress, Is.Empty);
        }

        [TestCase(QuestState.InProgress, false)]
        [TestCase(QuestState.CanComplete, false)]
        [TestCase(QuestState.TurnedIn, false)]
        [TestCase(QuestState.Failed, false)]
        [TestCase(QuestState.ExecutionError, false)]
        [TestCase(QuestState.InProgress, true)]
        [TestCase(QuestState.CanComplete, true)]
        [TestCase(QuestState.TurnedIn, true)]
        [TestCase(QuestState.Failed, true)]
        [TestCase(QuestState.ExecutionError, true)]
        public void StartQuest_ExistingStatesRequireResetBeforeStarting(QuestState state, bool acceptFromGraph)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Collect", RequiredAmount = 3 });
            container.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc" });
            container.Nodes.Add(new QuestSuggestionNodeData { Guid = "option" });
            Connect(container, "start", "goal");
            Connect(container, "entry", "option");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            var progress = new QuestProgress(container) { state = state };
            progress.ActiveNodeGuids.Add("goal");
            progress.NodeProgressCounts.Add("goal", 2);
            progress.CompletedNodeGuids.Add("previous-action");
            progress.CompletedGateInputs.Add("gate|previous-branch");
            controller.QuestProgress.Add(container.QuestId, progress);
            QuestSuggestion suggestion = QuestManager.GetQuestSuggestions(controller, "npc")[0];
            Assert.That(suggestion.IsAvailable, Is.False);

            // 시작 요청만으로 기존 진행 기록이나 완료 기록이 사라지지 않아야 합니다.
            bool started = acceptFromGraph
                ? QuestManager.AcceptQuest(controller, suggestion)
                : QuestManager.StartQuest(controller, container.QuestId);

            Assert.That(started, Is.False);
            Assert.That(controller.QuestProgress[container.QuestId], Is.SameAs(progress));
            Assert.That(progress.state, Is.EqualTo(state));
            Assert.That(progress.ActiveNodeGuids, Is.EqualTo(new[] { "goal" }));
            Assert.That(progress.NodeProgressCounts.Count, Is.EqualTo(1));
            Assert.That(progress.NodeProgressCounts["goal"], Is.EqualTo(2));
            Assert.That(progress.CompletedNodeGuids, Is.EqualTo(new[] { "previous-action" }));
            Assert.That(progress.CompletedGateInputs, Is.EqualTo(new[] { "gate|previous-branch" }));

            // 재시작 의도가 명확한 초기화 이후에는 같은 API로 다시 시작할 수 있습니다.
            Assert.That(QuestManager.ResetQuest(controller, container.QuestId), Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.NotStarted));
            Assert.That(progress.ActiveNodeGuids, Is.Empty);
            Assert.That(progress.NodeProgressCounts, Is.Empty);
            Assert.That(progress.CompletedNodeGuids, Is.Empty);
            Assert.That(progress.CompletedGateInputs, Is.Empty);

            started = acceptFromGraph
                ? QuestManager.AcceptQuest(controller, suggestion)
                : QuestManager.StartQuest(controller, container.QuestId);

            Assert.That(started, Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.ActiveNodeGuids, Is.EqualTo(new[] { "goal" }));
            Assert.That(progress.NodeProgressCounts["goal"], Is.Zero);
            Assert.That(progress.CompletedNodeGuids, Is.Empty);
            Assert.That(progress.CompletedGateInputs, Is.Empty);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void GetQuestSuggestions_NotStartedPreservesAuthoredAvailability(bool isAvailable, bool hasProgress)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc" });
            var suggestionNodeData = new QuestSuggestionNodeData
            {
                Guid = "option",
                IsAvailable = isAvailable,
                BlockReason = "작성한 차단 이유"
            };
            container.Nodes.Add(suggestionNodeData);
            Connect(container, "entry", "option");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            if (hasProgress)
            {
                controller.QuestProgress.Add(container.QuestId, new QuestProgress(container));
            }

            QuestSuggestion[] suggestions = QuestManager.GetQuestSuggestions(controller, "npc");

            Assert.That(suggestions, Has.Length.EqualTo(1));
            Assert.That(suggestions[0].IsAvailable, Is.EqualTo(isAvailable));
            Assert.That(suggestions[0].BlockReason, Is.EqualTo(suggestionNodeData.BlockReason));
            Assert.That(suggestionNodeData.IsAvailable, Is.EqualTo(isAvailable));
            Assert.That(controller.QuestProgress.Count, Is.EqualTo(hasProgress ? 1 : 0));
            if (hasProgress)
            {
                Assert.That(controller.QuestProgress[container.QuestId].state, Is.EqualTo(QuestState.NotStarted));
            }
        }

        [TestCase(QuestState.InProgress)]
        [TestCase(QuestState.CanComplete)]
        [TestCase(QuestState.TurnedIn)]
        [TestCase(QuestState.Failed)]
        [TestCase(QuestState.ExecutionError)]
        public void TryAcceptQuest_RechecksOwnStateAfterSuggestionWasShown(QuestState state)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Collect" });
            container.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc" });
            var suggestionNodeData = new QuestSuggestionNodeData { Guid = "option" };
            container.Nodes.Add(suggestionNodeData);
            Connect(container, "start", "goal");
            Connect(container, "entry", "option");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            QuestSuggestion shownSuggestion = QuestManager.GetQuestSuggestions(controller, "npc")[0];
            Assert.That(shownSuggestion.IsAvailable, Is.True);

            var progress = new QuestProgress(container) { state = state };
            controller.QuestProgress.Add(container.QuestId, progress);
            QuestSuggestion[] currentSuggestions = QuestManager.GetQuestSuggestions(controller, "npc");

            Assert.That(currentSuggestions, Has.Length.EqualTo(1));
            Assert.That(currentSuggestions[0].IsAvailable, Is.False);
            Assert.That(suggestionNodeData.IsAvailable, Is.True);
            Assert.That(QuestManager.AcceptQuest(controller, shownSuggestion), Is.False);
            Assert.That(controller.QuestProgress[container.QuestId], Is.SameAs(progress));
            Assert.That(progress.state, Is.EqualTo(state));
        }

        [Test]
        public void TryAcceptQuest_RejectsSuggestionsFromRemovedOrReplacedContainer()
        {
            QuestContainer originalContainer = CreateQuest();
            QuestContainer replacementContainer = CreateQuest();
            replacementContainer.QuestId = originalContainer.QuestId;
            foreach (QuestContainer container in new[] { originalContainer, replacementContainer })
            {
                container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
                container.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Collect" });
                container.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc" });
                container.Nodes.Add(new QuestSuggestionNodeData { Guid = "option" });
                Connect(container, "start", "goal");
                Connect(container, "entry", "option");
            }
            QuestManager.Initialize(new[] { originalContainer });
            var controller = new TestController();
            QuestSuggestion originalSuggestion = QuestManager.GetQuestSuggestions(controller, "npc")[0];

            QuestManager.Initialize(Array.Empty<QuestContainer>());
            Assert.That(QuestManager.AcceptQuest(controller, originalSuggestion), Is.False);
            Assert.That(controller.QuestProgress, Is.Empty);

            QuestManager.Initialize(new[] { replacementContainer });
            Assert.That(QuestManager.AcceptQuest(controller, originalSuggestion), Is.False);
            Assert.That(controller.QuestProgress, Is.Empty);

            QuestSuggestion replacementSuggestion = QuestManager.GetQuestSuggestions(controller, "npc")[0];
            Assert.That(replacementSuggestion.Container, Is.SameAs(replacementContainer));
            Assert.That(QuestManager.AcceptQuest(controller, replacementSuggestion), Is.True);
        }

        [Test]
        public void GetCurrentObjectives_EmptyProgressReturnsNoObjectives()
        {
            QuestContainer container = CreateQuest();
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            controller.QuestProgress.Add(container.QuestId, new QuestProgress(container));

            Assert.That(QuestManager.GetCurrentObjectives(controller, container.QuestId), Is.Empty);
            Assert.That(controller.QuestProgress[container.QuestId].ActiveNodeGuids, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InteractionQueries_ExposeMatchingStringAndEnumerableOverloads(bool multipleTargets)
        {
            QuestContainer container = CreateQuest();
            DialogueContainer dialogue = ScriptableObject.CreateInstance<DialogueContainer>();
            try
            {
                container.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc" });
                container.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "other-entry", TargetId = "other" });
                container.Nodes.Add(new QuestSuggestionNodeData { Guid = "quest-candidate", Priority = 3 });
                container.Nodes.Add(new DialogueCandidateNodeData
                {
                    Guid = "dialogue-candidate",
                    EntryPoint = new DialogueEntryPoint(dialogue, DialogueEntryNodeData.DefaultEntryId),
                    DisplayName = "Dialogue",
                    Priority = 5
                });
                Connect(container, "entry", "quest-candidate");
                Connect(container, "entry", "dialogue-candidate");
                Connect(container, "other-entry", "quest-candidate");
                Connect(container, "other-entry", "dialogue-candidate");
                QuestManager.Initialize(new[] { container });
                var controller = new TestController();
                IEnumerable<string> interactionTargetIds = new[] { "unmatched", "npc", "npc" };

                QuestSuggestion[] questSuggestions = multipleTargets
                    ? QuestManager.GetQuestSuggestions(controller, interactionTargetIds)
                    : QuestManager.GetQuestSuggestions(controller, "npc");
                DialogueCandidateNodeData[] dialogueCandidates = multipleTargets
                    ? QuestManager.GetDialogueCandidates(controller, interactionTargetIds)
                    : QuestManager.GetDialogueCandidates(controller, "npc");

                Assert.That(questSuggestions, Has.Length.EqualTo(1));
                Assert.That(questSuggestions[0].QuestId, Is.EqualTo(container.QuestId));
                Assert.That(questSuggestions[0].Priority, Is.EqualTo(3));
                Assert.That(dialogueCandidates, Has.Length.EqualTo(1));
                Assert.That(dialogueCandidates[0], Is.SameAs(container.Nodes.Find(nodeData => nodeData.Guid == "dialogue-candidate")));
                Assert.That(dialogueCandidates[0].EntryPoint.Container, Is.SameAs(dialogue));
                Assert.That(dialogueCandidates[0].DisplayName, Is.EqualTo("Dialogue"));
                Assert.That(dialogueCandidates[0].Priority, Is.EqualTo(5));
                Assert.That(controller.QuestProgress, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dialogue);
            }
        }

        [Test]
        public void QuestIdsAndTargetIds_ReferToDifferentObjects()
        {
            QuestContainer container = CreateQuest();
            container.QuestId = 101;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "objective",
                EventKey = "Collect",
                TargetId = 7,
                RequiredAmount = 2
            });
            container.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "village-chief" });
            container.Nodes.Add(new QuestSuggestionNodeData { Guid = "option" });
            Connect(container, "start", "objective");
            Connect(container, "entry", "option");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();

            QuestSuggestion[] questSuggestions = QuestManager.GetQuestSuggestions(controller, interactionTargetId: "village-chief");
            Assert.That(questSuggestions, Has.Length.EqualTo(1));
            Assert.That(questSuggestions[0].QuestId, Is.EqualTo(101));
            Assert.That(QuestManager.GetQuestSuggestions(controller, interactionTargetId: "101"), Is.Empty);
            Assert.That(controller.QuestProgress, Is.Empty);
            Assert.That(QuestManager.StartQuest(controller, questId: 7), Is.False);
            Assert.That(QuestManager.AcceptQuest(controller, suggestion: questSuggestions[0]), Is.True);

            QuestManager.ReportObjectiveProgress(controller, "Collect", objectiveTargetId: 101, amount: 1);
            Assert.That(controller.QuestProgress[101].NodeProgressCounts["objective"], Is.Zero);
            QuestManager.ReportObjectiveProgress(controller, "Collect", objectiveTargetId: 7, amount: 1);
            Assert.That(controller.QuestProgress[101].NodeProgressCounts["objective"], Is.EqualTo(1));
        }

        [Test]
        public void MissingControllerErrors_ExplainWhichInterfaceToImplement()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => QuestManager.StartQuest(null, 1));
            Assert.That(exception.ParamName, Is.EqualTo("controller"));
            Assert.That(exception.Message, Does.Contain("IQuestController를 구현한 객체"));

            exception = Assert.Throws<ArgumentNullException>(() => QuestManager.GetQuestSuggestions(null, "npc"));
            Assert.That(exception.Message, Does.Contain("IQuestController를 구현한 객체"));

            exception = Assert.Throws<ArgumentNullException>(() => QuestSaveData.Capture(null));
            Assert.That(exception.Message, Does.Contain("IQuestController를 구현한 객체"));
        }

        [TestCase(8, 2)]
        [TestCase(2, 20)]
        public void ConvergingRewardBranches_DoNotMultiplyImmediateSteps(int layers, int width)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            string[] previous = { "start" };
            for (int layer = 0; layer < layers; layer++)
            {
                var current = new string[width];
                for (int branch = 0; branch < width; branch++)
                {
                    string guid = $"reward-{layer}-{branch}";
                    current[branch] = guid;
                    container.Nodes.Add(new QuestRewardNodeData { Guid = guid });
                    foreach (string source in previous)
                    {
                        Connect(container, source, guid);
                    }
                }
                previous = current;
            }
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test", RequiredAmount = 1 });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            foreach (string source in previous)
            {
                Connect(container, source, "goal");
            }
            Connect(container, "goal", "end");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();

            Assert.That(container.Nodes, Has.Count.EqualTo(layers * width + 3));
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.ActiveNodeGuids, Is.EqualTo(new[] { "goal" }));
            Assert.That(progress.CompletedNodeGuids, Has.Count.EqualTo(layers * width));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedAction_ReentrantObjectiveCompletionDoesNotExecuteItTwice(bool useReward)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "first", RequiredAmount = 1 });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "second", RequiredAmount = 1 });
            var binding = new MethodBindingData { Key = FlowActionKey };
            container.Nodes.Add(useReward
                ? new QuestRewardNodeData { Guid = "shared", RewardAction = binding }
                : new QuestActionNodeData { Guid = "shared", Action = binding });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "after", RequiredAmount = 1 });
            Connect(container, "start", "first");
            Connect(container, "start", "second");
            Connect(container, "first", "shared");
            Connect(container, "second", "shared");
            Connect(container, "shared", "after");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            int actionCount = 0;
            flowAction = context =>
            {
                actionCount++;
                QuestManager.AdvanceObjective(context.Controller, container.QuestId, "second");
            };
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "first"), Is.True);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(actionCount, Is.EqualTo(1));
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.ActiveNodeGuids, Is.EqualTo(new[] { "after" }));
            Assert.That(progress.CompletedNodeGuids, Is.EquivalentTo(new[] { "first", "second", "shared" }));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void ActionNodes_RejectMissingRequiredBinding(bool useReward, bool nullBinding)
        {
            QuestContainer container = CreateQuest();
            MethodBindingData binding = nullBinding ? null : new MethodBindingData();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(useReward
                ? new QuestRewardNodeData { Guid = "action", RewardAction = binding }
                : new QuestActionNodeData { Guid = "action", Action = binding });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "next", RequiredAmount = 1 });
            Connect(container, "start", "action");
            Connect(container, "action", "next");
            if (nullBinding)
            {
                //호출 정보 자체가 없으면 실행 전에 그래프 초기화 단계에서 거부합니다.
                Assert.Throws<InvalidOperationException>(() => QuestManager.Initialize(new[] { container }));
                return;
            }

            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            LogAssert.Expect(LogType.Error, "[Quest] Action 키가 비어 있습니다.");

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.False);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.ExecutionError));
            Assert.That(progress.CompletedNodeGuids, Does.Not.Contain("action"));
            Assert.That(progress.ActiveNodeGuids, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActionInvocationFailure_DoesNotLeaveTheNodeCompleted(bool useReward)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            var binding = new MethodBindingData { Key = FlowActionKey };
            container.Nodes.Add(useReward
                ? new QuestRewardNodeData { Guid = "action", RewardAction = binding }
                : new QuestActionNodeData { Guid = "action", Action = binding });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "after", RequiredAmount = 1 });
            Connect(container, "start", "action");
            Connect(container, "action", "after");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            flowAction = _ => throw new InvalidOperationException("Expected flow action failure.");
            LogAssert.Expect(LogType.Error, new Regex($"메서드 '{Regex.Escape(FlowActionKey)}' 실행 중 예외가 발생했습니다"));

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.False);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.ExecutionError));
            Assert.That(progress.ActiveNodeGuids, Is.Empty);
            Assert.That(progress.CompletedNodeGuids, Does.Not.Contain("action"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Condition_MultipleOutputsStartObjectivesAndRestoreBeforeAndGateCompletes(bool result)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestStateConditionNodeData
            {
                Guid = "condition",
                QuestId = container.QuestId,
                TargetState = result ? QuestState.InProgress : QuestState.NotStarted
            });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "first", EventKey = "First", RequiredAmount = 1 });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "second", EventKey = "Second", RequiredAmount = 2 });
            container.Nodes.Add(new QuestAndGateNodeData { Guid = "gate" });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "fail", NewState = QuestState.Failed });
            Connect(container, "start", "condition");
            string selectedPort = result ? QuestPortNames.True : QuestPortNames.False;
            Connect(container, "condition", "first", selectedPort);
            Connect(container, "condition", "second", selectedPort);
            Connect(container, "condition", "fail", result ? QuestPortNames.False : QuestPortNames.True);
            Connect(container, "first", "gate");
            Connect(container, "second", "gate");
            Connect(container, "gate", "end");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.ActiveNodeGuids, Is.EquivalentTo(new[] { "first", "second" }));
            Assert.That(progress.CompletedNodeGuids, Is.Empty);
            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "first"), Is.True);
            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "second"), Is.True);
            Assert.That(progress.CompletedGateInputs, Is.EqualTo(new[] { "gate|first" }));
            Assert.That(progress.CompletedNodeGuids, Does.Not.Contain("gate"));

            string json = QuestSaveData.Capture(controller).ToJson();
            Assert.That(QuestSaveData.TryFromJson(json, out QuestSaveData saved, out string parseError), Is.True, parseError);
            var restoredController = new TestController();
            Assert.That(saved.TryApplyTo(restoredController, replaceExisting: true, out string restoreError), Is.True, restoreError);
            QuestProgress restored = restoredController.QuestProgress[container.QuestId];
            Assert.That(restored.CompletedNodeGuids, Is.EqualTo(new[] { "first" }));
            Assert.That(restored.CompletedGateInputs, Is.EqualTo(new[] { "gate|first" }));
            Assert.That(restored.NodeProgressCounts["second"], Is.EqualTo(1));
            Assert.That(restored.ActiveNodeGuids, Is.EquivalentTo(progress.ActiveNodeGuids));

            Assert.That(QuestManager.AdvanceObjective(restoredController, container.QuestId, "second"), Is.True);
            Assert.That(restored.state, Is.EqualTo(QuestState.TurnedIn));
            Assert.That(restored.ActiveNodeGuids, Is.Empty);
            Assert.That(restored.CompletedNodeGuids, Is.EquivalentTo(new[] { "first", "second", "gate", "end" }));
            AssertSaveRoundTrip(restoredController, restored);
        }

        [Test]
        public void AndGate_WithOneIncomingLinkContinuesImmediately()
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestAndGateNodeData { Guid = "gate" });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "complete", NewState = QuestState.CanComplete });
            Connect(container, "start", "gate");
            Connect(container, "gate", "complete");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
            Assert.That(progress.CompletedNodeGuids, Does.Contain("gate"));
            Assert.That(progress.CompletedGateInputs, Is.EqualTo(new[] { "gate|start" }));
        }

        [Test]
        public void AndGate_RecordsEveryDistinctArrivalBeforeContinuing()
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "first", EventKey = "Test", RequiredAmount = 1 });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "second", EventKey = "Test", RequiredAmount = 1 });
            container.Nodes.Add(new QuestAndGateNodeData { Guid = "gate" });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(container, "start", "first");
            Connect(container, "start", "second");
            Connect(container, "first", "gate");
            Connect(container, "second", "gate");
            Connect(container, "gate", "end");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "first"), Is.True);
            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.CompletedGateInputs, Is.EqualTo(new[] { "gate|first" }));
            Assert.That(progress.CompletedNodeGuids, Does.Not.Contain("gate"));

            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "second"), Is.True);
            Assert.That(progress.CompletedGateInputs, Is.EquivalentTo(new[] { "gate|first", "gate|second" }));
            Assert.That(progress.CompletedNodeGuids, Does.Contain("gate"));
            Assert.That(progress.state, Is.EqualTo(QuestState.TurnedIn));
        }

        [TestCase(QuestState.InProgress)]
        [TestCase(QuestState.NotStarted)]
        public void AndGate_CountsDifferentPortsFromTheSameNodeAsOneSource(QuestState targetState)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestStateConditionNodeData
            {
                Guid = "condition",
                QuestId = container.QuestId,
                TargetState = targetState
            });
            container.Nodes.Add(new QuestAndGateNodeData { Guid = "gate" });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(container, "start", "condition");
            Connect(container, "condition", "gate", QuestPortNames.True);
            Connect(container, "condition", "gate", QuestPortNames.False);
            Connect(container, "gate", "end");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.CompletedGateInputs, Is.EqualTo(new[] { "gate|condition" }));
            Assert.That(progress.CompletedNodeGuids, Does.Contain("gate"));
            Assert.That(progress.state, Is.EqualTo(QuestState.TurnedIn));
        }

        [TestCase("none")]
        [TestCase("reward")]
        public void SharedCondition_ReevaluatesUnlessItsOneShotPredecessorAlreadyCompleted(string predecessor)
        {
            QuestContainer container = CreateQuest();
            QuestContainer inspectedContainer = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "first", EventKey = "Test", RequiredAmount = 1 });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "second", EventKey = "Test", RequiredAmount = 1 });
            container.Nodes.Add(new QuestStateConditionNodeData
            {
                Guid = "condition",
                QuestId = inspectedContainer.QuestId,
                TargetState = QuestState.NotStarted
            });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "true-goal", EventKey = "Test", RequiredAmount = 1 });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "false-goal", EventKey = "Test", RequiredAmount = 1 });
            Connect(container, "start", "first");
            Connect(container, "start", "second");
            string target = "condition";
            if (predecessor != "none")
            {
                container.Nodes.Add(new QuestRewardNodeData { Guid = predecessor });
                Connect(container, predecessor, "condition");
                target = predecessor;
            }
            Connect(container, "first", target);
            Connect(container, "second", target);
            Connect(container, "condition", "true-goal", QuestPortNames.True);
            Connect(container, "condition", "false-goal", QuestPortNames.False);
            QuestManager.Initialize(new[] { container, inspectedContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "first"), Is.True);
            controller.QuestProgress[inspectedContainer.QuestId] = new QuestProgress(inspectedContainer) { state = QuestState.TurnedIn };

            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "second"), Is.True);
            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.CompletedNodeGuids, Does.Not.Contain("condition"));
            Assert.That(progress.ActiveNodeGuids, Does.Contain("true-goal"));
            Assert.That(progress.ActiveNodeGuids.Contains("false-goal"), Is.EqualTo(predecessor == "none"));
            if (predecessor != "none")
            {
                Assert.That(progress.CompletedNodeGuids, Does.Contain(predecessor));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ImmediateConditionCycle_StopsEvenIfAnotherObjectiveIsActive(bool parallelObjective)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestStateConditionNodeData
            {
                Guid = "cycle",
                QuestId = container.QuestId,
                TargetState = QuestState.InProgress
            });
            Connect(container, "start", "cycle");
            Connect(container, "cycle", "cycle", QuestPortNames.True);
            if (parallelObjective)
            {
                container.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test", RequiredAmount = 1 });
                Connect(container, "start", "goal");
            }
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            LogAssert.Expect(LogType.Error, new Regex("즉시 실행 단계가 256회를 초과"));

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.False);
            Assert.That(controller.QuestProgress[container.QuestId].state, Is.EqualTo(QuestState.ExecutionError));
            Assert.That(controller.QuestProgress[container.QuestId].ActiveNodeGuids, Is.Empty);
        }

        [TestCase(QuestState.Failed, false)]
        [TestCase(QuestState.Failed, true)]
        [TestCase(QuestState.TurnedIn, false)]
        [TestCase(QuestState.TurnedIn, true)]
        [TestCase(QuestState.CanComplete, false)]
        [TestCase(QuestState.CanComplete, true)]
        public void ExecutionError_StopsOnlyTheBrokenQuest(QuestState requiredState, bool waitAfterError)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestActionNodeData { Guid = "invalid-action" });
            Connect(container, "start", "invalid-action");
            QuestContainer waitingContainer = CreateQuest();
            waitingContainer.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            waitingContainer.Nodes.Add(new WaitForQuestNodeData
            {
                Guid = "wait",
                TargetQuestId = container.QuestId,
                RequiredState = requiredState
            });
            waitingContainer.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test", RequiredAmount = 1 });
            Connect(waitingContainer, "start", "wait");
            Connect(waitingContainer, "wait", "goal");
            QuestManager.Initialize(new[] { container, waitingContainer });
            var controller = new TestController();
            var notifications = new List<(int QuestId, QuestState State)>();
            controller.ProgressChanged = (_, progress) => notifications.Add((progress.questId, progress.state));
            if (!waitAfterError)
            {
                Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);
            }
            LogAssert.Expect(LogType.Error, "[Quest] Action 키가 비어 있습니다.");

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.False);
            Assert.That(controller.QuestProgress[container.QuestId].state, Is.EqualTo(QuestState.ExecutionError));
            Assert.That(controller.QuestProgress[container.QuestId].ActiveNodeGuids, Is.Empty);
            Assert.That(notifications, Does.Contain((container.QuestId, QuestState.ExecutionError)));
            if (waitAfterError)
            {
                Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);
            }

            // 상태 API와 복원 재개도 실행 오류를 대기 충족으로 취급하지 않습니다.
            Assert.That(QuestManager.SetQuestState(controller, container.QuestId, QuestState.ExecutionError), Is.True);
            QuestManager.ResumeRestoredQuests(controller);
            Assert.That(controller.QuestProgress[container.QuestId].state, Is.EqualTo(QuestState.ExecutionError));
            Assert.That(controller.QuestProgress[waitingContainer.QuestId].state, Is.EqualTo(QuestState.InProgress));
            Assert.That(controller.QuestProgress[waitingContainer.QuestId].ActiveNodeGuids,
                Is.EqualTo(new[] { "wait" }));
        }

        [Test]
        public void Initialize_RejectsWaitingForExecutionError()
        {
            QuestContainer originalContainer = CreateQuest();
            QuestManager.Initialize(new[] { originalContainer });
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new WaitForQuestNodeData
            {
                Guid = "invalid-wait",
                TargetQuestId = originalContainer.QuestId,
                RequiredState = QuestState.ExecutionError
            });

            var exception = Assert.Throws<InvalidOperationException>(() => QuestManager.Initialize(new[] { container }));

            Assert.That(exception.Message, Does.Contain("invalid-wait").And.Contain("ExecutionError"));
            Assert.That(QuestManager.RegisteredQuests, Is.EqualTo(new[] { originalContainer }));
        }

        [TestCase(QuestState.CanComplete)]
        [TestCase(QuestState.TurnedIn)]
        [TestCase(QuestState.Failed)]
        public void StateChange_StopsOtherObjectivesAndResumesDependentQuest(QuestState state)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "first", EventKey = "First" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "second", EventKey = "Second" });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = state });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "after", EventKey = "After" });
            Connect(container, "start", "first");
            Connect(container, "start", "second");
            Connect(container, "first", "end");
            // 잘못된 출력선이 남아 있어도 종료 노드 뒤로 진행하지 않습니다.
            Connect(container, "end", "after");

            QuestContainer waitingContainer = CreateQuest();
            waitingContainer.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            waitingContainer.Nodes.Add(new WaitForQuestNodeData
            {
                Guid = "wait",
                TargetQuestId = container.QuestId,
                RequiredState = state
            });
            waitingContainer.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Goal" });
            Connect(waitingContainer, "start", "wait");
            Connect(waitingContainer, "wait", "goal");
            QuestManager.Initialize(new[] { container, waitingContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, waitingContainer.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);

            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "first"), Is.True);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(state));
            Assert.That(progress.ActiveNodeGuids, Is.Empty);
            Assert.That(progress.CompletedNodeGuids, Does.Contain("end"));
            Assert.That(progress.CompletedNodeGuids, Does.Not.Contain("second"));
            Assert.That(progress.NodeProgressCounts.ContainsKey("after"), Is.False);
            Assert.That(controller.QuestProgress[waitingContainer.QuestId].ActiveNodeGuids,
                Is.EqualTo(new[] { "goal" }));
        }

        [TestCase(QuestState.NotStarted)]
        [TestCase(QuestState.InProgress)]
        [TestCase(QuestState.ExecutionError)]
        [TestCase((QuestState)999)]
        public void StateChange_InvalidStateStopsWithoutCompletingOrFollowingOutputs(QuestState state)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "invalid-state", NewState = state });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "after", EventKey = "Test", RequiredAmount = 1 });
            Connect(container, "start", "invalid-state");
            Connect(container, "invalid-state", "after");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            LogAssert.Expect(LogType.Error, new Regex("\\[Quest\\] State Change 노드는 상태를 .* 변경할 수 없습니다"));

            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.False);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.ExecutionError));
            Assert.That(progress.ActiveNodeGuids, Is.Empty);
            Assert.That(progress.CompletedNodeGuids, Does.Not.Contain("invalid-state"));
            Assert.That(progress.NodeProgressCounts.ContainsKey("after"), Is.False);
        }

        [TestCase(QuestState.Failed)]
        [TestCase(QuestState.CanComplete)]
        [TestCase(QuestState.TurnedIn)]
        [TestCase(QuestState.ExecutionError)]
        public void TerminalNode_RoundTripsPartiallyCompleteParallelObjective(QuestState terminalState)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "first", EventKey = "Test", RequiredAmount = 1 });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "second", EventKey = "Test", RequiredAmount = 3 });
            if (terminalState == QuestState.ExecutionError)
            {
                container.Nodes.Add(new QuestActionNodeData { Guid = "end" });
            }
            else
            {
                container.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = terminalState });
            }
            Connect(container, "start", "first");
            Connect(container, "start", "second");
            Connect(container, "first", "end");
            Connect(container, "second", "end");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "second", 2), Is.True);
            if (terminalState == QuestState.ExecutionError)
            {
                LogAssert.Expect(LogType.Error, "[Quest] Action 키가 비어 있습니다.");
            }

            Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "first"),
                Is.EqualTo(terminalState != QuestState.ExecutionError));

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(terminalState));
            Assert.That(progress.ActiveNodeGuids, Is.Empty);
            Assert.That(progress.NodeProgressCounts["first"], Is.EqualTo(1));
            Assert.That(progress.NodeProgressCounts["second"], Is.EqualTo(2));
            Assert.That(progress.CompletedNodeGuids, Does.Contain("first"));
            Assert.That(progress.CompletedNodeGuids, Does.Not.Contain("second"));
            AssertSaveRoundTrip(controller, progress);
        }

        [TestCase(QuestState.Failed, 0)]
        [TestCase(QuestState.Failed, 1)]
        [TestCase(QuestState.CanComplete, 0)]
        [TestCase(QuestState.CanComplete, 1)]
        [TestCase(QuestState.TurnedIn, 0)]
        [TestCase(QuestState.TurnedIn, 1)]
        [TestCase(QuestState.ExecutionError, 0)]
        [TestCase(QuestState.ExecutionError, 1)]
        public void SetQuestState_RoundTripsStoppedObjectiveCounters(QuestState terminalState, int count)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test", RequiredAmount = 3 });
            Connect(container, "start", "goal");
            QuestManager.Initialize(new[] { container });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            if (count > 0)
            {
                Assert.That(QuestManager.AdvanceObjective(controller, container.QuestId, "goal", count), Is.True);
            }

            Assert.That(QuestManager.SetQuestState(controller, container.QuestId, terminalState), Is.True);

            QuestProgress progress = controller.QuestProgress[container.QuestId];
            Assert.That(progress.state, Is.EqualTo(terminalState));
            Assert.That(progress.ActiveNodeGuids, Is.Empty);
            Assert.That(progress.NodeProgressCounts["goal"], Is.EqualTo(count));
            Assert.That(progress.CompletedNodeGuids, Is.Empty);
            AssertSaveRoundTrip(controller, progress);
        }

        [TestCase(QuestState.InProgress)]
        [TestCase(QuestState.NotStarted)]
        public void SaveData_RejectsOrphanObjectiveCountersOutsideTerminalStates(QuestState state)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test", RequiredAmount = 3 });
            container.Nodes.Add(new QuestObjectiveNodeData { Guid = "orphan", EventKey = "Test", RequiredAmount = 3 });
            Connect(container, "start", "goal");
            Connect(container, "start", "orphan");
            QuestManager.Initialize(new[] { container });
            var progress = new QuestProgress(container) { state = state };
            if (state == QuestState.InProgress)
            {
                progress.ActiveNodeGuids.Add("goal");
            }
            progress.NodeProgressCounts.Add("orphan", 1);
            var source = new TestController();
            source.QuestProgress.Add(container.QuestId, progress);
            var saved = new QuestSaveData
            {
                quests = new List<QuestProgressSaveData>
                {
                    new()
                    {
                        questId = container.QuestId,
                        definitionSchemaVersion = container.SchemaVersion,
                        state = state,
                        activeNodeGuids = new List<string>(progress.ActiveNodeGuids),
                        nodeProgressCounts = new List<QuestNodeProgressSaveData>
                        {
                            new() { nodeGuid = "orphan", count = 1 }
                        }
                    }
                }
            };
            var target = new TestController();
            var original = new QuestProgress(container);
            target.QuestProgress.Add(container.QuestId, original);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => QuestSaveData.Capture(source));
            Assert.That(saved.TryApplyTo(target, replaceExisting: true, out string error), Is.False);

            Assert.That(exception.Message, Is.EqualTo(error));
            Assert.That(error, Does.Contain(state == QuestState.InProgress
                ? "활성 또는 완료 기록과 연결되어 있지 않습니다"
                : "NotStarted이지만 이전 진행 기록이 남아 있습니다"));
            Assert.That(target.QuestProgress[container.QuestId], Is.SameAs(original));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReportObjectiveProgress_DoesNotCountTheEventThatActivatedAnotherQuestObjective(bool waitingQuestFirst)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "goal", EventKey = "Kill", TargetId = 7, RequiredAmount = 1
            });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(container, "start", "goal");
            Connect(container, "goal", "end");
            QuestContainer waitingContainer = CreateQuest();
            waitingContainer.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            waitingContainer.Nodes.Add(new WaitForQuestNodeData
            {
                Guid = "wait", TargetQuestId = container.QuestId, RequiredState = QuestState.TurnedIn
            });
            waitingContainer.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "goal", EventKey = "Kill", TargetId = 7, RequiredAmount = 1
            });
            waitingContainer.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(waitingContainer, "start", "wait");
            Connect(waitingContainer, "wait", "goal");
            Connect(waitingContainer, "goal", "end");
            QuestManager.Initialize(new[] { container, waitingContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller,
                waitingQuestFirst ? waitingContainer.QuestId : container.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller,
                waitingQuestFirst ? container.QuestId : waitingContainer.QuestId), Is.True);
            QuestProgress waitingProgress = controller.QuestProgress[waitingContainer.QuestId];
            Assert.That(waitingProgress.ActiveNodeGuids, Is.EqualTo(new[] { "wait" }));

            QuestManager.ReportObjectiveProgress(controller, "Kill", 7, 1);

            Assert.That(controller.QuestProgress[container.QuestId].state, Is.EqualTo(QuestState.TurnedIn));
            Assert.That(waitingProgress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(waitingProgress.ActiveNodeGuids, Is.EqualTo(new[] { "goal" }));
            Assert.That(waitingProgress.NodeProgressCounts["goal"], Is.Zero);
            QuestManager.ReportObjectiveProgress(controller, "Kill", 7, 1);
            Assert.That(waitingProgress.state, Is.EqualTo(QuestState.TurnedIn));
            Assert.That(waitingProgress.NodeProgressCounts["goal"], Is.EqualTo(1));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ReportObjectiveProgress_DoesNotApplyTheEventToAnotherQuestRestartedByACallback(
            bool useAction, bool replaceProgress)
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            container.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "goal", EventKey = "Kill", TargetId = 7, RequiredAmount = 1
            });
            container.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(container, "start", "goal");
            if (useAction)
            {
                container.Nodes.Add(new QuestActionNodeData
                {
                    Guid = "action", Action = new MethodBindingData { Key = FlowActionKey }
                });
                Connect(container, "goal", "action");
                Connect(container, "action", "end");
            }
            else
            {
                Connect(container, "goal", "end");
            }
            QuestContainer restartedContainer = CreateQuest();
            restartedContainer.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            restartedContainer.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "goal", EventKey = "Kill", TargetId = 7, RequiredAmount = 3
            });
            Connect(restartedContainer, "start", "goal");
            QuestManager.Initialize(new[] { container, restartedContainer });
            var controller = new TestController();
            Assert.That(QuestManager.StartQuest(controller, container.QuestId), Is.True);
            Assert.That(QuestManager.StartQuest(controller, restartedContainer.QuestId), Is.True);
            Assert.That(QuestManager.AdvanceObjective(controller, restartedContainer.QuestId, "goal"), Is.True);
            QuestProgress original = controller.QuestProgress[restartedContainer.QuestId];
            int restartCount = 0;
            void RestartOtherQuest()
            {
                restartCount++;
                if (replaceProgress)
                {
                    controller.QuestProgress[restartedContainer.QuestId] = new QuestProgress(restartedContainer);
                }
                else
                {
                    Assert.That(QuestManager.ResetQuest(controller, restartedContainer.QuestId), Is.True);
                }
                Assert.That(QuestManager.StartQuest(controller, restartedContainer.QuestId), Is.True);
            }
            if (useAction)
            {
                flowAction = _ => RestartOtherQuest();
            }
            else
            {
                controller.ProgressChanged = (_, progress) =>
                {
                    if (progress.questId == container.QuestId && progress.state == QuestState.TurnedIn)
                    {
                        RestartOtherQuest();
                    }
                };
            }

            QuestManager.ReportObjectiveProgress(controller, "Kill", 7, 1);

            Assert.That(restartCount, Is.EqualTo(1));
            Assert.That(controller.QuestProgress[container.QuestId].state, Is.EqualTo(QuestState.TurnedIn));
            QuestProgress restarted = controller.QuestProgress[restartedContainer.QuestId];
            Assert.That(ReferenceEquals(restarted, original), Is.EqualTo(!replaceProgress));
            Assert.That(restarted.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(restarted.ActiveNodeGuids, Is.EqualTo(new[] { "goal" }));
            Assert.That(restarted.NodeProgressCounts["goal"], Is.Zero);
            if (replaceProgress)
            {
                Assert.That(original.NodeProgressCounts["goal"], Is.EqualTo(1));
            }
            QuestManager.ReportObjectiveProgress(controller, "Kill", 7, 1);
            Assert.That(restartCount, Is.EqualTo(1));
            Assert.That(restarted.NodeProgressCounts["goal"], Is.EqualTo(1));
        }

        [Test]
        public void InteractionQuery_FollowsValidPathsLongerThan128Nodes()
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc" });
            string previous = "entry";
            for (int i = 0; i < 150; i++)
            {
                string guid = $"condition-{i}";
                container.Nodes.Add(new QuestStateConditionNodeData
                {
                    Guid = guid,
                    QuestId = container.QuestId,
                    TargetState = QuestState.NotStarted
                });
                Connect(container, previous, guid, i == 0 ? QuestPortNames.Next : QuestPortNames.True);
                previous = guid;
            }
            container.Nodes.Add(new QuestSuggestionNodeData { Guid = "candidate" });
            Connect(container, previous, "candidate", QuestPortNames.True);
            QuestManager.Initialize(new[] { container });

            QuestSuggestion[] candidates = QuestManager.GetQuestSuggestions(new TestController(), "npc");

            Assert.That(candidates, Has.Length.EqualTo(1));
            Assert.That(candidates[0].QuestId, Is.EqualTo(container.QuestId));
        }

        [Test]
        public void InteractionQuery_StopsAtVisitedNodesWithoutAStepLimit()
        {
            QuestContainer container = CreateQuest();
            container.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc" });
            container.Nodes.Add(new QuestStateConditionNodeData
            {
                Guid = "cycle",
                QuestId = container.QuestId,
                TargetState = QuestState.NotStarted
            });
            Connect(container, "entry", "cycle");
            Connect(container, "cycle", "cycle", QuestPortNames.True);
            QuestManager.Initialize(new[] { container });

            Assert.That(QuestManager.GetQuestSuggestions(new TestController(), "npc"), Is.Empty);
        }

        [QuestAction(FlowActionKey, Owner = QuestMethodOwner.Global)]
        private static void InvokeFlowAction(QuestExecutionContext context)
        {
            flowAction?.Invoke(context);
        }

        private static void AssertSaveRoundTrip(TestController source, QuestProgress original)
        {
            string json = QuestSaveData.Capture(source).ToJson();
            Assert.That(QuestSaveData.TryFromJson(json, out QuestSaveData saved, out string parseError),
                Is.True, parseError);
            var target = new TestController();
            Assert.That(saved.TryApplyTo(target, replaceExisting: true, out string restoreError),
                Is.True, restoreError);

            QuestProgress restored = target.QuestProgress[original.questId];
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(restored.state, Is.EqualTo(original.state));
            Assert.That(restored.ActiveNodeGuids, Is.EqualTo(original.ActiveNodeGuids));
            Assert.That(restored.NodeProgressCounts, Is.EquivalentTo(original.NodeProgressCounts));
            Assert.That(restored.CompletedNodeGuids, Is.EqualTo(original.CompletedNodeGuids));
            Assert.That(restored.CompletedGateInputs, Is.EqualTo(original.CompletedGateInputs));
        }

        private QuestContainer CreateQuest()
        {
            QuestContainer container = ScriptableObject.CreateInstance<QuestContainer>();
            container.QuestId = createdContainers.Count + 1;
            container.name = $"Flow Regression {container.QuestId}";
            createdContainers.Add(container);
            return container;
        }

        private static void Connect(QuestContainer container, string source, string target, string port = QuestPortNames.Next)
        {
            container.NodeLinks.Add(new NodeLinkData
            {
                StartNodeGuid = source,
                StartPortName = port,
                TargetNodeGuid = target,
                TargetPortName = "Input"
            });
        }

        private sealed class TestController : IQuestController
        {
            public IDictionary<int, QuestProgress> QuestProgress { get; } = new Dictionary<int, QuestProgress>();
            public Action<QuestContainer, QuestProgress> ProgressChanged { get; set; }

            public void OnQuestProgressChanged(QuestContainer container, QuestProgress progress)
            {
                ProgressChanged?.Invoke(container, progress);
            }
        }
    }
}
