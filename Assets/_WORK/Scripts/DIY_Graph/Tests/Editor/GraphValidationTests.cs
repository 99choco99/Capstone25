using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UniversalGraph.Dialogue.Editor;
using UniversalGraph.Editor;
using UniversalGraph.Quest.Editor;

namespace UniversalGraph.Tests
{
    public sealed class GraphValidationTests
    {
        private readonly List<Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void DialogueValidator_ReportsEntryWithoutNextLink()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueStartNodeData { Guid = "entry", EntryId = "Default" });

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationContext(graph), issues);

            Assert.That(issues.Any(issue => issue.Code == "DIALOGUE_OUTPUT_COUNT"
                                            && issue.Severity == GraphValidationSeverity.Error), Is.True);
        }

        [Test]
        public void DialogueValidator_AcceptsMinimalConnectedLine()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueStartNodeData { Guid = "entry", EntryId = "Default" });
            graph.Nodes.Add(new DialogueNodeData { Guid = "line", DialogueText = "Hello" });
            graph.NodeLinks.Add(Link("entry", "Next", "line"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationContext(graph), issues);

            Assert.That(issues.Any(issue => issue.Severity == GraphValidationSeverity.Error), Is.False);
        }

        [Test]
        public void DialogueValidator_AcceptsConnectedChoiceNode()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var choiceNode = new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData>
                {
                    new()
                    {
                        PortName = "accept",
                        ChoiceText = "Accept"
                    }
                }
            };
            graph.Nodes.Add(new DialogueStartNodeData { Guid = "entry", EntryId = "Default" });
            graph.Nodes.Add(choiceNode);
            graph.Nodes.Add(new DialogueNodeData { Guid = "line", DialogueText = "Accepted" });
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));
            graph.NodeLinks.Add(Link("choice", "accept", "line"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationContext(graph), issues);

            Assert.That(issues.Any(issue => issue.Severity == GraphValidationSeverity.Error), Is.False);
        }

        [Test]
        public void DialogueValidator_RejectsChoiceUsingDefaultPortName()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueStartNodeData { Guid = "entry", EntryId = "Default" });
            graph.Nodes.Add(new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData>
                {
                    new()
                    {
                        PortName = DialogueChoiceNodeData.DefaultPortName,
                        ChoiceText = "Invalid"
                    }
                }
            });
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationContext(graph), issues);

            Assert.That(issues.Any(issue => issue.Code == "DIALOGUE_DUPLICATE_CHOICE"
                                            && issue.NodeGuid == "choice"), Is.True);
        }

        [Test]
        public void GraphSerializer_RejectsDuplicateLinkBeforeDrawingEdges()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueStartNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueNodeData { Guid = "line" });
            graph.NodeLinks.Add(Link("entry", "Next", "line"));
            graph.NodeLinks.Add(Link("entry", "Next", "line"));

            var graphView = new UniversalGraphView();
            System.InvalidOperationException exception = Assert.Throws<System.InvalidOperationException>(
                () => GraphSerializer.LoadGraph(graphView, graph));

            Assert.That(exception.Message, Does.Contain("DUPLICATE_LINK"));
        }

        [Test]
        public void CycleFinder_ReturnsOnlyNodesInsideCycle()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueActionNodeData { Guid = "prefix" });
            graph.Nodes.Add(new DialogueActionNodeData { Guid = "a" });
            graph.Nodes.Add(new DialogueActionNodeData { Guid = "b" });
            graph.NodeLinks.Add(Link("prefix", "Next", "a"));
            graph.NodeLinks.Add(Link("a", "Next", "b"));
            graph.NodeLinks.Add(Link("b", "Next", "a"));

            HashSet<string> cycleNodes = GraphValidatorRegistry.FindCycleNodes(
                new GraphValidationContext(graph),
                _ => true);

            Assert.That(cycleNodes, Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void QuestValidator_ReportsMetadataAndFlowIssuesForEmptyAsset()
        {
            QuestContainer graph = CreateAsset<QuestContainer>();
            graph.id = -99999;

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new QuestGraphValidator()).Validate(new GraphValidationContext(graph), issues);

            Assert.That(issues.Select(issue => issue.Code), Is.EquivalentTo(new[]
            {
                "QUEST_ID",
                "QUEST_EMPTY_GRAPH"
            }));
        }

        [Test]
        public void QuestValidator_ReportsDialogueEntryWithoutNextLink()
        {
            DialogueContainer dialogue = CreateAsset<DialogueContainer>();
            dialogue.Nodes.Add(new DialogueStartNodeData { Guid = "dialogue-entry" });

            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.id = -99998;
            quest.Nodes.Add(new QuestEventEntryNodeData { Guid = "interaction" });
            quest.Nodes.Add(new DialogueRequestNodeData
            {
                Guid = "request",
                DialogueReference = new DialogueReference(dialogue, DialogueStartNodeData.DefaultEntryId)
            });
            quest.NodeLinks.Add(Link("interaction", "Next", "request"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new QuestGraphValidator()).Validate(new GraphValidationContext(quest), issues);

            Assert.That(
                issues.Any(issue => issue.Code == "QUEST_DIALOGUE_ENTRY"
                                    && issue.Severity == GraphValidationSeverity.Error),
                Is.True);
        }

        [Test]
        public void SelectingGraphNode_NotifiesInspectorTargetImmediately()
        {
            var graphView = new UniversalGraphView();
            var node = new DialogueNode();
            node.BindNodeData(new DialogueNodeData
            {
                Guid = "selected-line",
                DialogueText = "Selection test"
            });
            GraphNode selectedNode = null;
            graphView.Selected += selected => selectedNode = selected;
            graphView.AddElement(node);

            graphView.AddToSelection(node);

            Assert.That(selectedNode, Is.SameAs(node));
            Assert.That(graphView.selection, Does.Contain(node));
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
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
    }
}
