using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
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
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" });

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

            Assert.That(issues.Any(issue => issue.Code == "DIALOGUE_OUTPUT_COUNT"
                                            && issue.Severity == GraphValidationSeverity.Error), Is.True);
        }

        [Test]
        public void DialogueValidator_AcceptsMinimalConnectedLine()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" });
            graph.Nodes.Add(new DialogueLineNodeData { Guid = "line", DialogueText = "Hello" });
            graph.Nodes.Add(new DialogueEndNodeData { Guid = "end" });
            graph.NodeLinks.Add(Link("entry", "Next", "line"));
            graph.NodeLinks.Add(Link("line", "Next", "end"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

            Assert.That(issues.Any(issue => issue.Severity == GraphValidationSeverity.Error), Is.False);
        }

        [Test]
        public void DialogueValidator_ReportsNullMethodBindingWithoutThrowing()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueLineNodeData { Guid = "line", EnterAction = null });
            graph.NodeLinks.Add(Link("entry", DialoguePortNames.Next, "line"));

            IReadOnlyList<GraphValidationIssue> issues = GraphValidatorRegistry.Validate(graph);

            Assert.That(
                issues.Any(issue => issue.Code == "DIALOGUE_BINDING_DATA" && issue.NodeGuid == "line"),
                Is.True);
            Assert.That(issues.Any(issue => issue.Code == "VALIDATOR_EXCEPTION"), Is.False);
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
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" });
            graph.Nodes.Add(choiceNode);
            graph.Nodes.Add(new DialogueLineNodeData { Guid = "line", DialogueText = "Accepted" });
            graph.Nodes.Add(new DialogueEndNodeData { Guid = "end" });
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));
            graph.NodeLinks.Add(Link("choice", "accept", "line"));
            graph.NodeLinks.Add(Link("line", "Next", "end"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

            Assert.That(issues.Any(issue => issue.Severity == GraphValidationSeverity.Error), Is.False);
        }

        [Test]
        public void DialogueValidator_RejectsChoiceUsingDefaultPortName()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" });
            graph.Nodes.Add(new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData>
                {
                    new()
                    {
                        PortName = DialoguePortNames.Default,
                        ChoiceText = "Invalid"
                    }
                }
            });
            graph.NodeLinks.Add(Link("entry", "Next", "choice"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

            Assert.That(issues.Any(issue => issue.Code == "DIALOGUE_DUPLICATE_CHOICE"
                                            && issue.NodeGuid == "choice"), Is.True);
        }

        [Test]
        public void GraphViewSerializer_RejectsDuplicateLinkBeforeDrawingEdges()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueLineNodeData { Guid = "line" });
            graph.NodeLinks.Add(Link("entry", "Next", "line"));
            graph.NodeLinks.Add(Link("entry", "Next", "line"));

            var graphView = new UniversalGraphView();
            System.InvalidOperationException exception = Assert.Throws<System.InvalidOperationException>(
                () => GraphViewSerializer.LoadGraph(graphView, graph));

            Assert.That(exception.Message, Does.Contain("DUPLICATE_LINK"));
        }

        [Test]
        public void GraphValidator_RejectsCurrentLinkWithoutTargetPort()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueLineNodeData { Guid = "line" });
            GraphAssetMigrator.EnsureCurrent(graph);

            NodeLinkData link = Link("entry", DialoguePortNames.Next, "line");
            link.TargetPortName = string.Empty;
            graph.NodeLinks.Add(link);

            IReadOnlyList<GraphValidationIssue> issues = GraphValidatorRegistry.Validate(graph);

            Assert.That(
                issues.Any(issue => issue.Code == "MISSING_TARGET_PORT"
                                    && issue.Severity == GraphValidationSeverity.Error),
                Is.True);
        }

        [Test]
        public void GraphViewSerializer_RejectsMultipleLinksFromSingleOutput()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            var graphView = new UniversalGraphView();
            var entryNode = new DialogueEntryNode();
            var firstLineNode = new DialogueLineNode();
            var secondLineNode = new DialogueLineNode();
            entryNode.BindNodeData(new DialogueEntryNodeData { Guid = "entry" });
            firstLineNode.BindNodeData(new DialogueLineNodeData { Guid = "line-1" });
            secondLineNode.BindNodeData(new DialogueLineNodeData { Guid = "line-2" });

            graphView.ApplyWithoutSaveRequest(() =>
            {
                graphView.AddElement(entryNode);
                graphView.AddElement(firstLineNode);
                graphView.AddElement(secondLineNode);

                Port output = entryNode.outputContainer.Children().OfType<Port>().Single();
                Port firstInput = firstLineNode.inputContainer.Children().OfType<Port>().Single();
                Port secondInput = secondLineNode.inputContainer.Children().OfType<Port>().Single();
                AddEdge(output, firstInput);
                AddEdge(output, secondInput);

                void AddEdge(Port source, Port target)
                {
                    var edge = new Edge { output = source, input = target };
                    source.Connect(edge);
                    target.Connect(edge);
                    graphView.AddElement(edge);
                }
            });

            System.InvalidOperationException exception = Assert.Throws<System.InvalidOperationException>(
                () => GraphViewSerializer.WriteGraphViewToContainer(graphView, graph));

            Assert.That(exception.Message, Does.Contain("하나의 연결만 허용"));
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
                new GraphValidationIndex(graph),
                _ => true);

            Assert.That(cycleNodes, Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void GraphValidationIndex_ExcludesLinksToMissingNodes()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            graph.NodeLinks.Add(Link("entry", DialoguePortNames.Next, "missing"));

            var index = new GraphValidationIndex(graph);

            Assert.That(index.GetOutgoing("entry"), Is.Empty);
            Assert.That(index.GetIncoming("missing"), Is.Empty);
        }

        [Test]
        public void QuestValidator_ReportsMetadataAndFlowIssuesForEmptyAsset()
        {
            QuestContainer graph = CreateAsset<QuestContainer>();
            graph.QuestId = -99999;

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new QuestGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

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
            dialogue.Nodes.Add(new DialogueEntryNodeData { Guid = "dialogue-entry" });

            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = -99998;
            quest.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "interaction" });
            quest.Nodes.Add(new DialogueCandidateNodeData
            {
                Guid = "candidate",
                EntryPoint = new DialogueEntryPoint(dialogue, DialogueEntryNodeData.DefaultEntryId)
            });
            quest.NodeLinks.Add(Link("interaction", "Next", "candidate"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new QuestGraphValidator()).Validate(new GraphValidationIndex(quest), issues);

            Assert.That(
                issues.Any(issue => issue.Code == "QUEST_DIALOGUE_ENTRY"
                                    && issue.Severity == GraphValidationSeverity.Error),
                Is.True);
        }

        [Test]
        public void QuestValidator_AcceptsAvailableOfferInInteractionRoute()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 990001;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestRewardNodeData { Guid = "reward" });
            quest.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "interaction" });
            quest.Nodes.Add(new QuestOfferNodeData { Guid = "offer" });
            quest.NodeLinks.Add(Link("start", "Next", "reward"));
            quest.NodeLinks.Add(Link("interaction", "Next", "offer"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new QuestGraphValidator()).Validate(new GraphValidationIndex(quest), issues);

            Assert.That(
                issues.Any(issue => issue.NodeGuid == "offer"
                                    && (issue.Code == "QUEST_UNSUPPORTED_NODE"
                                        || issue.Code == "QUEST_ROUTE_UNSAFE_NODE")),
                Is.False);
        }

        [Test]
        public void QuestValidator_WarnsWhenBlockedOfferHasNoReason()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 990002;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestRewardNodeData { Guid = "reward" });
            quest.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "interaction" });
            quest.Nodes.Add(new QuestOfferNodeData
            {
                Guid = "offer",
                IsAvailable = false,
                BlockReason = string.Empty
            });
            quest.NodeLinks.Add(Link("start", "Next", "reward"));
            quest.NodeLinks.Add(Link("interaction", "Next", "offer"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new QuestGraphValidator()).Validate(new GraphValidationIndex(quest), issues);

            Assert.That(
                issues.Any(issue => issue.Code == "QUEST_OFFER_BLOCK_REASON"
                                    && issue.NodeGuid == "offer"
                                    && issue.Severity == GraphValidationSeverity.Warning),
                Is.True);
        }

        [Test]
        public void QuestValidator_RejectsOfferInProgressionFlow()
        {
            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 990003;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestOfferNodeData { Guid = "offer" });
            quest.NodeLinks.Add(Link("start", "Next", "offer"));

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new QuestGraphValidator()).Validate(new GraphValidationIndex(quest), issues);

            Assert.That(
                issues.Any(issue => issue.Code == "QUEST_OFFER_IN_PROGRESS_FLOW"
                                    && issue.NodeGuid == "offer"
                                    && issue.Severity == GraphValidationSeverity.Error),
                Is.True);
        }

        [Test]
        public void SelectingGraphNode_NotifiesInspectorTargetImmediately()
        {
            var graphView = new UniversalGraphView();
            var lineNode = new DialogueLineNode();
            lineNode.BindNodeData(new DialogueLineNodeData
            {
                Guid = "selected-line",
                DialogueText = "Selection test"
            });
            GraphNode selectedNode = null;
            graphView.Selected += selected => selectedNode = selected;
            graphView.AddElement(lineNode);

            graphView.AddToSelection(lineNode);

            Assert.That(selectedNode, Is.SameAs(lineNode));
            Assert.That(graphView.selection, Does.Contain(lineNode));
        }

        [Test]
        public void UnselectingGraphNode_ClearsInspectorTarget()
        {
            var graphView = new UniversalGraphView();
            var lineNode = new DialogueLineNode();
            lineNode.BindNodeData(new DialogueLineNodeData { Guid = "selected-line" });
            GraphNode selectedNode = null;
            graphView.Selected += selected => selectedNode = selected;
            graphView.AddElement(lineNode);
            graphView.AddToSelection(lineNode);

            graphView.RemoveFromSelection(lineNode);

            Assert.That(selectedNode, Is.Null);
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
