using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniversalGraph.Tests
{
    public sealed class UniversalGraphRuntimeTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new();
        private static int attributedQuestActionAmount;
        private static bool attributedQuestActionFlag;
        private static int dialogueChoiceActionCount;

        [TearDown]
        public void TearDown()
        {
            if (DialogueManager.Instance.IsConversationActive)
            {
                DialogueManager.Instance.CancelConversation();
            }

            foreach (UnityEngine.Object createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void DialogueContainer_ResolvesOneNamedEntry()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueStartNodeData
            {
                Guid = "entry",
                EntryId = $"  {DialogueStartNodeData.DefaultEntryId}  "
            };
            var line = new DialogueNodeData { Guid = "line" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(line);
            graph.NodeLinks.Add(Link("entry", "Next", "line"));

            bool resolved = graph.FindEntryNode(
                $" {DialogueStartNodeData.DefaultEntryId} ",
                out DialogueStartNodeData result,
                out string error);

            Assert.That(resolved, Is.True, error);
            Assert.That(result, Is.SameAs(entry));
            Assert.That(entry.EntryId, Is.EqualTo(DialogueStartNodeData.DefaultEntryId));

            bool wrongCaseResolved = graph.FindEntryNode(
                DialogueStartNodeData.DefaultEntryId.ToLowerInvariant(),
                out _,
                out _);
            Assert.That(wrongCaseResolved, Is.False);
        }

        [Test]
        public void DialogueContainer_EntryLookupDoesNotValidateUnrelatedNodeData()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueStartNodeData { Guid = "entry" };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(new DialogueChoiceNodeData { Guid = "broken-choice", Choices = null });
            graph.NodeLinks = null;

            bool resolved = graph.FindEntryNode(
                DialogueStartNodeData.DefaultEntryId,
                out DialogueStartNodeData result,
                out string error);

            Assert.That(resolved, Is.True, error);
            Assert.That(result, Is.SameAs(entry));
        }

        [Test]
        public void DialogueManager_RejectsChoiceNodeWithoutChoices()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.name = "Broken Dialogue";
            graph.Nodes.Add(new DialogueStartNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueChoiceNodeData { Guid = "choice" });
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            LogAssert.Expect(
                LogType.Error,
                "[Dialogue] 대화를 시작하지 못했습니다. 대화 그래프 'Broken Dialogue'의 " +
                "Choice 노드 'choice'에 선택지가 없습니다.");

            bool started = DialogueManager.Instance.TryStartConversation(
                graph,
                DialogueStartNodeData.DefaultEntryId,
                new DialogueContext(speaker, interactor));

            Assert.That(started, Is.False);
            Assert.That(DialogueManager.Instance.IsConversationActive, Is.False);
        }

        [Test]
        public void MethodArgumentCodec_SupportsOnlyTheChosenGraphValueTypes()
        {
            Assert.That(MethodArgumentCodec.TryGetKind(typeof(int), out _), Is.True);
            Assert.That(MethodArgumentCodec.TryGetKind(typeof(float), out _), Is.True);
            Assert.That(MethodArgumentCodec.TryGetKind(typeof(ScriptableObject), out MethodArgumentKind unityKind), Is.True);
            Assert.That(unityKind, Is.EqualTo(MethodArgumentKind.UnityObject));
            Assert.That(MethodArgumentCodec.TryGetKind(typeof(long), out _), Is.False);
            Assert.That(MethodArgumentCodec.TryGetKind(typeof(double), out _), Is.False);
            Assert.That(MethodArgumentCodec.TryGetKind(typeof(object), out _), Is.False);
        }

        [Test]
        public void WaitSignalNodeData_NormalizesSignalKeyWhenAssigned()
        {
            var data = new DialogueWaitSignalNodeData { SignalKey = "  dialogue.finished  " };

            Assert.That(data.SignalKey, Is.EqualTo("dialogue.finished"));
        }

        [Test]
        public void DialogueAndQuestDescriptors_UseTheCommonMethodContract()
        {
            MethodInfo dialogueMethod = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(IsDialogueChoiceVisible),
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo questMethod = typeof(UniversalGraphRuntimeTests).GetMethod(
                nameof(IsAttributedQuestReady),
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(DialogueMethodDescriptorFactory.TryCreate(
                    dialogueMethod,
                    MethodKind.Condition,
                    "tests.dialogue.choice-visible",
                    DialogueTarget.Global,
                    out DialogueMethodDescriptor dialogueDescriptor,
                    out string dialogueError),
                Is.True,
                dialogueError);
            Assert.That(QuestMethodDescriptorFactory.TryCreate(
                    questMethod,
                    MethodKind.Condition,
                    "tests.quest.is-ready",
                    QuestMethodTarget.Global,
                    out QuestMethodDescriptor questDescriptor,
                    out string questError),
                Is.True,
                questError);

            Assert.That(dialogueDescriptor, Is.InstanceOf<MethodDescriptor>());
            Assert.That(questDescriptor, Is.InstanceOf<MethodDescriptor>());
            Assert.That(MethodArgumentCodec.CreateDefaultArguments(dialogueDescriptor), Has.Count.EqualTo(1));
            Assert.That(MethodArgumentCodec.CreateDefaultArguments(questDescriptor), Has.Count.EqualTo(1));
        }

        [Test]
        public void QuestDialogueRouter_SelectsRouteFromQuestStateWithoutPlayerOrNpcTypes()
        {
            DialogueContainer dialogue = CreateAsset<DialogueContainer>();
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.id = 10;
            var entry = new QuestEventEntryNodeData { Guid = "entry", TargetId = "npc-7" };
            var condition = new QuestStateConditionNodeData
            {
                Guid = "condition",
                QuestId = 10,
                TargetState = QuestState.InProgress
            };
            var request = new DialogueRequestNodeData
            {
                Guid = "request",
                DialogueReference = new DialogueReference(dialogue, "Default"),
                TopicName = "Quest",
                Priority = 5
            };
            quest.Nodes.Add(entry);
            quest.Nodes.Add(condition);
            quest.Nodes.Add(request);
            quest.NodeLinks.Add(Link("entry", "Next", "condition"));
            quest.NodeLinks.Add(Link("condition", "True", "request"));

            var controller = new FakeQuestController();
            controller.QuestProgress.Add(10, new QuestProgress(quest) { state = QuestState.InProgress });

            List<DialogueRequest> results = QuestDialogueRouter.Evaluate(new[] { quest }, controller, "NPC-7");

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Reference.GraphAsset, Is.SameAs(dialogue));
            Assert.That(results[0].Priority, Is.EqualTo(5));
        }

        [Test]
        public void QuestRunner_CompletesObjectiveAndAdvancesToStateChange()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.id = 20;
            var start = new QuestStartNodeData { Guid = "start" };
            var objective = new QuestObjectiveNodeData
            {
                Guid = "objective",
                ObjectiveType = "Kill",
                TargetId = 3,
                RequiredAmount = 3
            };
            var complete = new QuestStateChangeNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            quest.Nodes.Add(start);
            quest.Nodes.Add(objective);
            quest.Nodes.Add(complete);
            quest.NodeLinks.Add(Link("start", "Next", "objective"));
            quest.NodeLinks.Add(Link("objective", "Next", "complete"));

            QuestManager.Initialize(new[] { quest });
            var controller = new FakeQuestController();
            var progress = new QuestProgress(quest) { state = QuestState.Ready };
            controller.QuestProgress.Add(quest.id, progress);

            QuestRunner.StartQuestGraph(controller, quest.id);
            Assert.That(progress.activeNodeGuids, Does.Contain("objective"));

            QuestRunner.ProcessEvent(controller, "Kill", 3, 2);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.nodeProgressCounts["objective"], Is.EqualTo(2));

            QuestRunner.ProcessEvent(controller, "Kill", 3, 1);
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
            Assert.That(progress.activeNodeGuids, Does.Not.Contain("objective"));
        }

        [Test]
        public void QuestManager_RejectsLinksToMissingNodesDuringInitialization()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.id = 21;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.NodeLinks.Add(Link("start", "Next", "missing"));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => QuestManager.Initialize(new[] { quest }));

            Assert.That(exception.Message, Does.Contain("존재하지 않는 노드"));
        }

        [Test]
        public void QuestManager_RebuildsIndexWhenGraphListsAreReplaced()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.id = 22;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "old-start" });
            quest.Nodes.Add(new QuestObjectiveNodeData
            {
                Guid = "old-objective",
                ObjectiveType = "Old"
            });
            quest.NodeLinks.Add(Link("old-start", "Next", "old-objective"));
            QuestManager.Initialize(new[] { quest });

            quest.Nodes = new List<NodeBaseData>
            {
                new QuestStartNodeData { Guid = "new-start" },
                new QuestStateChangeNodeData
                {
                    Guid = "new-state",
                    NewState = QuestState.CanComplete
                }
            };
            quest.NodeLinks = new List<NodeLinkData>
            {
                Link("new-start", "Next", "new-state")
            };

            var controller = new FakeQuestController();
            var progress = new QuestProgress(quest) { state = QuestState.Ready };
            controller.QuestProgress.Add(quest.id, progress);

            QuestRunner.StartQuestGraph(controller, quest.id);

            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
            Assert.That(progress.currentNodeGuid, Is.EqualTo("new-state"));
        }

        [Test]
        public void QuestRunner_AndGateDerivesRequiredCountFromConnectedBranches()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.id = 30;
            var start = new QuestStartNodeData { Guid = "start" };
            var first = new QuestObjectiveNodeData
            {
                Guid = "first",
                ObjectiveType = "Collect",
                TargetId = 1,
                RequiredAmount = 1
            };
            var second = new QuestObjectiveNodeData
            {
                Guid = "second",
                ObjectiveType = "Collect",
                TargetId = 2,
                RequiredAmount = 1
            };
#pragma warning disable CS0618
            var gate = new QuestAndGateNodeData { Guid = "gate" };
#pragma warning restore CS0618
            var complete = new QuestStateChangeNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            quest.Nodes.Add(start);
            quest.Nodes.Add(first);
            quest.Nodes.Add(second);
            quest.Nodes.Add(gate);
            quest.Nodes.Add(complete);
            quest.NodeLinks.Add(Link("start", "Next", "first"));
            quest.NodeLinks.Add(Link("start", "Next", "second"));
            quest.NodeLinks.Add(Link("first", "Next", "gate"));
            quest.NodeLinks.Add(Link("second", "Next", "gate"));
            quest.NodeLinks.Add(Link("gate", "Next", "complete"));

            QuestManager.Initialize(new[] { quest });
            var controller = new FakeQuestController();
            var progress = new QuestProgress(quest) { state = QuestState.Ready };
            controller.QuestProgress.Add(quest.id, progress);

            QuestRunner.StartQuestGraph(controller, quest.id);
            QuestRunner.ProcessEvent(controller, "Collect", 1, 1);
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));

            QuestRunner.ProcessEvent(controller, "Collect", 2, 1);
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
        }

        [Test]
        public void QuestSaveData_RoundTripsDictionaryAndFlowCollections()
        {
            var source = new FakeQuestController();
            source.QuestProgress.Add(77, new QuestProgress
            {
                questId = 77,
                state = QuestState.InProgress,
                currentNodeGuid = "objective-a",
                currentObjectiveCount = 4,
                activeNodeGuids = new List<string> { "objective-a", "sub-quest-b" },
                nodeProgressCounts = new Dictionary<string, int>
                {
                    ["objective-a"] = 4,
                    ["objective-c"] = 2
                },
                completedNodeGuids = new List<string> { "action-1" },
                completedGateInputs = new List<string> { "gate-1|objective-a" }
            });

            string json = QuestSaveData.Capture(source).ToJson();
            Assert.That(QuestSaveData.TryFromJson(json, out QuestSaveData parsed, out string parseError),
                Is.True,
                parseError);

            var target = new FakeQuestController();
            Assert.That(parsed.TryApplyTo(target, replaceExisting: true, out string restoreError),
                Is.True,
                restoreError);

            QuestProgress restored = target.QuestProgress[77];
            Assert.That(restored.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(restored.currentNodeGuid, Is.EqualTo("objective-a"));
            Assert.That(restored.currentObjectiveCount, Is.EqualTo(4));
            Assert.That(restored.activeNodeGuids, Is.EquivalentTo(new[] { "objective-a", "sub-quest-b" }));
            Assert.That(restored.nodeProgressCounts["objective-a"], Is.EqualTo(4));
            Assert.That(restored.nodeProgressCounts["objective-c"], Is.EqualTo(2));
            Assert.That(restored.completedNodeGuids, Does.Contain("action-1"));
            Assert.That(restored.completedGateInputs, Does.Contain("gate-1|objective-a"));
        }

        [Test]
        public void GraphAssetMigrator_UpgradesLegacySerializationAndIsIdempotent()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var choice = new DialogueChoiceData
            {
                PortName = string.Empty,
                ChoiceEvent = null,
                VisibilityCondition = null
            };
            var line = new DialogueNodeData
            {
                Guid = "line",
                Event = null
            };
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData> { choice }
            };
            graph.Nodes.Add(line);
            graph.Nodes.Add(choiceNode);
            graph.NodeLinks.Add(new NodeLinkData
            {
                StartNodeGuid = "line",
                StartPortName = "Next",
                TargetNodeGuid = "target",
                TargetPortName = string.Empty
            });

            Assert.That(GraphAssetMigrator.TryMigrate(
                    graph,
                    out GraphAssetMigrationResult first,
                    out string firstError),
                Is.True,
                firstError);
            Assert.That(first.FromVersion, Is.Zero);
            Assert.That(first.ToVersion, Is.EqualTo(GraphAssetMigrator.CurrentVersion));
            Assert.That(first.Changed, Is.True);
            Assert.That(line.Event, Is.Not.Null);
            Assert.That(line.Event.Arguments, Is.Not.Null);
            Assert.That(choice.ChoiceEvent, Is.Not.Null);
            Assert.That(choice.ChoiceEvent.Arguments, Is.Not.Null);
            Assert.That(choice.VisibilityCondition, Is.Not.Null);
            Assert.That(choice.VisibilityCondition.Arguments, Is.Not.Null);
            Assert.That(choice.PortName, Is.Not.Empty);
            Assert.That(graph.NodeLinks[0].TargetPortName, Is.EqualTo("Input"));

            Assert.That(GraphAssetMigrator.TryMigrate(
                    graph,
                    out GraphAssetMigrationResult second,
                    out string secondError),
                Is.True,
                secondError);
            Assert.That(second.Changed, Is.False);
        }

        [Test]
        public void GraphAssetMigrator_AppliesCommonAndQuestMigrationsToQuestContainer()
        {
            QuestContainer graph = CreateAsset<QuestContainer>();
            var action = new QuestActionTriggerNodeData { Action = null };
            var condition = new QuestConditionBranchNodeData { Condition = null };
            var reward = new QuestRewardNodeData { RewardAction = null };
            graph.Nodes.Add(action);
            graph.Nodes.Add(condition);
            graph.Nodes.Add(reward);
            graph.NodeLinks.Add(new NodeLinkData { TargetPortName = string.Empty });

            Assert.That(GraphAssetMigrator.TryMigrate(
                    graph,
                    out GraphAssetMigrationResult result,
                    out string error),
                Is.True,
                error);

            Assert.That(result.Changed, Is.True);
            Assert.That(action.Action, Is.Not.Null);
            Assert.That(action.Action.Arguments, Is.Not.Null);
            Assert.That(condition.Condition, Is.Not.Null);
            Assert.That(condition.Condition.Arguments, Is.Not.Null);
            Assert.That(reward.RewardAction, Is.Not.Null);
            Assert.That(reward.RewardAction.Arguments, Is.Not.Null);
            Assert.That(graph.NodeLinks[0].TargetPortName, Is.EqualTo("Input"));
        }

        [Test]
        public void GraphAssetMigrator_RunsCommonMigrationBeforeDomainMigration()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes = null;
            graph.NodeLinks = null;

            Assert.That(GraphAssetMigrator.TryMigrate(graph, out _, out string error), Is.True, error);
            Assert.That(graph.Nodes, Is.Not.Null);
            Assert.That(graph.NodeLinks, Is.Not.Null);
        }

        [Test]
        public void GraphAssetMigrator_RejectsFutureSchemaWithoutChangingIt()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            FieldInfo schemaField = typeof(GraphContainer).GetField(
                "schemaVersion",
                BindingFlags.Instance | BindingFlags.NonPublic);
            int futureVersion = GraphAssetMigrator.CurrentVersion + 1;
            schemaField.SetValue(graph, futureVersion);

            Assert.That(GraphAssetMigrator.TryMigrate(graph, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("미래 스키마"));
            Assert.That(graph.SchemaVersion, Is.EqualTo(futureVersion));
        }

        [Test]
        public void QuestSaveData_MigratesVersionOneAndRejectsFutureVersions()
        {
            const string versionOneJson =
                "{\"schemaVersion\":1,\"quests\":[{\"questId\":91,\"state\":2," +
                "\"currentNodeGuid\":\"objective\",\"currentObjectiveCount\":3," +
                "\"activeNodeGuids\":[\"objective\"],\"nodeProgressCounts\":[]," +
                "\"completedNodeGuids\":[],\"completedGateInputs\":[]}]}";

            Assert.That(QuestSaveData.TryFromJson(
                    versionOneJson,
                    out QuestSaveData migrated,
                    out string migrationError),
                Is.True,
                migrationError);
            Assert.That(migrated.schemaVersion, Is.EqualTo(QuestSaveData.CurrentSchemaVersion));
            Assert.That(migrated.quests.Single().definitionSchemaVersion,
                Is.EqualTo(GraphAssetMigrator.CurrentVersion));

            string futureJson = $"{{\"schemaVersion\":{QuestSaveData.CurrentSchemaVersion + 1},\"quests\":[]}}";
            Assert.That(QuestSaveData.TryFromJson(futureJson, out _, out string futureError), Is.False);
            Assert.That(futureError, Does.Contain("지원 버전"));
        }

        [Test]
        public void QuestRunner_InvokesAttributedConditionAndTypedAction()
        {
            attributedQuestActionAmount = 0;
            attributedQuestActionFlag = false;

            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.id = 88;
            var start = new QuestStartNodeData { Guid = "start" };
            var condition = new QuestConditionBranchNodeData
            {
                Guid = "condition",
                Condition = new MethodCallData
                {
                    Key = "tests.quest.is-ready",
                    Arguments = CreateQuestArguments(
                        nameof(IsAttributedQuestReady),
                        MethodKind.Condition,
                        ("required", 42))
                }
            };
            var action = new QuestActionTriggerNodeData
            {
                Guid = "action",
                Action = new MethodCallData
                {
                    Key = "tests.quest.record-action",
                    Arguments = CreateQuestArguments(
                        nameof(RecordAttributedQuestAction),
                        MethodKind.Action,
                        ("amount", 7),
                        ("flag", true))
                }
            };
            var complete = new QuestStateChangeNodeData
            {
                Guid = "complete",
                NewState = QuestState.CanComplete
            };
            quest.Nodes.Add(start);
            quest.Nodes.Add(condition);
            quest.Nodes.Add(action);
            quest.Nodes.Add(complete);
            quest.NodeLinks.Add(Link("start", "Next", "condition"));
            quest.NodeLinks.Add(Link("condition", "True", "action"));
            quest.NodeLinks.Add(Link("action", "Next", "complete"));

            QuestManager.Initialize(new[] { quest });
            QuestEventRegistry.Initialize();
            var controller = new FakeQuestController();
            var progress = new QuestProgress(quest) { state = QuestState.Ready };
            controller.QuestProgress.Add(quest.id, progress);

            QuestRunner.StartQuestGraph(controller, quest.id);

            Assert.That(attributedQuestActionAmount, Is.EqualTo(7));
            Assert.That(attributedQuestActionFlag, Is.True);
            Assert.That(progress.state, Is.EqualTo(QuestState.CanComplete));
        }

        [Test]
        public void DialogueManager_HidesChoicesWhoseConditionIsFalse()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueStartNodeData { Guid = "entry", EntryId = "Default" };
            var line = new DialogueNodeData
            {
                Guid = "line",
                DialogueText = "Choose"
            };
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData>
                {
                    new()
                    {
                        PortName = "visible",
                        ChoiceText = "Visible",
                        VisibilityCondition = new MethodCallData
                        {
                            Key = "tests.dialogue.choice-visible",
                            Arguments = CreateDialogueArguments(
                                nameof(IsDialogueChoiceVisible),
                                MethodKind.Condition,
                                ("visible", true))
                        }
                    },
                    new()
                    {
                        PortName = "hidden",
                        ChoiceText = "Hidden",
                        VisibilityCondition = new MethodCallData
                        {
                            Key = "tests.dialogue.choice-visible",
                            Arguments = CreateDialogueArguments(
                                nameof(IsDialogueChoiceVisible),
                                MethodKind.Condition,
                                ("visible", false))
                        }
                    }
                }
            };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(line);
            graph.Nodes.Add(choiceNode);
            graph.NodeLinks.Add(Link("entry", "Next", "line"));
            graph.NodeLinks.Add(Link("line", "Next", "choice"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            List<DialogueChoiceData> shown = null;
            void CaptureChoices(List<DialogueChoiceData> choices) => shown = choices;
            DialogueManager.Instance.OnShowChoices += CaptureChoices;
            try
            {
                DialogueEventRegistry.Initialize();
                Assert.That(DialogueManager.Instance.TryStartConversation(
                        graph,
                        "Default",
                        new DialogueContext(speaker, interactor)),
                    Is.True);
                Assert.That(shown, Is.Null);
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.False);

                DialogueManager.Instance.ContinueNextLine();

                Assert.That(shown, Is.Not.Null);
                Assert.That(shown.Select(choice => choice.PortName), Is.EqualTo(new[] { "visible" }));
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.True);
            }
            finally
            {
                DialogueManager.Instance.OnShowChoices -= CaptureChoices;
            }
        }

        [Test]
        public void DialogueManager_UsesDefaultWhenEveryChoiceIsHidden()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueStartNodeData { Guid = "entry", EntryId = "Default" };
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData>
                {
                    new()
                    {
                        PortName = "hidden",
                        ChoiceText = "Hidden",
                        VisibilityCondition = new MethodCallData
                        {
                            Key = "tests.dialogue.choice-visible",
                            Arguments = CreateDialogueArguments(
                                nameof(IsDialogueChoiceVisible),
                                MethodKind.Condition,
                                ("visible", false))
                        }
                    }
                }
            };
            var defaultLine = new DialogueNodeData
            {
                Guid = "default",
                DialogueText = "Default"
            };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(choiceNode);
            graph.Nodes.Add(defaultLine);
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));
            graph.NodeLinks.Add(Link("choice", DialogueChoiceNodeData.DefaultPortName, "default"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            DialogueNodeData shownLine = null;
            int choiceEventCount = 0;
            void CaptureLine(DialogueNodeData line) => shownLine = line;
            void CaptureChoices(List<DialogueChoiceData> _) => choiceEventCount++;
            DialogueManager.Instance.OnShowLine += CaptureLine;
            DialogueManager.Instance.OnShowChoices += CaptureChoices;
            try
            {
                DialogueEventRegistry.Initialize();
                Assert.That(DialogueManager.Instance.TryStartConversation(
                        graph,
                        "Default",
                        new DialogueContext(speaker, interactor)),
                    Is.True);
                Assert.That(shownLine, Is.SameAs(defaultLine));
                Assert.That(choiceEventCount, Is.Zero);
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.False);
            }
            finally
            {
                DialogueManager.Instance.OnShowLine -= CaptureLine;
                DialogueManager.Instance.OnShowChoices -= CaptureChoices;
            }
        }

        [Test]
        public void DialogueManager_ExecutesChoiceActionAndFollowsSelectedPort()
        {
            dialogueChoiceActionCount = 0;
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var entry = new DialogueStartNodeData { Guid = "entry", EntryId = "Default" };
            var selectedChoice = new DialogueChoiceData
            {
                PortName = "accept",
                ChoiceText = "Accept",
                ChoiceEvent = new MethodCallData
                {
                    Key = "tests.dialogue.choice-action"
                }
            };
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData> { selectedChoice }
            };
            var resultLine = new DialogueNodeData
            {
                Guid = "result",
                DialogueText = "Accepted"
            };
            graph.Nodes.Add(entry);
            graph.Nodes.Add(choiceNode);
            graph.Nodes.Add(resultLine);
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));
            graph.NodeLinks.Add(Link("choice", "accept", "result"));

            GameObject speaker = CreateGameObject("speaker");
            GameObject interactor = CreateGameObject("interactor");
            DialogueNodeData shownLine = null;
            void CaptureLine(DialogueNodeData line) => shownLine = line;
            DialogueManager.Instance.OnShowLine += CaptureLine;
            try
            {
                DialogueEventRegistry.Initialize();
                Assert.That(DialogueManager.Instance.TryStartConversation(
                        graph,
                        "Default",
                        new DialogueContext(speaker, interactor)),
                    Is.True);
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.True);

                DialogueManager.Instance.OnSelectionChoice(selectedChoice);

                Assert.That(dialogueChoiceActionCount, Is.EqualTo(1));
                Assert.That(shownLine, Is.SameAs(resultLine));
                Assert.That(DialogueManager.Instance.IsWaitingForChoice, Is.False);
            }
            finally
            {
                DialogueManager.Instance.OnShowLine -= CaptureLine;
            }
        }

        [QuestCondition("tests.quest.is-ready", Target = QuestMethodTarget.Global)]
        internal static bool IsAttributedQuestReady(
            QuestExecutionContext context,
            [QuestParameter("required")] int required)
        {
            return context.Progress?.state == QuestState.InProgress && required == 42;
        }

        [QuestAction("tests.quest.record-action", Target = QuestMethodTarget.Global)]
        internal static void RecordAttributedQuestAction(
            QuestExecutionContext context,
            [QuestParameter("amount")] int amount,
            [QuestParameter("flag")] bool flag)
        {
            attributedQuestActionAmount = context.Progress == null ? -1 : amount;
            attributedQuestActionFlag = flag;
        }

        [DialogueCondition("tests.dialogue.choice-visible", Target = DialogueTarget.Global)]
        private static bool IsDialogueChoiceVisible([DialogueParameter("visible")] bool visible)
        {
            return visible;
        }

        [DialogueAction("tests.dialogue.choice-action", Target = DialogueTarget.Global)]
        private static void RecordDialogueChoiceAction()
        {
            dialogueChoiceActionCount++;
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static List<MethodArgumentData> CreateQuestArguments(
            string methodName,
            MethodKind kind,
            params (string Id, object Value)[] values)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(QuestMethodDescriptorFactory.TryCreate(
                    method,
                    kind,
                    kind == MethodKind.Action
                        ? "tests.quest.record-action"
                        : "tests.quest.is-ready",
                    QuestMethodTarget.Global,
                    out QuestMethodDescriptor descriptor,
                    out string error),
                Is.True,
                error);
            List<MethodArgumentData> arguments = MethodArgumentCodec.CreateDefaultArguments(descriptor);
            WriteArguments(arguments, descriptor.SerializedParameters, values);
            return arguments;
        }

        private static List<MethodArgumentData> CreateDialogueArguments(
            string methodName,
            MethodKind kind,
            params (string Id, object Value)[] values)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(DialogueMethodDescriptorFactory.TryCreate(
                    method,
                    kind,
                    "tests.dialogue.choice-visible",
                    DialogueTarget.Global,
                    out DialogueMethodDescriptor descriptor,
                    out string error),
                Is.True,
                error);
            List<MethodArgumentData> arguments = MethodArgumentCodec.CreateDefaultArguments(descriptor);
            WriteArguments(arguments, descriptor.SerializedParameters, values);
            return arguments;
        }

        private static void WriteArguments(
            IList<MethodArgumentData> arguments,
            IReadOnlyList<MethodParameterDescriptor> descriptors,
            IEnumerable<(string Id, object Value)> values)
        {
            foreach ((string id, object value) in values)
            {
                MethodParameterDescriptor descriptor = descriptors.Single(parameter => parameter.ParameterId == id);
                MethodArgumentData argument = arguments.Single(candidate => candidate.ParameterId == id);
                argument.SerializedValue = MethodArgumentCodec.SerializeScalar(
                    value,
                    descriptor.ParameterType,
                    descriptor.Kind);
            }
        }

        private static NodeLinkData Link(string source, string port, string target)
        {
            return new NodeLinkData
            {
                StartNodeGuid = source,
                StartPortName = port,
                TargetNodeGuid = target,
                TargetPortName = "Input"
            };
        }

        private sealed class FakeQuestController : IQuestController
        {
            public Dictionary<int, QuestProgress> QuestProgress { get; } = new();

            public QuestProgress GetQuestStatus(int questId)
            {
                QuestProgress.TryGetValue(questId, out QuestProgress progress);
                return progress;
            }

            public void InvokeStatusChanged(QuestContainer container, QuestProgress progress)
            {
            }

            public void TurnInQuest(int questId)
            {
                if (QuestProgress.TryGetValue(questId, out QuestProgress progress))
                {
                    progress.state = QuestState.TurnedIn;
                }
            }
        }
    }
}
