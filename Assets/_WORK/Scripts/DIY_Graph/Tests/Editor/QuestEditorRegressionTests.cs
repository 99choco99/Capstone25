using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
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
            Assert.That(interaction.NodeData.TargetId, Is.EqualTo("npc-1"));
            Assert.That(targetField.value, Is.EqualTo("npc-1"));
        }

        [TestCase(QuestState.InProgress, 1)]
        [TestCase(QuestState.CanComplete, 0)]
        [TestCase(QuestState.TurnedIn, 0)]
        public void StateChange_OnlyInProgressHasAnOutput(QuestState state, int outputCount)
        {
            var node = new QuestStateChangeNode();
            node.BindNodeData(new QuestStateChangeNodeData { Guid = "state", NewState = state });

            Assert.That(node.inputContainer.Children().OfType<Port>().Count(), Is.EqualTo(1));
            Assert.That(node.outputContainer.Children().OfType<Port>().Count(), Is.EqualTo(outputCount));
        }

        [TestCase(QuestState.CanComplete)]
        [TestCase(QuestState.TurnedIn)]
        public void StateChange_RemovesOnlyOutgoingLinksAndUndoRestoresThem(QuestState state)
        {
            var graph = ScriptableObject.CreateInstance<QuestContainer>();
            createdObjects.Add(graph);
            graph.QuestId = 1;
            var view = new UniversalGraphView();
            view.SetContainer(graph);
            window.rootVisualElement.Add(view);

            var start = new QuestStartNode();
            start.BindNodeData(new QuestStartNodeData { Guid = "start" });
            var node = new QuestStateChangeNode();
            node.BindNodeData(new QuestStateChangeNodeData { Guid = "state" });
            var end = new QuestFailNode();
            end.BindNodeData(new QuestFailNodeData { Guid = "end" });
            view.AddElement(start);
            view.AddElement(node);
            view.AddElement(end);
            Port input = node.inputContainer.Children().OfType<Port>().Single();
            Port output = node.outputContainer.Children().OfType<Port>().Single();
            Port endInput = end.inputContainer.Children().OfType<Port>().Single();
            view.AddElement(start.outputContainer.Children().OfType<Port>().Single().ConnectTo(input));
            view.AddElement(output.ConnectTo(endInput));
            GraphViewSerializer.WriteGraphViewToContainer(view, graph);

            int structureEdits = 0;
            var editHandler = new NodeInspectorEditHandler(
                (_, _) => Assert.Fail("상태 변경은 연결까지 저장하는 구조 수정이어야 합니다."),
                (undoName, edit) =>
                {
                    structureEdits++;
                    // 실제 Window와 같은 순서로 데이터와 연결을 하나의 Undo에 기록합니다.
                    Undo.RegisterCompleteObjectUndo(graph, undoName);
                    view.ApplyWithoutSaveRequest(edit);
                    GraphViewSerializer.WriteGraphViewToContainer(view, graph);
                    EditorUtility.SetDirty(graph);
                });
            var field = (PopupField<QuestState>)node.CreateInspector(editHandler);
            window.rootVisualElement.Add(field);

            field.value = state;

            Assert.That(structureEdits, Is.EqualTo(1));
            Assert.That(node.NodeData.NewState, Is.EqualTo(state));
            Assert.That(node.outputContainer.Children().OfType<Port>(), Is.Empty);
            Assert.That(input.connected, Is.True);
            Assert.That(endInput.connected, Is.False);
            Assert.That(view.edges.Count(), Is.EqualTo(1));
            Assert.That(graph.NodeLinks.Count, Is.EqualTo(1));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();

            Assert.That(graph.Nodes.OfType<QuestStateChangeNodeData>().Single().NewState,
                Is.EqualTo(QuestState.InProgress));
            Assert.That(graph.NodeLinks.Count, Is.EqualTo(2));
            view.ApplyWithoutSaveRequest(() => GraphViewSerializer.LoadGraph(view, graph));
            Assert.That(view.edges.Count(), Is.EqualTo(2));
            QuestStateChangeNode restored = view.nodes.OfType<QuestStateChangeNode>().Single();
            Assert.That(restored.outputContainer.Children().OfType<Port>().Single().connected, Is.True);
        }

        [Test]
        public void StateChange_ReturningToInProgressCreatesAnEmptyNextPort()
        {
            var node = new QuestStateChangeNode();
            node.BindNodeData(new QuestStateChangeNodeData
            {
                Guid = "state",
                NewState = QuestState.CanComplete
            });
            var field = (PopupField<QuestState>)node.CreateInspector(new NodeInspectorEditHandler(
                (_, _) => Assert.Fail("상태 변경은 구조 수정이어야 합니다."), (_, edit) => edit()));
            window.rootVisualElement.Add(field);

            field.value = QuestState.InProgress;

            Port next = node.outputContainer.Children().OfType<Port>().Single();
            Assert.That(next.portName, Is.EqualTo(QuestPortNames.Next));
            Assert.That(next.connected, Is.False);
            Assert.That(node.inputContainer.Children().OfType<Port>().Count(), Is.EqualTo(1));
        }
    }
}
