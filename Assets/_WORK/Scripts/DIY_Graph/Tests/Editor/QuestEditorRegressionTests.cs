using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Dialogue.Editor;
using UniversalGraph.Editor;
using UniversalGraph.Quest.Editor;

namespace UniversalGraph.Tests
{
    public sealed class QuestEditorRegressionTests
    {
        private sealed class TestWindow : EditorWindow { }

        private readonly List<Object> createdObjects = new();
        private TestWindow window;

        [SetUp]
        public void SetUp()
        {
            window = ScriptableObject.CreateInstance<TestWindow>();
            window.Show();
        }

        [TearDown]
        public void TearDown()
        {
            window.Close();
            foreach (Object createdObject in createdObjects)
            {
                Undo.ClearUndo(createdObject);
                Object.DestroyImmediate(createdObject);
            }
            createdObjects.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GraphCreation_AcceptedNamePersistsDefaultsWithoutOpeningGraphWindow(bool quest)
        {
            EndNameEditAction action = CreateGraphMenuAction(quest);
            string folderName = "__GraphCreationTest_" + System.Guid.NewGuid().ToString("N");
            string folderPath = "Assets/" + folderName;
            string path = folderPath + "/Chosen Graph Name.asset";
            Object previousSelection = Selection.activeObject;
            int openWindowCount = Resources.FindObjectsOfTypeAll<UniversalGraphWindow>().Length;
            int expectedQuestId = 0;
            try
            {
                Assert.That(AssetDatabase.CreateFolder("Assets", folderName), Is.Not.Empty);
                if (quest)
                {
                    var existingQuest = ScriptableObject.CreateInstance<QuestContainer>();
                    existingQuest.QuestId = AssetDatabase.FindAssets("t:QuestContainer")
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<QuestContainer>)
                        .Where(candidate => candidate != null && candidate.QuestId > 0)
                        .Select(candidate => candidate.QuestId)
                        .DefaultIfEmpty(0)
                        .Max() + 1;
                    expectedQuestId = existingQuest.QuestId + 1;
                    AssetDatabase.CreateAsset(existingQuest, folderPath + "/Existing Quest.asset");
                }

                // Project 창에서 이름을 확정한 뒤 호출되는 실제 콜백을 실행합니다.
                action.Action(0, path, null);

                var container = AssetDatabase.LoadAssetAtPath<GraphContainer>(path);
                Assert.That(container, Is.Not.Null);
                Assert.That(Selection.activeObject, Is.SameAs(container));
                Assert.That(System.IO.File.Exists(path), Is.True);
                string nodeGuid = container.Nodes.Single().Guid;
                Assert.That(System.Guid.TryParse(nodeGuid, out _), Is.True);
                if (quest)
                {
                    // projectChanged 이벤트를 기다리지 않아도 연속 생성한 ID가 겹치면 안 됩니다.
                    string secondPath = folderPath + "/Second Quest.asset";
                    action.Action(0, secondPath, null);
                    Assert.That(AssetDatabase.LoadAssetAtPath<QuestContainer>(secondPath).QuestId,
                        Is.EqualTo(expectedQuestId + 1));
                }

                // 별도 SaveAssets 호출 없이 생성된 파일에서 초기 데이터가 복원되어야 합니다.
                Selection.activeObject = null;
                Resources.UnloadAsset(container);
                container = AssetDatabase.LoadAssetAtPath<GraphContainer>(path);
                Assert.That(container.name, Is.EqualTo("Chosen Graph Name"));
                Assert.That(container.SchemaVersion, Is.EqualTo(GraphAssetMigrator.CurrentVersion));
                Assert.That(container.Nodes.Count, Is.EqualTo(1));
                Assert.That(container.Nodes[0].Guid, Is.EqualTo(nodeGuid));
                Assert.That(container.Nodes[0].Position, Is.EqualTo(new Vector2(100f, 100f)));
                if (quest)
                {
                    Assert.That(container, Is.TypeOf<QuestContainer>());
                    Assert.That(((QuestContainer)container).QuestId, Is.EqualTo(expectedQuestId));
                    Assert.That(((QuestContainer)container).questName, Is.EqualTo("Chosen Graph Name"));
                    Assert.That(container.Nodes[0], Is.TypeOf<QuestStartNodeData>());
                }
                else
                {
                    Assert.That(container, Is.TypeOf<DialogueContainer>());
                    Assert.That(container.Nodes[0], Is.TypeOf<DialogueEntryNodeData>());
                    Assert.That(((DialogueEntryNodeData)container.Nodes[0]).EntryId,
                        Is.EqualTo(DialogueEntryNodeData.DefaultEntryId));
                }
                Assert.That(Resources.FindObjectsOfTypeAll<UniversalGraphWindow>().Length, Is.EqualTo(openWindowCount));
            }
            finally
            {
                Selection.activeObject = previousSelection;
                action.CleanUp();
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GraphCreation_CancelledNameDoesNotCreateAnAsset(bool quest)
        {
            EndNameEditAction action = CreateGraphMenuAction(quest);
            string folderName = "__GraphCreationTest_" + System.Guid.NewGuid().ToString("N");
            string folderPath = "Assets/" + folderName;
            string path = folderPath + "/Cancelled Graph.asset";
            int openWindowCount = Resources.FindObjectsOfTypeAll<UniversalGraphWindow>().Length;
            try
            {
                Assert.That(AssetDatabase.CreateFolder("Assets", folderName), Is.Not.Empty);

                action.Cancelled(0, path, null);

                Assert.That(AssetDatabase.LoadMainAssetAtPath(path), Is.Null);
                Assert.That(System.IO.File.Exists(path), Is.False);
                Assert.That(Resources.FindObjectsOfTypeAll<UniversalGraphWindow>().Length, Is.EqualTo(openWindowCount));
            }
            finally
            {
                action.CleanUp();
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        private static EndNameEditAction CreateGraphMenuAction(bool quest)
        {
            System.Type menuType = quest
                ? typeof(QuestGraphValidator).Assembly.GetType("UniversalGraph.Quest.Editor.QuestGraphMenu", true)
                : typeof(UniversalGraph.Dialogue.Editor.DialogueGraphValidator).Assembly.GetType(
                    "UniversalGraph.Dialogue.Editor.DialogueGraphMenu", true);
            return (EndNameEditAction)ScriptableObject.CreateInstance(menuType);
        }

        [Test]
        public void QuestStart_CannotBeCopiedButInteractionEntryCan()
        {
            var view = new UniversalGraphView();
            var start = new QuestStartNode();
            start.BindNodeData(new QuestStartNodeData { Guid = "start" });
            var interaction = new QuestInteractionEntryNode();
            interaction.BindNodeData(new QuestInteractionEntryNodeData { Guid = "interaction" });

            Assert.That(start.capabilities.HasFlag(Capabilities.Copiable), Is.False);
            Assert.That(view.serializeGraphElements(new GraphElement[] { start }), Is.Empty);
            Assert.That(interaction.capabilities.HasFlag(Capabilities.Copiable), Is.True);
        }

        [TestCase(-3, 1)]
        [TestCase(0, 1)]
        [TestCase(4, 4)]
        public void ObjectiveAmount_ShowsTheValueStoredInData(int requested, int expected)
        {
            var data = new QuestObjectiveNodeData { Guid = "objective", RequiredAmount = 2 };
            var node = new QuestObjectiveNode();
            node.BindNodeData(data);
            VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                (_, edit) => edit(), (_, edit) => edit()));
            window.rootVisualElement.Add(inspector);
            IntegerField field = inspector.Query<IntegerField>().ToList()
                .Single(candidate => candidate.label == "Required Amount");

            field.value = requested;

            Assert.That(data.RequiredAmount, Is.EqualTo(expected));
            Assert.That(field.value, Is.EqualTo(expected));
            Assert.That(node.title, Does.EndWith($"x{expected}"));
        }

        [TestCase(-3)]
        [TestCase(0)]
        [TestCase(4)]
        public void ObjectiveAmount_OpeningInspectorShowsStoredValueWithoutChangingIt(int stored)
        {
            var data = new QuestObjectiveNodeData { Guid = "objective", RequiredAmount = stored };
            var node = new QuestObjectiveNode();
            node.BindNodeData(data);
            VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                (_, _) => Assert.Fail("인스펙터를 여는 것만으로 데이터를 수정하면 안 됩니다."),
                (_, _) => Assert.Fail("인스펙터를 여는 것만으로 구조를 수정하면 안 됩니다.")));
            window.rootVisualElement.Add(inspector);
            IntegerField field = inspector.Query<IntegerField>().ToList()
                .Single(candidate => candidate.label == "Required Amount");

            Assert.That(data.RequiredAmount, Is.EqualTo(stored));
            Assert.That(field.value, Is.EqualTo(stored));
            Assert.That(node.title, Does.EndWith($"x{stored}"));
        }

        [Test]
        public void ObjectiveAndInteractionKeys_ShowTheirTrimmedValues()
        {
            var editHandler = new NodeInspectorEditHandler((_, edit) => edit(), (_, edit) => edit());
            var objective = new QuestObjectiveNode();
            objective.BindNodeData(new QuestObjectiveNodeData { Guid = "objective" });
            VisualElement objectiveInspector = objective.CreateInspector(editHandler);
            window.rootVisualElement.Add(objectiveInspector);
            TextField objectiveField = objectiveInspector.Query<TextField>().ToList()
                .Single(field => field.label == "Event Key");
            var interaction = new QuestInteractionEntryNode();
            interaction.BindNodeData(new QuestInteractionEntryNodeData { Guid = "interaction" });
            VisualElement interactionInspector = interaction.CreateInspector(editHandler);
            window.rootVisualElement.Add(interactionInspector);
            TextField targetField = interactionInspector.Q<TextField>();

            objectiveField.value = "  Collect  ";
            targetField.value = "  npc-1  ";

            Assert.That(objective.NodeData.EventKey, Is.EqualTo("Collect"));
            Assert.That(objectiveField.value, Is.EqualTo("Collect"));
            Assert.That(objectiveInspector.Query<IntegerField>().ToList().Any(field => field.label == "Objective Target ID"), Is.True);
            Assert.That(interaction.NodeData.TargetId, Is.EqualTo("npc-1"));
            Assert.That(targetField.value, Is.EqualTo("npc-1"));
        }

        [Test]
        public void WaitForQuest_KeepsItsTitleSizeAndMultiPorts()
        {
            var node = new QuestStateWaitNode();
            node.BindNodeData(new QuestStateWaitNodeData { Guid = "wait", TargetQuestId = 101 });

            Assert.That(node.DefaultSize, Is.EqualTo(new Vector2(200f, 100f)));
            Assert.That(node.title, Is.EqualTo("WAIT QUEST: 101 (TurnedIn)"));
            Port input = node.inputContainer.Children().OfType<Port>().Single();
            Port output = node.outputContainer.Children().OfType<Port>().Single();
            Assert.That(input.portName, Is.EqualTo(QuestPortNames.Input));
            Assert.That(input.direction, Is.EqualTo(Direction.Input));
            Assert.That(input.capacity, Is.EqualTo(Port.Capacity.Multi));
            Assert.That(input.portType, Is.EqualTo(typeof(float)));
            Assert.That(output.portName, Is.EqualTo(QuestPortNames.Next));
            Assert.That(output.direction, Is.EqualTo(Direction.Output));
            Assert.That(output.capacity, Is.EqualTo(Port.Capacity.Multi));
            Assert.That(output.portType, Is.EqualTo(typeof(float)));
        }

        [TestCase("Speaker", "After", "After : Old line")]
        [TestCase("Dialogue", "New dialogue", "Before : New dialogue")]
        public void DialogueLine_InspectorEditsRefreshPreviewAndSupportUndoRedo(string label, string value, string expectedTitle)
        {
            var container = ScriptableObject.CreateInstance<DialogueContainer>();
            createdObjects.Add(container);
            container.Nodes.Add(new DialogueLineNodeData { Guid = "line", SpeakerName = "Before", DialogueText = "Old line" });
            var node = new DialogueLineNode();
            node.BindNodeData(container.Nodes.Single());
            window.rootVisualElement.Add(node);
            int dataEdits = 0;
            VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                (undoName, edit) =>
                {
                    dataEdits++;
                    Undo.RegisterCompleteObjectUndo(container, undoName);
                    edit();
                    EditorUtility.SetDirty(container);
                },
                (_, _) => Assert.Fail("화자와 대화문은 데이터 수정이어야 합니다.")));
            window.rootVisualElement.Add(inspector);
            Assert.That(dataEdits, Is.Zero);
            Assert.That(inspector.Children().OfType<TextField>().Count(), Is.EqualTo(2));
            TextField dialogueField = inspector.Query<TextField>().ToList().Single(field => field.label == "Dialogue");
            Assert.That(dialogueField.multiline, Is.True);
            Assert.That(dialogueField.value, Is.EqualTo("Old line"));

            inspector.Query<TextField>().ToList().Single(field => field.label == label).value = value;

            Assert.That(dataEdits, Is.EqualTo(1));
            Assert.That(node.NodeData.SpeakerName, Is.EqualTo(label == "Speaker" ? value : "Before"));
            Assert.That(node.NodeData.DialogueText, Is.EqualTo(label == "Dialogue" ? value : "Old line"));
            Assert.That(node.title, Is.EqualTo(expectedTitle));
            Assert.That(node.extensionContainer.Children().OfType<Label>().Single().text,
                Is.EqualTo(label == "Dialogue" ? value : "Old line"));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            var restored = new DialogueLineNode();
            restored.BindNodeData(container.Nodes.Single());
            Assert.That(restored.NodeData.SpeakerName, Is.EqualTo("Before"));
            Assert.That(restored.NodeData.DialogueText, Is.EqualTo("Old line"));
            Assert.That(restored.title, Is.EqualTo("Before : Old line"));

            Undo.PerformRedo();
            var redone = new DialogueLineNode();
            redone.BindNodeData(container.Nodes.Single());
            Assert.That(redone.title, Is.EqualTo(expectedTitle));
            Assert.That(redone.extensionContainer.Children().OfType<Label>().Single().text,
                Is.EqualTo(label == "Dialogue" ? value : "Old line"));
        }

        [Test]
        public void DialogueChoice_AddAndRemoveKeepDataPortsAndTitleTogether()
        {
            var node = new DialogueChoiceNode();
            node.BindNodeData(new DialogueChoiceNodeData());
            var choice = new DialogueChoiceData { ChoiceText = "New Choice" };

            node.AddChoice(choice);

            Assert.That(node.NodeData.Choices, Is.EqualTo(new[] { choice }));
            Assert.That(node.title, Is.EqualTo("CHOICE: 1"));
            Assert.That(node.outputContainer.Children().OfType<Port>().Select(port => port.portName),
                Is.EqualTo(new[] { DialoguePortNames.Default, choice.PortName }));

            node.RemoveChoice(choice);

            Assert.That(node.NodeData.Choices, Is.Empty);
            Assert.That(node.title, Is.EqualTo("CHOICE: 0"));
            Assert.That(node.outputContainer.Children().OfType<Port>().Single().portName,
                Is.EqualTo(DialoguePortNames.Default));
        }

        [Test]
        public void DialogueChoice_InvalidAdditionDoesNotModifyDataOrPorts()
        {
            var node = new DialogueChoiceNode();
            node.BindNodeData(new DialogueChoiceNodeData());

            Assert.Throws<System.ArgumentException>(() => node.AddChoice(null));
            Assert.Throws<System.ArgumentException>(() => node.AddChoice(new DialogueChoiceData { PortName = "" }));
            Assert.That(node.NodeData.Choices, Is.Empty);
            Assert.That(node.outputContainer.Children().OfType<Port>().Count(), Is.EqualTo(1));
            Assert.That(node.title, Is.EqualTo("CHOICE: 0"));
        }

        [Test]
        public void DialogueChoice_CopyPasteRegeneratesNodeIdsAndPreservesChoiceLinks()
        {
            var container = ScriptableObject.CreateInstance<DialogueContainer>();
            createdObjects.Add(container);
            var entry = new DialogueEntryNodeData();
            var choice = new DialogueChoiceData { ChoiceText = "Choice" };
            var data = new DialogueChoiceNodeData { Choices = new List<DialogueChoiceData> { choice } };
            var end = new DialogueEndNodeData();
            container.Nodes.AddRange(new NodeBaseData[] { entry, data, end });
            container.NodeLinks.Add(new NodeLinkData
            {
                StartNodeGuid = entry.Guid, StartPortName = DialoguePortNames.Next,
                TargetNodeGuid = data.Guid, TargetPortName = "Input"
            });
            foreach (string portName in new[] { DialoguePortNames.Default, choice.PortName })
            {
                container.NodeLinks.Add(new NodeLinkData
                {
                    StartNodeGuid = data.Guid, StartPortName = portName,
                    TargetNodeGuid = end.Guid, TargetPortName = "Input"
                });
            }

            var view = new UniversalGraphView();
            view.SetContainer(container);
            window.rootVisualElement.Add(view);
            view.ApplyWithoutSaveRequest(() => GraphViewSerializer.LoadGraph(view, container));
            DialogueChoiceNode node = view.nodes.OfType<DialogueChoiceNode>().Single();
            GraphNode endNode = view.nodes.OfType<GraphNode>().Single(candidate => candidate.Data.Guid == end.Guid);
            string copied = view.serializeGraphElements(new GraphElement[] { node, endNode });

            view.ApplyWithoutSaveRequest(() => view.unserializeAndPaste("Paste", copied));

            DialogueChoiceNode pasted = view.nodes.OfType<DialogueChoiceNode>().Single(candidate => candidate != node);
            Assert.That(pasted.NodeData.Guid, Is.Not.EqualTo(data.Guid));
            Assert.That(pasted.NodeData.Choices.Single().PortName, Is.EqualTo(choice.PortName));
            Assert.That(node.NodeData.Choices.Single(), Is.SameAs(choice));
            Assert.That(view.nodes.OfType<GraphNode>().Select(candidate => candidate.Data.Guid).Distinct().Count(),
                Is.EqualTo(5));
            Edge[] pastedEdges = view.edges.Where(edge => edge.output.node == pasted).ToArray();
            Assert.That(pastedEdges.Select(edge => edge.output.portName),
                Is.EquivalentTo(new[] { DialoguePortNames.Default, choice.PortName }));
            Assert.That(pastedEdges.All(edge => edge.input.node != endNode), Is.True);
            Assert.That(pastedEdges.Select(edge => edge.input.node).Distinct().Count(), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DialogueChoice_AddOrDeleteUpdatesPortsAndUndoRedoPreservesLinks(bool deleteChoice)
        {
            var container = ScriptableObject.CreateInstance<DialogueContainer>();
            createdObjects.Add(container);
            container.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            container.Nodes.Add(new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData>
                {
                    new() { PortName = "first", ChoiceText = "First" },
                    new() { PortName = "second", ChoiceText = "Second" }
                }
            });
            container.Nodes.Add(new DialogueEndNodeData { Guid = "end" });
            container.NodeLinks.Add(new NodeLinkData
            {
                StartNodeGuid = "entry", StartPortName = DialoguePortNames.Next,
                TargetNodeGuid = "choice", TargetPortName = "Input"
            });
            foreach (string portName in new[] { DialoguePortNames.Default, "first", "second" })
            {
                container.NodeLinks.Add(new NodeLinkData
                {
                    StartNodeGuid = "choice", StartPortName = portName,
                    TargetNodeGuid = "end", TargetPortName = "Input"
                });
            }
            var view = new UniversalGraphView();
            view.SetContainer(container);
            window.rootVisualElement.Add(view);
            view.ApplyWithoutSaveRequest(() => GraphViewSerializer.LoadGraph(view, container));
            DialogueChoiceNode node = view.nodes.OfType<DialogueChoiceNode>().Single();
            Assert.That(node.title, Is.EqualTo("CHOICE: 2"));
            Assert.That(node.outputContainer.Children().OfType<Port>().Select(port => port.portName),
                Is.EqualTo(new[] { DialoguePortNames.Default, "first", "second" }));
            foreach (Port port in node.outputContainer.Children().OfType<Port>().Skip(1))
            {
                Assert.That(port.contentContainer.Query<Label>().ToList().Any(label => label.text == "Choice"), Is.True);
                Assert.That(port.contentContainer.Q<Label>("type").ClassListContains("universal-graph-hidden"), Is.True);
            }
            Edge keptEdge = view.edges.Single(edge => edge.output.portName == "second");
            int structureEdits = 0;
            VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                (_, _) => Assert.Fail("선택지 추가와 삭제는 구조 수정이어야 합니다."),
                (undoName, edit) =>
                {
                    structureEdits++;
                    Undo.RegisterCompleteObjectUndo(container, undoName);
                    view.ApplyWithoutSaveRequest(edit);
                    GraphViewSerializer.WriteGraphViewToContainer(view, container);
                    EditorUtility.SetDirty(container);
                }));
            window.rootVisualElement.Add(inspector);
            Assert.That(inspector.childCount, Is.EqualTo(2));
            Assert.That(inspector.ElementAt(0), Is.TypeOf<HelpBox>());
            VisualElement choicesSection = inspector.ElementAt(1);
            Assert.That(choicesSection.Children().OfType<Label>().Single().text, Is.EqualTo("Choices"));
            VisualElement choicesContainer = choicesSection.ElementAt(1);
            Button button = deleteChoice
                ? choicesContainer.Children().OfType<Box>().First().Children().OfType<Button>().Single()
                : choicesSection.Children().OfType<Button>().Single(candidate => candidate.text == "+ Add Choice");
            MethodInfo invokeClick = typeof(Clickable).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);
            invokeClick.Invoke(button.clickable, new object[] { null });

            int expectedCount = deleteChoice ? 1 : 3;
            Assert.That(structureEdits, Is.EqualTo(1));
            Assert.That(node.title, Is.EqualTo($"CHOICE: {expectedCount}"));
            Assert.That(node.NodeData.Choices.Count, Is.EqualTo(expectedCount));
            Assert.That(node.outputContainer.Children().OfType<Port>().Count(), Is.EqualTo(expectedCount + 1));
            Assert.That(choicesContainer.Children().OfType<Box>().Count(), Is.EqualTo(expectedCount));
            Assert.That(view.edges.Single(edge => edge.output.portName == "second"), Is.SameAs(keptEdge));
            Assert.That(container.NodeLinks.Count, Is.EqualTo(deleteChoice ? 3 : 4));
            string[] editedPorts = node.outputContainer.Children().OfType<Port>().Select(port => port.portName).ToArray();

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            view.ApplyWithoutSaveRequest(() => GraphViewSerializer.LoadGraph(view, container));
            DialogueChoiceNode restored = view.nodes.OfType<DialogueChoiceNode>().Single();
            Assert.That(restored.title, Is.EqualTo("CHOICE: 2"));
            Assert.That(restored.outputContainer.Children().OfType<Port>().Select(port => port.portName),
                Is.EqualTo(new[] { DialoguePortNames.Default, "first", "second" }));
            Assert.That(view.edges.Count(), Is.EqualTo(4));
            Assert.That(view.edges.Single(edge => edge.output.portName == "first").input.node,
                Is.SameAs(view.nodes.OfType<DialogueEndNode>().Single()));

            Undo.PerformRedo();
            view.ApplyWithoutSaveRequest(() => GraphViewSerializer.LoadGraph(view, container));
            DialogueChoiceNode redone = view.nodes.OfType<DialogueChoiceNode>().Single();
            Assert.That(redone.title, Is.EqualTo($"CHOICE: {expectedCount}"));
            Assert.That(redone.outputContainer.Children().OfType<Port>().Select(port => port.portName), Is.EqualTo(editedPorts));
            Assert.That(view.edges.Count(), Is.EqualTo(deleteChoice ? 3 : 4));
        }

        [TestCase(QuestState.CanComplete)]
        [TestCase(QuestState.TurnedIn)]
        [TestCase(QuestState.Failed)]
        public void WaitForQuest_EditingRequiredStateRefreshesItsTitle(QuestState state)
        {
            var data = new QuestStateWaitNodeData
            {
                Guid = "wait",
                TargetQuestId = 101,
                RequiredState = QuestState.InProgress
            };
            var node = new QuestStateWaitNode();
            node.BindNodeData(data);
            var editNames = new List<string>();
            VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                (name, edit) => { editNames.Add(name); edit(); },
                (_, _) => Assert.Fail("대기 상태 변경은 데이터 수정이어야 합니다.")));
            window.rootVisualElement.Add(inspector);

            PopupField<QuestState> field = inspector.Q<PopupField<QuestState>>();
            Assert.That(field.choices, Has.No.Member(QuestState.ExecutionError));
            field.value = state;

            Assert.That(data.RequiredState, Is.EqualTo(state));
            Assert.That(node.title, Is.EqualTo($"WAIT QUEST: 101 ({state})"));
            Assert.That(editNames, Is.EqualTo(new[] { "Change required quest state" }));
        }

        [Test]
        public void WaitForQuest_OpeningExecutionErrorDoesNotChangeDataOrOfferIt()
        {
            var data = new QuestStateWaitNodeData { Guid = "wait", RequiredState = QuestState.ExecutionError };
            var node = new QuestStateWaitNode();
            node.BindNodeData(data);
            VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                (_, _) => Assert.Fail("인스펙터를 여는 것만으로 데이터를 수정하면 안 됩니다."),
                (_, _) => Assert.Fail("인스펙터를 여는 것만으로 구조를 수정하면 안 됩니다.")));
            window.rootVisualElement.Add(inspector);

            Assert.That(data.RequiredState, Is.EqualTo(QuestState.ExecutionError));
            Assert.That(inspector.Q<PopupField<QuestState>>().choices, Has.No.Member(QuestState.ExecutionError));
        }

        [Test]
        public void AndGate_InspectorExplainsAllInputsWithoutASnapshotCount()
        {
            var node = new QuestAndGateNode();
            node.BindNodeData(new QuestAndGateNodeData { Guid = "and" });
            VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                (_, _) => Assert.Fail("AND 조건 설명은 데이터를 수정하면 안 됩니다."),
                (_, _) => Assert.Fail("AND 조건 설명은 구조를 수정하면 안 됩니다.")));

            Assert.That(((HelpBox)inspector).text,
                Is.EqualTo("서로 다른 모든 입력 분기가 도착하면 진행합니다."));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuestAssetCatalog_RefreshesMissingContainersBeforeReturning(bool destroyContainer)
        {
            System.Type catalogType = typeof(QuestGraphValidator).Assembly.GetType("UniversalGraph.Quest.Editor.QuestAssetCatalog", true);
            FieldInfo containersField = catalogType.GetField("containers", BindingFlags.Static | BindingFlags.NonPublic);
            PropertyInfo containersProperty = catalogType.GetProperty("Containers", BindingFlags.Static | BindingFlags.Public);
            object originalContainers = containersField.GetValue(null);
            var container = ScriptableObject.CreateInstance<QuestContainer>();
            var cachedContainers = new[] { container };
            try
            {
                containersField.SetValue(null, cachedContainers);
                Assert.That(containersProperty.GetValue(null), Is.SameAs(cachedContainers));

                // 프로젝트 변경 알림 없이 null 또는 삭제된 Unity 객체가 남은 상황을 재현합니다.
                if (destroyContainer)
                {
                    Object.DestroyImmediate(container);
                }
                else
                {
                    cachedContainers[0] = null;
                }

                var refreshedContainers = (IReadOnlyList<QuestContainer>)containersProperty.GetValue(null);
                Assert.That(refreshedContainers, Is.Not.SameAs(cachedContainers));
                Assert.That(refreshedContainers.All(item => item != null), Is.True);
                Assert.That(containersProperty.GetValue(null), Is.SameAs(refreshedContainers));
            }
            finally
            {
                containersField.SetValue(null, originalContainers);
                if (container != null)
                {
                    Object.DestroyImmediate(container);
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WaitForQuest_OpenButtonRechecksWhetherTheTargetIsUnique(bool duplicate)
        {
            var firstContainer = ScriptableObject.CreateInstance<QuestContainer>();
            var secondContainer = ScriptableObject.CreateInstance<QuestContainer>();
            createdObjects.Add(firstContainer);
            createdObjects.Add(secondContainer);
            firstContainer.QuestId = 101;
            secondContainer.QuestId = 102;
            System.Type catalogType = typeof(QuestGraphValidator).Assembly.GetType("UniversalGraph.Quest.Editor.QuestAssetCatalog", true);
            FieldInfo containersField = catalogType.GetField("containers", BindingFlags.Static | BindingFlags.NonPublic);
            object originalContainers = containersField.GetValue(null);
            try
            {
                containersField.SetValue(null, new[] { firstContainer, secondContainer });
                var node = new QuestStateWaitNode();
                node.BindNodeData(new QuestStateWaitNodeData { Guid = "wait", TargetQuestId = 101 });
                VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                    (_, edit) => edit(), (_, edit) => edit()));
                window.rootVisualElement.Add(inspector);
                Button button = inspector.Query<Button>().ToList()
                    .Single(candidate => candidate.text == "Open Target Quest Graph");
                Assert.That(button.enabledSelf, Is.True);

                // 인스펙터를 연 뒤 대상 ID가 달라져도 기존 버튼의 활성 상태는 그대로입니다.
                if (duplicate)
                {
                    secondContainer.QuestId = firstContainer.QuestId;
                }
                else
                {
                    firstContainer.QuestId = 103;
                }
                Assert.That(button.enabledSelf, Is.True);
                int openWindowCount = Resources.FindObjectsOfTypeAll<UniversalGraphWindow>().Length;

                // 키보드 포커스나 이벤트 처리 프레임에 의존하지 않고 실제 버튼에 등록된 클릭을 실행합니다.
                MethodInfo invokeClick = typeof(Clickable).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(invokeClick, Is.Not.Null);
                Assert.DoesNotThrow(() => invokeClick.Invoke(button.clickable, new object[] { null }));

                Assert.That(button.enabledSelf, Is.False);
                Assert.That(Resources.FindObjectsOfTypeAll<UniversalGraphWindow>().Length, Is.EqualTo(openWindowCount));
            }
            finally
            {
                containersField.SetValue(null, originalContainers);
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void DialogueEntryField_OnlyOffersExistingEntriesWithoutChangingStoredId(bool hasGraph, bool hasEntry)
        {
            DialogueContainer container = null;
            if (hasGraph)
            {
                container = ScriptableObject.CreateInstance<DialogueContainer>();
                createdObjects.Add(container);
                if (hasEntry)
                {
                    container.Nodes.Add(new DialogueEntryNodeData { Guid = "entry", EntryId = "Talk" });
                }
            }

            var data = new QuestSuggestionNodeData
            {
                Guid = "option",
                DialogueEntryPoint = new DialogueEntryPoint(container, "RemovedEntry")
            };
            var node = new QuestSuggestionNode();
            node.BindNodeData(data);
            int dataEdits = 0;
            VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                (_, edit) => { dataEdits++; edit(); },
                (_, _) => Assert.Fail("Entry 선택은 데이터 수정이어야 합니다.")));
            window.rootVisualElement.Add(inspector);
            PopupField<string> field = inspector.Q<PopupField<string>>();

            Assert.That(dataEdits, Is.Zero);
            Assert.That(data.DialogueEntryPoint.EntryId, Is.EqualTo("RemovedEntry"));
            Assert.That(field.value, Is.EqualTo("RemovedEntry"));
            Assert.That(field.index, Is.EqualTo(-1));
            Assert.That(field.choices, Is.EqualTo(hasEntry ? new[] { "Talk" } : System.Array.Empty<string>()));
            Assert.That(field.enabledSelf, Is.EqualTo(hasEntry));
            Assert.That(field.formatSelectedValueCallback(field.value), Is.EqualTo(
                hasEntry ? "<다시 선택 필요> RemovedEntry" : "선택 가능한 Entry 없음"));

            if (hasEntry)
            {
                field.value = "Talk";
                Assert.That(dataEdits, Is.EqualTo(1));
                Assert.That(data.DialogueEntryPoint.EntryId, Is.EqualTo("Talk"));
            }
        }

        [Test]
        public void DialogueEntryField_ChangingOrClearingGraphDoesNotSelectTheFirstEntry()
        {
            var first = ScriptableObject.CreateInstance<DialogueContainer>();
            var second = ScriptableObject.CreateInstance<DialogueContainer>();
            createdObjects.Add(first);
            createdObjects.Add(second);
            first.Nodes.Add(new DialogueEntryNodeData { Guid = "old", EntryId = "Old" });
            second.Nodes.Add(new DialogueEntryNodeData { Guid = "new", EntryId = "New" });
            var data = new QuestSuggestionNodeData
            {
                Guid = "option",
                DialogueEntryPoint = new DialogueEntryPoint(first, "Old")
            };
            var node = new QuestSuggestionNode();
            node.BindNodeData(data);
            VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                (_, edit) => edit(), (_, _) => Assert.Fail("Entry 선택은 데이터 수정이어야 합니다.")));
            window.rootVisualElement.Add(inspector);
            ObjectField graphField = inspector.Q<ObjectField>();
            PopupField<string> entryField = inspector.Q<PopupField<string>>();
            Assert.That(entryField.index, Is.Zero);

            graphField.value = second;

            Assert.That(data.DialogueEntryPoint.Container, Is.SameAs(second));
            Assert.That(data.DialogueEntryPoint.EntryId, Is.EqualTo("Old"));
            Assert.That(entryField.choices, Is.EqualTo(new[] { "New" }));
            Assert.That(entryField.index, Is.EqualTo(-1));
            Assert.That(entryField.formatSelectedValueCallback(entryField.value), Is.EqualTo("<다시 선택 필요> Old"));
            entryField.value = "New";
            Assert.That(data.DialogueEntryPoint.EntryId, Is.EqualTo("New"));

            graphField.value = null;

            Assert.That(data.DialogueEntryPoint.Container, Is.Null);
            Assert.That(data.DialogueEntryPoint.EntryId, Is.EqualTo("New"));
            Assert.That(entryField.choices, Is.Empty);
            Assert.That(entryField.enabledSelf, Is.False);
            Assert.That(inspector.Query<Button>().ToList().Single(button => button.text == "Open Dialogue Graph").enabledSelf, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuestIdField_DoesNotAddMissingIdEvenWhenTheCatalogIsEmpty(bool hasQuest)
        {
            var container = ScriptableObject.CreateInstance<QuestContainer>();
            createdObjects.Add(container);
            container.QuestId = 101;
            System.Type catalogType = typeof(QuestGraphValidator).Assembly.GetType("UniversalGraph.Quest.Editor.QuestAssetCatalog", true);
            FieldInfo containersField = catalogType.GetField("containers", BindingFlags.Static | BindingFlags.NonPublic);
            object originalContainers = containersField.GetValue(null);
            try
            {
                containersField.SetValue(null, hasQuest ? new[] { container } : System.Array.Empty<QuestContainer>());
                var data = new QuestStateWaitNodeData { Guid = "wait", TargetQuestId = 999 };
                var node = new QuestStateWaitNode();
                node.BindNodeData(data);
                int dataEdits = 0;
                VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                    (_, edit) => { dataEdits++; edit(); },
                    (_, _) => Assert.Fail("Quest 선택은 데이터 수정이어야 합니다.")));
                window.rootVisualElement.Add(inspector);
                PopupField<int> field = inspector.Q<PopupField<int>>();

                Assert.That(dataEdits, Is.Zero);
                Assert.That(data.TargetQuestId, Is.EqualTo(999));
                Assert.That(field.value, Is.EqualTo(999));
                Assert.That(field.index, Is.EqualTo(-1));
                Assert.That(field.choices, Is.EqualTo(hasQuest ? new[] { 101 } : System.Array.Empty<int>()));
                Assert.That(field.enabledSelf, Is.EqualTo(hasQuest));
                Assert.That(field.formatSelectedValueCallback(field.value), Is.EqualTo(
                    hasQuest ? "<존재하지 않음> 999" : "선택 가능한 Quest 없음"));
                if (hasQuest)
                {
                    field.value = 101;
                    Assert.That(dataEdits, Is.EqualTo(1));
                    Assert.That(data.TargetQuestId, Is.EqualTo(101));
                }
            }
            finally
            {
                containersField.SetValue(null, originalContainers);
            }
        }

        [Test]
        public void MethodField_DistinguishesNoneKeyFromTheEmptySelection()
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod("RecordNoneKeyAction", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(DialogueMethodDescriptorFactory.CreateDescriptor(
                method, MethodKind.Action, "None", DialogueMethodOwner.Global,
                out DialogueMethodDescriptor descriptor, out string error), Is.True, error);
            var data = new MethodBindingData();
            int dataEdits = 0;
            VisualElement inspector = MethodBindingInspector.Create(new NodeInspectorEditHandler(
                (_, edit) => { dataEdits++; edit(); },
                (_, _) => Assert.Fail("메서드 선택은 데이터 수정이어야 합니다.")),
                "Action", data, new[] { descriptor });
            window.rootVisualElement.Add(inspector);
            PopupField<string> field = inspector.Q<PopupField<string>>();

            Assert.That(field.choices, Is.EqualTo(new[] { string.Empty, "None" }));
            Assert.That(field.formatSelectedValueCallback(string.Empty), Is.EqualTo("None"));
            Assert.That(field.formatSelectedValueCallback("None"), Is.EqualTo(descriptor.DisplayName));
            Assert.That(descriptor.DisplayName, Is.Not.EqualTo("None"));

            field.value = "None";
            Assert.That(data.Key, Is.EqualTo("None"));
            Assert.That(data.HasKey, Is.True);
            Assert.That(data.Arguments, Is.Empty);

            field.value = string.Empty;
            Assert.That(data.HasKey, Is.False);
            Assert.That(data.Arguments, Is.Empty);
            Assert.That(dataEdits, Is.EqualTo(2));
        }

        [TestCase("RemovedMethod")]
        [TestCase("")]
        [TestCase(null)]
        public void MethodField_DoesNotAddMissingKeyAndStillAllowsClearingIt(string storedKey)
        {
            var data = new MethodBindingData { Key = storedKey };
            string originalKey = data.Key;
            data.Arguments.Add(new MethodArgumentData { ParameterId = "old-value", SerializedValue = "123" });
            List<MethodArgumentData> originalArguments = data.Arguments;
            int dataEdits = 0;
            VisualElement inspector = MethodBindingInspector.Create(new NodeInspectorEditHandler(
                (_, edit) => { dataEdits++; edit(); },
                (_, _) => Assert.Fail("메서드 선택은 데이터 수정이어야 합니다.")),
                "Action", data, System.Array.Empty<MethodDescriptor>());
            window.rootVisualElement.Add(inspector);
            PopupField<string> field = inspector.Q<PopupField<string>>();

            Assert.That(dataEdits, Is.Zero);
            Assert.That(data.Key, Is.EqualTo(originalKey));
            Assert.That(data.Arguments, Is.SameAs(originalArguments));
            Assert.That(field.choices, Is.EqualTo(new[] { string.Empty }));
            Assert.That(field.value, Is.EqualTo(originalKey));
            Assert.That(field.enabledSelf, Is.True);
            if (!string.IsNullOrWhiteSpace(storedKey))
            {
                Assert.That(field.index, Is.EqualTo(-1));
                Assert.That(field.formatSelectedValueCallback(field.value), Is.EqualTo("<등록되지 않음> RemovedMethod"));
                Assert.That(inspector.Query<HelpBox>().ToList().Any(box => box.messageType == HelpBoxMessageType.Error), Is.True);
                field.value = string.Empty;
                Assert.That(dataEdits, Is.EqualTo(1));
                Assert.That(data.Key, Is.Empty);
                Assert.That(data.Arguments, Is.Empty);
                Assert.That(inspector.Query<HelpBox>().ToList(), Is.Empty);
            }
        }

        [Test]
        public void DialogueCandidate_EditingListMetadataKeepsItsGraphTitleAndPorts()
        {
            var container = ScriptableObject.CreateInstance<DialogueContainer>();
            createdObjects.Add(container);
            container.name = "Greeting";
            var data = new DialogueCandidateNodeData
            {
                Guid = "candidate",
                EntryPoint = new DialogueEntryPoint(container, DialogueEntryNodeData.DefaultEntryId)
            };
            var node = new DialogueCandidateNode();
            node.BindNodeData(data);
            var editNames = new List<string>();
            VisualElement inspector = node.CreateInspector(new NodeInspectorEditHandler(
                (name, edit) => { editNames.Add(name); edit(); },
                (_, _) => Assert.Fail("표시 이름과 우선순위는 데이터 수정이어야 합니다.")));
            window.rootVisualElement.Add(inspector);
            string title = node.title;

            inspector.Query<TextField>().ToList().Single(field => field.label == "Display Name").value = "Ask about the quest";
            inspector.Query<IntegerField>().ToList().Single(field => field.label == "Priority").value = 3;

            Assert.That(data.DisplayName, Is.EqualTo("Ask about the quest"));
            Assert.That(data.Priority, Is.EqualTo(3));
            Assert.That(editNames, Is.EqualTo(new[] { "Change dialogue display name", "Change dialogue priority" }));
            Assert.That(node.title, Is.EqualTo(title));
            Assert.That(node.title, Is.EqualTo($"Dialogue Candidate: Greeting ({DialogueEntryNodeData.DefaultEntryId})"));
            Assert.That(node.DefaultSize, Is.EqualTo(new Vector2(250f, 150f)));
            Port input = node.inputContainer.Children().OfType<Port>().Single();
            Assert.That(input.portName, Is.EqualTo(QuestPortNames.Input));
            Assert.That(input.capacity, Is.EqualTo(Port.Capacity.Multi));
            Assert.That(node.outputContainer.Children().OfType<Port>(), Is.Empty);
        }

        [TestCase(QuestState.CanComplete)]
        [TestCase(QuestState.TurnedIn)]
        [TestCase(QuestState.Failed)]
        public void StateChange_OnlyOffersTerminalStatesAndHasNoOutput(QuestState state)
        {
            var node = new QuestFlowEndNode();
            node.BindNodeData(new QuestFlowEndNodeData { Guid = "state", NewState = state });

            Assert.That(node.inputContainer.Children().OfType<Port>().Count(), Is.EqualTo(1));
            Assert.That(node.outputContainer.Children().OfType<Port>(), Is.Empty);
            var field = (PopupField<QuestState>)node.CreateInspector(new NodeInspectorEditHandler(
                (_, _) => Assert.Fail("인스펙터를 여는 것만으로 데이터를 수정하면 안 됩니다."),
                (_, _) => Assert.Fail("인스펙터를 여는 것만으로 구조를 수정하면 안 됩니다.")));
            Assert.That(field.choices, Is.EqualTo(new[]
            {
                QuestState.CanComplete, QuestState.TurnedIn, QuestState.Failed
            }));
            Assert.That(field.value, Is.EqualTo(state));
        }

        [TestCase(QuestState.NotStarted)]
        [TestCase(QuestState.InProgress)]
        [TestCase(QuestState.ExecutionError)]
        [TestCase((QuestState)999)]
        public void StateChange_OpeningInvalidStateDoesNotAddItToChoicesOrChangeData(QuestState stored)
        {
            var data = new QuestFlowEndNodeData { Guid = "state", NewState = stored };
            var node = new QuestFlowEndNode();
            node.BindNodeData(data);
            int dataEdits = 0;
            var field = (PopupField<QuestState>)node.CreateInspector(new NodeInspectorEditHandler(
                (_, edit) => { dataEdits++; edit(); },
                (_, _) => Assert.Fail("종료 상태 선택은 포트를 바꾸지 않는 데이터 수정이어야 합니다.")));
            window.rootVisualElement.Add(field);

            Assert.That(dataEdits, Is.Zero);
            Assert.That(data.NewState, Is.EqualTo(stored));
            Assert.That(node.title, Is.EqualTo($"STATE: {stored}"));
            Assert.That(node.outputContainer.Children().OfType<Port>(), Is.Empty);
            Assert.That(field.choices, Is.EqualTo(new[] { QuestState.CanComplete, QuestState.TurnedIn, QuestState.Failed }));
        }

        [TestCase(QuestState.CanComplete)]
        [TestCase(QuestState.TurnedIn)]
        [TestCase(QuestState.Failed)]
        public void StateChange_DataEditAndUndoPreserveItsInputLinks(QuestState state)
        {
            var container = ScriptableObject.CreateInstance<QuestContainer>();
            createdObjects.Add(container);
            container.QuestId = 1;
            var view = new UniversalGraphView();
            view.SetContainer(container);
            window.rootVisualElement.Add(view);

            var start = new QuestStartNode();
            start.BindNodeData(new QuestStartNodeData { Guid = "start" });
            var node = new QuestFlowEndNode();
            QuestState previous = state == QuestState.CanComplete ? QuestState.TurnedIn : QuestState.CanComplete;
            node.BindNodeData(new QuestFlowEndNodeData { Guid = "state", NewState = previous });
            view.AddElement(start);
            view.AddElement(node);
            Port input = node.inputContainer.Children().OfType<Port>().Single();
            Edge edge = start.outputContainer.Children().OfType<Port>().Single().ConnectTo(input);
            view.AddElement(edge);
            GraphViewSerializer.WriteGraphViewToContainer(view, container);

            int dataEdits = 0;
            var editHandler = new NodeInspectorEditHandler(
                (undoName, edit) =>
                {
                    dataEdits++;
                    // 종료 상태 선택은 연결을 다시 쓰지 않고 데이터만 Undo에 기록합니다.
                    Undo.RegisterCompleteObjectUndo(container, undoName);
                    edit();
                    EditorUtility.SetDirty(container);
                },
                (_, _) => Assert.Fail("종료 상태 선택은 구조 수정이어야 할 이유가 없습니다."));
            var field = (PopupField<QuestState>)node.CreateInspector(editHandler);
            window.rootVisualElement.Add(field);

            field.value = state;

            Assert.That(dataEdits, Is.EqualTo(1));
            Assert.That(node.NodeData.NewState, Is.EqualTo(state));
            Assert.That(node.title, Is.EqualTo($"STATE: {state}"));
            Assert.That(node.outputContainer.Children().OfType<Port>(), Is.Empty);
            Assert.That(node.inputContainer.Children().OfType<Port>().Single(), Is.SameAs(input));
            Assert.That(input.connected, Is.True);
            Assert.That(view.edges.Single(), Is.SameAs(edge));
            Assert.That(view.edges.Count(), Is.EqualTo(1));
            Assert.That(container.NodeLinks.Count, Is.EqualTo(1));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();

            Assert.That(container.Nodes.OfType<QuestFlowEndNodeData>().Single().NewState,
                Is.EqualTo(previous));
            Assert.That(container.NodeLinks.Count, Is.EqualTo(1));
            view.ApplyWithoutSaveRequest(() => GraphViewSerializer.LoadGraph(view, container));
            Assert.That(view.edges.Count(), Is.EqualTo(1));
            QuestFlowEndNode restored = view.nodes.OfType<QuestFlowEndNode>().Single();
            Assert.That(restored.inputContainer.Children().OfType<Port>().Single().connected, Is.True);
            Assert.That(restored.outputContainer.Children().OfType<Port>(), Is.Empty);
            Assert.That(restored.title, Is.EqualTo($"STATE: {previous}"));

            Undo.PerformRedo();
            Assert.That(container.Nodes.OfType<QuestFlowEndNodeData>().Single().NewState, Is.EqualTo(state));
            Assert.That(container.NodeLinks.Count, Is.EqualTo(1));
        }

        [Test]
        public void StateChange_DefaultsToCanComplete()
        {
            Assert.That(new QuestFlowEndNodeData().NewState, Is.EqualTo(QuestState.CanComplete));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Condition_CatalogAndGraphSerializationKeepMultipleLinksOnBothOutputs(bool customCondition)
        {
            var container = ScriptableObject.CreateInstance<QuestContainer>();
            createdObjects.Add(container);
            container.QuestId = 1;
            container.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            NodeBaseData data = customCondition
                ? new QuestConditionNodeData()
                : new QuestStateConditionNodeData { QuestId = container.QuestId };
            data.Guid = "condition";
            data.Position = new Vector2(220f, 80f);
            container.Nodes.Add(data);
            container.Nodes.Add(new QuestFlowEndNodeData { Guid = "first", NewState = QuestState.Failed });
            container.Nodes.Add(new QuestFlowEndNodeData { Guid = "second", NewState = QuestState.Failed });
            container.NodeLinks.Add(new NodeLinkData
            {
                StartNodeGuid = "start",
                StartPortName = QuestPortNames.Next,
                TargetNodeGuid = "condition",
                TargetPortName = QuestPortNames.Input
            });
            foreach (string port in new[] { QuestPortNames.True, QuestPortNames.False })
            {
                foreach (string target in new[] { "first", "second" })
                {
                    container.NodeLinks.Add(new NodeLinkData
                    {
                        StartNodeGuid = "condition",
                        StartPortName = port,
                        TargetNodeGuid = target,
                        TargetPortName = QuestPortNames.Input
                    });
                }
            }

            GraphNodeCatalog.NodeDefinition definition = GraphNodeCatalog.GetNodeCatalog(container)
                .Single(candidate => candidate.DataType == data.GetType());
            Assert.That(definition.ViewType, Is.EqualTo(customCondition
                ? typeof(QuestConditionNode) : typeof(QuestStateConditionNode)));

            var restored = ScriptableObject.CreateInstance<QuestContainer>();
            createdObjects.Add(restored);
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(container), restored);
            Assert.That(restored.Nodes.Single(item => item.Guid == "condition").Position, Is.EqualTo(data.Position));
            var view = new UniversalGraphView();
            view.SetContainer(restored);
            window.rootVisualElement.Add(view);
            view.ApplyWithoutSaveRequest(() => GraphViewSerializer.LoadGraph(view, restored));

            GraphNode node = view.nodes.OfType<GraphNode>().Single(item => item.GetType() == definition.ViewType);
            Port input = node.inputContainer.Children().OfType<Port>().Single();
            Assert.That(input.portName, Is.EqualTo(QuestPortNames.Input));
            Assert.That(input.capacity, Is.EqualTo(Port.Capacity.Multi));
            Assert.That(input.connections.Count(), Is.EqualTo(1));
            Port[] outputs = node.outputContainer.Children().OfType<Port>().ToArray();
            Assert.That(outputs.Select(port => port.portName),
                Is.EquivalentTo(new[] { QuestPortNames.True, QuestPortNames.False }));
            foreach (Port output in outputs)
            {
                Assert.That(output.capacity, Is.EqualTo(Port.Capacity.Multi));
                Assert.That(output.connections.Count(), Is.EqualTo(2));
            }
            GraphViewSerializer.WriteGraphViewToContainer(view, restored);
            Assert.That(restored.Nodes.Single(item => item.Guid == "condition").GetType(), Is.EqualTo(data.GetType()));
            Assert.That(restored.NodeLinks.Count, Is.EqualTo(5));
        }
    }
}
