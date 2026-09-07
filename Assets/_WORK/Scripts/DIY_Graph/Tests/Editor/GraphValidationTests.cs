using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public void MethodBuildValidator_UsesReflectionAndCountsEachAttributeOnce()
        {
            MethodInfo action = typeof(UniversalGraphRuntimeTests).GetMethod("RecordInvokerAction", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo condition = typeof(UniversalGraphRuntimeTests).GetMethod("IsDialogueChoiceVisible", BindingFlags.Static | BindingFlags.NonPublic);
            System.Type validator = typeof(GraphValidatorRegistry).Assembly.GetType("UniversalGraph.Editor.GraphMethodBuildValidator", true);
            MethodInfo validate = validator.GetMethod("Validate", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(IEnumerable<MethodInfo>) }, null);

            // TypeCache 결과에 같은 메서드가 여러 번 포함되어도 각 도메인의 Attribute는 한 번만 검사합니다.
            object report = validate.Invoke(null, new object[] { new[] { action, action, condition, condition } });
            System.Type reportType = report.GetType();
            var errors = (List<string>)reportType.GetField("Errors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(report);

            Assert.That(errors, Is.Empty);
            Assert.That(reportType.GetField("DialogueMethodCount", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(report), Is.EqualTo(2));
            Assert.That(reportType.GetField("QuestMethodCount", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(report), Is.EqualTo(2));
        }

        [Test]
        public void DialogueValidator_ReportsEntryWithoutNextLink()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" });

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

            Assert.That(issues.Any(issue => issue.IssueKind == "DIALOGUE_OUTPUT_COUNT"
                                            && issue.Severity == GraphValidationSeverity.Error), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MethodBindingInspector_ShowsOnlyActualArgumentErrors(bool invalidValue)
        {
            MethodInfo method = typeof(UniversalGraphRuntimeTests).GetMethod(
                invalidValue ? "IsDialogueChoiceVisible" : "RecordInvokerAction", BindingFlags.Static | BindingFlags.NonPublic);
            MethodKind kind = invalidValue ? MethodKind.Condition : MethodKind.Action;
            Assert.That(DialogueMethodDescriptorFactory.TryCreateFromReflection(
                method, kind, "test.inspector", DialogueMethodOwner.Global, out DialogueMethodDescriptor descriptor, out string error),
                Is.True, error);
            var binding = new MethodBindingData
            {
                Key = descriptor.Key,
                Arguments = MethodArgumentCodec.CreateDefaultArgumentData(descriptor)
            };
            if (invalidValue)
            {
                binding.Arguments[0].SerializedValue = "invalid-bool";
                Assert.That(MethodArgumentCodec.TryDecodeAllArgumentData(binding.Arguments, descriptor, out _, out error), Is.False);
            }

            VisualElement inspector = MethodBindingInspector.Create(null, "Action", binding, new[] { descriptor });
            List<HelpBox> messages = inspector.Query<HelpBox>().ToList();
            if (invalidValue)
            {
                Assert.That(messages.Single().text, Is.EqualTo(error));
                Assert.That(inspector.Query<Button>().ToList().Any(button => button.text.Contains("호환되는 값 유지")), Is.True);
            }
            else
            {
                Assert.That(messages, Is.Empty);
            }
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
                issues.Any(issue => issue.IssueKind == "DIALOGUE_BINDING_DATA" && issue.NodeGuid == "line"),
                Is.True);
            Assert.That(issues.Any(issue => issue.IssueKind == "VALIDATOR_EXCEPTION"), Is.False);
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

            Assert.That(issues.Any(issue => issue.IssueKind == "DIALOGUE_DUPLICATE_CHOICE"
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
                issues.Any(issue => issue.IssueKind == "MISSING_TARGET_PORT"
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
        public void GraphValidationIndex_IndexesStructurallyValidNodesAndLinks()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueEndNodeData { Guid = "end" });
            graph.NodeLinks.Add(Link("entry", DialoguePortNames.Next, "end"));

            Assert.That(GraphValidatorRegistry.ValidateStructure(graph), Is.Empty);

            var index = new GraphValidationIndex(graph);

            Assert.That(index.GetLinkInStartPort("entry"), Has.Count.EqualTo(1));
            Assert.That(index.GetLinkInTargetPorts("end"), Has.Count.EqualTo(1));
            Assert.That(index.GetReachableNode(new[] { "entry" }), Is.EquivalentTo(new[] { "entry", "end" }));
        }

        [TestCase("NULL_NODE")]
        [TestCase("EMPTY_NODE_GUID")]
        [TestCase("DUPLICATE_NODE_GUID")]
        [TestCase("MISSING_LINK_NODE")]
        public void GraphValidator_StopsBeforeDomainValidationWhenStructureIsInvalid(string issueKind)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            switch (issueKind)
            {
                case "NULL_NODE":
                    graph.Nodes.Add(null);
                    break;
                case "EMPTY_NODE_GUID":
                    graph.Nodes.Add(new DialogueEndNodeData { Guid = string.Empty });
                    break;
                case "DUPLICATE_NODE_GUID":
                    graph.Nodes.Add(new DialogueEndNodeData { Guid = "entry" });
                    break;
                case "MISSING_LINK_NODE":
                    graph.NodeLinks.Add(Link("entry", DialoguePortNames.Next, "missing"));
                    break;
            }

            IReadOnlyList<GraphValidationIssue> issues = GraphValidatorRegistry.Validate(graph);

            Assert.That(issues.Select(issue => issue.IssueKind), Is.EquivalentTo(new[] { issueKind }));
        }

        [Test]
        public void GraphViewSerializer_AllowsEditingGraphWithDomainError()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            var graphView = new UniversalGraphView();

            Assert.That(GraphValidatorRegistry.Validate(graph).Any(issue => issue.IssueKind == "DIALOGUE_OUTPUT_COUNT"), Is.True);
            Assert.DoesNotThrow(() => GraphViewSerializer.LoadGraph(graphView, graph));
            Assert.That(graphView.nodes.ToList(), Has.Count.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GraphValidator_ReportsMissingCollectionWithoutDomainErrors(bool missingNodes)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            GraphAssetMigrator.EnsureCurrent(graph);
            if (missingNodes)
            {
                graph.Nodes = null;
            }
            else
            {
                graph.NodeLinks = null;
            }

            IReadOnlyList<GraphValidationIssue> issues = GraphValidatorRegistry.Validate(graph);

            Assert.That(issues.Select(issue => issue.IssueKind),
                Is.EquivalentTo(new[] { missingNodes ? "NULL_NODE_LIST" : "NULL_LINK_LIST" }));
        }

        [Test]
        public void DialogueValidator_ReportsMissingEntryWithoutUnreachableWarnings()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEndNodeData { Guid = "end" });
            graph.Nodes.Add(new DialogueWaitNodeData { Guid = "wait", DurationSeconds = -1 });
            graph.NodeLinks.Add(Link("wait", DialoguePortNames.Next, "end"));
            var issues = new List<GraphValidationIssue>();

            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

            Assert.That(issues.Any(issue => issue.IssueKind == "DIALOGUE_NO_ENTRY"), Is.True);
            Assert.That(issues.Any(issue => issue.IssueKind == "DIALOGUE_UNREACHABLE"), Is.False);
            Assert.That(issues.Any(issue => issue.IssueKind == "DIALOGUE_WAIT_DURATION"), Is.True);
        }

        [Test]
        public void GraphValidator_ContinuesAfterDomainWarning()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueLineNodeData { Guid = "line", DialogueText = string.Empty });
            graph.Nodes.Add(new DialogueWaitNodeData { Guid = "wait", DurationSeconds = -1 });
            graph.Nodes.Add(new DialogueEndNodeData { Guid = "end" });
            graph.NodeLinks.Add(Link("entry", DialoguePortNames.Next, "line"));
            graph.NodeLinks.Add(Link("line", DialoguePortNames.Next, "wait"));
            graph.NodeLinks.Add(Link("wait", DialoguePortNames.Next, "end"));

            IReadOnlyList<GraphValidationIssue> issues = GraphValidatorRegistry.Validate(graph);

            Assert.That(issues.Any(issue => issue.NodeGuid == "line" && issue.Severity == GraphValidationSeverity.Warning), Is.True);
            Assert.That(issues.Any(issue => issue.IssueKind == "DIALOGUE_WAIT_DURATION"), Is.True);
        }

        [Test]
        public void QuestValidator_ReportsMissingStartWithoutUnreachableWarnings()
        {
            QuestContainer graph = CreateAsset<QuestContainer>();
            graph.QuestId = 990010;
            graph.Nodes.Add(new QuestRewardNodeData { Guid = "reward" });
            graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "objective", RequiredAmount = -1 });
            var issues = new List<GraphValidationIssue>();

            ((IGraphValidator)new QuestGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

            Assert.That(issues.Any(issue => issue.IssueKind == "QUEST_START_COUNT"), Is.True);
            Assert.That(issues.Any(issue => issue.IssueKind == "QUEST_UNREACHABLE"), Is.False);
            Assert.That(issues.Any(issue => issue.IssueKind == "QUEST_OBJECTIVE_AMOUNT"), Is.True);
        }

        [Test]
        public void QuestValidator_AllowsMultipleProgressionOutputs()
        {
            QuestContainer graph = CreateAsset<QuestContainer>();
            graph.QuestId = 990013;
            graph.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            graph.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.CanComplete });
            foreach (string guid in new[] { "objective-a", "objective-b" })
            {
                graph.Nodes.Add(new QuestObjectiveNodeData { Guid = guid, ObjectiveType = "Collect", RequiredAmount = 1 });
                graph.NodeLinks.Add(Link("start", QuestPortNames.Next, guid));
                graph.NodeLinks.Add(Link(guid, QuestPortNames.Next, "end"));
            }
            var issues = new List<GraphValidationIssue>();

            ((IGraphValidator)new QuestGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

            Assert.That(issues.Any(issue => issue.Severity == GraphValidationSeverity.Error), Is.False);
        }

        [Test]
        public void DialogueValidator_IdentifiesChoiceWithInvalidBinding()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueChoiceNodeData
            {
                Guid = "choice",
                Choices = new List<DialogueChoiceData>
                {
                    new() { PortName = "one", ChoiceText = "First" },
                    new() { PortName = "two", ChoiceText = "Second", SelectionAction = null }
                }
            });
            var issues = new List<GraphValidationIssue>();

            ((IGraphValidator)new DialogueGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

            GraphValidationIssue issue = issues.Single(item => item.IssueKind == "DIALOGUE_BINDING_DATA");
            Assert.That(issue.Message, Does.StartWith("선택지 2 Action:"));
            Assert.That(issue.NodeGuid, Is.EqualTo("choice"));
        }

        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public void QuestValidator_ValidatesReferencedStructureBeforeCheckingWaitDeadlock(bool waitsForSelf, bool invalidReference)
        {
            QuestContainer first = CreateAsset<QuestContainer>();
            QuestContainer second = CreateAsset<QuestContainer>();
            first.QuestId = 990011;
            second.QuestId = 990012;
            foreach (QuestContainer graph in new[] { first, second })
            {
                graph.Nodes.Add(new QuestStartNodeData { Guid = "start" });
                graph.Nodes.Add(new QuestWaitForQuestNodeData
                {
                    Guid = "wait",
                    TargetQuestId = graph == first && !waitsForSelf ? second.QuestId : first.QuestId,
                    RequiredState = QuestState.InProgress
                });
                graph.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
                graph.NodeLinks.Add(Link("start", QuestPortNames.Next, "wait"));
                graph.NodeLinks.Add(Link("wait", QuestPortNames.Next, "end"));
            }
            if (invalidReference)
            {
                second.Nodes.Add(null);
            }

            // 실제 프로젝트 에셋을 만들지 않고, 검증기가 읽을 두 퀘스트만 제공한 뒤 원래 캐시를 복원합니다.
            System.Type indexType = typeof(QuestGraphValidator).Assembly.GetType("UniversalGraph.Quest.Editor.QuestAssetIndex", true);
            FieldInfo questsField = indexType.GetField("quests", BindingFlags.Static | BindingFlags.NonPublic);
            object originalQuests = questsField.GetValue(null);
            try
            {
                questsField.SetValue(null, new[] { first, second });
                IReadOnlyList<GraphValidationIssue> issues = GraphValidatorRegistry.Validate(first);
                if (invalidReference)
                {
                    Assert.That(issues.Single().IssueKind, Is.EqualTo("QUEST_REFERENCE_STRUCTURE"));
                    Assert.That(issues.Single().NodeGuid, Is.EqualTo("wait"));
                }
                else
                {
                    string issueKind = waitsForSelf ? "QUEST_SELF_DEPENDENCY" : "QUEST_WAIT_DEPENDENCY_CYCLE";
                    Assert.That(issues.Single(issue => issue.IssueKind == issueKind).Severity, Is.EqualTo(GraphValidationSeverity.Warning));
                    Assert.That(issues.Any(issue => issue.Severity == GraphValidationSeverity.Error), Is.False);
                }
            }
            finally
            {
                questsField.SetValue(null, originalQuests);
            }
        }

        [Test]
        public void QuestValidator_ReportsMetadataAndFlowIssuesForEmptyAsset()
        {
            QuestContainer graph = CreateAsset<QuestContainer>();
            graph.QuestId = -99999;

            var issues = new List<GraphValidationIssue>();
            ((IGraphValidator)new QuestGraphValidator()).Validate(new GraphValidationIndex(graph), issues);

            Assert.That(issues.Select(issue => issue.IssueKind), Is.EquivalentTo(new[]
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
                issues.Any(issue => issue.IssueKind == "QUEST_DIALOGUE_ENTRY"
                                    && issue.Severity == GraphValidationSeverity.Error),
                Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void QuestValidator_ReportsBrokenReferencedDialogueBeforeReadingConnections(bool missingLinks)
        {
            DialogueContainer dialogue = CreateAsset<DialogueContainer>();
            dialogue.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            if (missingLinks)
            {
                dialogue.NodeLinks = null;
            }
            else
            {
                dialogue.Nodes.Add(new DialogueEndNodeData { Guid = "entry" });
            }

            QuestContainer quest = CreateAsset<QuestContainer>();
            quest.QuestId = 990014;
            quest.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            quest.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            quest.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "interaction" });
            quest.Nodes.Add(new DialogueCandidateNodeData
            {
                Guid = "candidate",
                EntryPoint = new DialogueEntryPoint(dialogue, DialogueEntryNodeData.DefaultEntryId)
            });
            quest.NodeLinks.Add(Link("start", QuestPortNames.Next, "end"));
            quest.NodeLinks.Add(Link("interaction", QuestPortNames.Next, "candidate"));

            IReadOnlyList<GraphValidationIssue> issues = GraphValidatorRegistry.Validate(quest);

            Assert.That(issues.Single().IssueKind, Is.EqualTo("QUEST_DIALOGUE_GRAPH"));
            Assert.That(issues.Single().NodeGuid, Is.EqualTo("candidate"));
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
                                    && (issue.IssueKind == "QUEST_UNSUPPORTED_NODE"
                                        || issue.IssueKind == "QUEST_ROUTE_UNSAFE_NODE")),
                Is.False);
        }

        [Test]
        public void QuestValidator_AllowsBlockedOfferWithoutDisplayReason()
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
                issues.Any(issue => issue.IssueKind == "QUEST_OFFER_BLOCK_REASON"
                                    && issue.NodeGuid == "offer"
                                    && issue.Severity == GraphValidationSeverity.Warning),
                Is.False);
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
                issues.Any(issue => issue.IssueKind == "QUEST_OFFER_IN_PROGRESS_FLOW"
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
            if (asset is GraphContainer graph)
            {
                GraphAssetMigrator.EnsureCurrent(graph);
            }
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
