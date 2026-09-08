using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.TestTools;
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
        public void MethodBuildValidator_UsesReflectionAndValidatesEachAttributeOnce()
        {
            MethodInfo action = typeof(UniversalGraphRuntimeTests).GetMethod("RecordInvokerAction", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo condition = typeof(UniversalGraphRuntimeTests).GetMethod("IsDialogueChoiceVisible", BindingFlags.Static | BindingFlags.NonPublic);
            System.Type validator = typeof(GraphValidator).Assembly.GetType("UniversalGraph.Editor.GraphMethodValidator", true);
            MethodInfo validate = validator.GetMethod("Validate", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(IEnumerable<MethodInfo>) }, null);

            // TypeCache 결과에 같은 메서드가 여러 번 포함되어도 각 도메인의 Attribute는 한 번만 검사합니다.
            var errors = (List<string>)validate.Invoke(null, new object[] { new[] { action, action, condition, condition } });

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void GraphBuildValidator_IsDiscoverableByUnity()
        {
            System.Type preprocessor = typeof(GraphValidator).Assembly.GetType(
                "UniversalGraph.Editor.GraphAllValidator", true);

            Assert.That(TypeCache.GetTypesDerivedFrom<IPreprocessBuildWithReport>(), Does.Contain(preprocessor));
            Assert.That(TypeCache.GetTypesDerivedFrom<AssetPostprocessor>(), Does.Contain(preprocessor));
            var callback = (IPreprocessBuildWithReport)System.Activator.CreateInstance(preprocessor);
            Assert.That(callback.callbackOrder, Is.LessThan(0));
        }

        [Test]
        public void GraphBuildValidator_ValidatesMainAndHiddenGraphAssetsOnceInPathOrder()
        {
            int baselineErrors = ValidateProject(out _);
            string folder = CreateValidationFolder();
            DialogueContainer dialogue = CreateAsset<DialogueContainer>();
            QuestContainer quest = CreateAsset<QuestContainer>();
            DialogueContainer hiddenDialogue = CreateAsset<DialogueContainer>();
            try
            {
                var questIds = new HashSet<int>(AssetDatabase.FindAssets("t:QuestContainer")
                    .Select(AssetDatabase.GUIDToAssetPath).SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                    .OfType<QuestContainer>().Select(existing => existing.QuestId));
                quest.QuestId = 1;
                while (questIds.Contains(quest.QuestId)) quest.QuestId++;
                AssetDatabase.CreateAsset(quest, folder + "/Quest.asset");
                AssetDatabase.CreateAsset(dialogue, folder + "/Dialogue.asset");
                hiddenDialogue.name = "Hidden Dialogue";
                hiddenDialogue.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(hiddenDialogue, dialogue);
                AssetDatabase.SaveAssetIfDirty(dialogue);
                GraphContainer[] graphs = { dialogue, hiddenDialogue, quest };
                string[] expectedMessages = graphs.SelectMany(graph => GraphValidator.Validate(graph)
                    .Select(issue => $"[Universal Graph] '{AssetDatabase.GetAssetPath(graph)}' ({graph.name}) {issue}"))
                    .ToArray();
                int fixtureErrors = graphs.Sum(graph => GraphValidator.Validate(graph)
                    .Count(issue => issue.Severity == GraphValidationSeverity.Error));

                int errors = ValidateProject(out var logs);
                string[] fixtureMessages = logs.Where(log => log.Message.Contains(folder)).Select(log => log.Message).ToArray();

                Assert.That(errors, Is.EqualTo(baselineErrors + fixtureErrors));
                Assert.That(fixtureMessages, Is.EquivalentTo(expectedMessages));
                int dialogueIndex = System.Array.FindIndex(fixtureMessages, message => message.Contains("/Dialogue.asset'"));
                int questIndex = System.Array.FindIndex(fixtureMessages, message => message.Contains("/Quest.asset'"));
                Assert.That(dialogueIndex, Is.LessThan(questIndex));
            }
            finally
            {
                // 이 테스트에서 만든 에셋만 삭제합니다.
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void GraphBuildValidator_WarningsDoNotBlockAnOtherwiseValidBuild()
        {
            int baselineErrors = ValidateProject(out _);
            string folder = CreateValidationFolder();
            DialogueContainer graph = CreateValidDialogue(emptyText: true);
            try
            {
                AssetDatabase.CreateAsset(graph, folder + "/Warning.asset");
                Assert.That(GraphValidator.Validate(graph).Select(issue => issue.Severity),
                    Is.EqualTo(new[] { GraphValidationSeverity.Warning }));

                int errors = ValidateProject(out var logs);
                Assert.That(errors, Is.EqualTo(baselineErrors));
                Assert.That(logs.Count(log => log.Type == LogType.Warning && log.Message.Contains(folder)), Is.EqualTo(1));

                var callback = (IPreprocessBuildWithReport)System.Activator.CreateInstance(BuildValidatorType);
                logs = CaptureValidationLogs(() =>
                {
                    if (baselineErrors == 0)
                    {
                        Assert.DoesNotThrow(() => callback.OnPreprocessBuild(null));
                    }
                    else
                    {
                        // 기존 프로젝트 오류는 그대로 차단하되, 테스트의 Warning은 오류 수에 더하지 않습니다.
                        BuildFailedException exception = Assert.Throws<BuildFailedException>(() => callback.OnPreprocessBuild(null));
                        Assert.That(exception.Message, Does.Contain($"오류 {baselineErrors}개"));
                    }
                });
                Assert.That(logs.Count(log => log.Type == LogType.Warning && log.Message.Contains(folder)), Is.EqualTo(1));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void GraphBuildValidator_UnreferencedIncompleteGraphBlocksBuild()
        {
            int baselineErrors = ValidateProject(out _);
            string folder = CreateValidationFolder();
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            try
            {
                // 어떤 Scene이나 다른 그래프에서도 참조하지 않은 작성 중 에셋입니다.
                AssetDatabase.CreateAsset(graph, folder + "/UnusedDraft.asset");
                int errors = ValidateProject(out var logs);
                Assert.That(errors, Is.EqualTo(baselineErrors + 1));
                Assert.That(logs.Count(log => log.Type == LogType.Error && log.Message.Contains(folder)
                    && log.Message.Contains("DIALOGUE_NO_ENTRY")), Is.EqualTo(1));

                var callback = (IPreprocessBuildWithReport)System.Activator.CreateInstance(BuildValidatorType);
                logs = CaptureValidationLogs(() =>
                {
                    BuildFailedException exception = Assert.Throws<BuildFailedException>(() => callback.OnPreprocessBuild(null));
                    Assert.That(exception.Message, Does.Contain($"오류 {errors}개"));
                });
                Assert.That(logs.Count(log => log.Type == LogType.Error && log.Message.Contains(folder)), Is.EqualTo(1));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [TestCase(0)]
        [TestCase(GraphAssetMigrator.CurrentVersion + 1)]
        public void GraphBuildValidator_DoesNotMigrateOrModifyAssets(int schemaVersion)
        {
            string folder = CreateValidationFolder();
            DialogueContainer graph = CreateValidDialogue();
            try
            {
                var serializedGraph = new SerializedObject(graph);
                serializedGraph.FindProperty("schemaVersion").intValue = schemaVersion;
                serializedGraph.ApplyModifiedPropertiesWithoutUndo();
                string path = folder + "/Schema.asset";
                AssetDatabase.CreateAsset(graph, path);
                AssetDatabase.SaveAssetIfDirty(graph);
                string filePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), path);
                string dataBefore = EditorJsonUtility.ToJson(graph);
                string fileBefore = System.IO.File.ReadAllText(filePath);
                bool dirtyBefore = EditorUtility.IsDirty(graph);

                ValidateProject(out var logs);

                Assert.That(logs.Count(log => log.Type == LogType.Error && log.Message.Contains(folder)
                    && log.Message.Contains("GRAPH_SCHEMA_VERSION")), Is.EqualTo(1));
                Assert.That(graph.SchemaVersion, Is.EqualTo(schemaVersion));
                Assert.That(EditorJsonUtility.ToJson(graph), Is.EqualTo(dataBefore));
                Assert.That(System.IO.File.ReadAllText(filePath), Is.EqualTo(fileBefore));
                Assert.That(EditorUtility.IsDirty(graph), Is.EqualTo(dirtyBefore));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void GraphBuildValidator_ReloadScansAssetsButOrdinaryImportDoesNot()
        {
            string folder = CreateValidationFolder();
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            try
            {
                string path = folder + "/Reload.asset";
                AssetDatabase.CreateAsset(graph, path);
                MethodInfo callback = BuildValidatorType.GetMethod("OnPostprocessAllAssets", BindingFlags.Static | BindingFlags.NonPublic);
                object[] arguments = { new[] { path }, System.Array.Empty<string>(), System.Array.Empty<string>(), System.Array.Empty<string>(), false };

                var logs = CaptureValidationLogs(() => callback.Invoke(null, arguments));
                Assert.That(logs, Is.Empty);

                arguments[4] = true;
                logs = CaptureValidationLogs(() => callback.Invoke(null, arguments));
                Assert.That(logs.Count(log => log.Type == LogType.Error && log.Message.Contains(folder)
                    && log.Message.Contains("DIALOGUE_NO_ENTRY")), Is.EqualTo(1));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void GraphValidatorRegistry_ReportsNullGraphWithoutThrowing()
        {
            IReadOnlyList<GraphValidationIssue> issues = GraphValidator.Validate(null);

            Assert.That(issues.Any(issue => issue.IssueKind == "GRAPH_NULL"
                && issue.Severity == GraphValidationSeverity.Error), Is.True);
        }

        [Test]
        public void GraphValidatorRegistry_ReportsInitializationAndStructureErrorsTogether()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(null);
            var initializationIssues = (List<GraphValidationIssue>)typeof(GraphValidator)
                .GetField("initializationIssues", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            GraphValidationIssue[] originalIssues = initializationIssues.ToArray();
            var initializationError = new GraphValidationIssue(
                GraphValidationSeverity.Error, "TEST_INITIALIZATION_FAILURE", "Test validator could not initialize.");
            try
            {
                initializationIssues.Add(initializationError);
                IReadOnlyList<GraphValidationIssue> issues = GraphValidator.Validate(graph);

                Assert.That(issues, Does.Contain(initializationError));
                Assert.That(issues.Any(issue => issue.IssueKind == "NULL_NODE"), Is.True);
            }
            finally
            {
                initializationIssues.Clear();
                initializationIssues.AddRange(originalIssues);
            }
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
            Assert.That(DialogueMethodDescriptorFactory.TryCreateDescriptor(
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

            IReadOnlyList<GraphValidationIssue> issues = GraphValidator.Validate(graph);

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

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void GraphValidator_RejectsLinkWithoutTargetPortRegardlessOfVersion(int version)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueLineNodeData { Guid = "line" });
            typeof(GraphContainer).GetField("schemaVersion", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(graph, version);

            NodeLinkData link = Link("entry", DialoguePortNames.Next, "line");
            link.TargetPortName = string.Empty;
            graph.NodeLinks.Add(link);

            IReadOnlyList<GraphValidationIssue> issues = GraphValidator.Validate(graph);

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

            HashSet<string> cycleNodes = new GraphValidationIndex(graph).FindCycleNodes(_ => true);

            Assert.That(cycleNodes, Is.EquivalentTo(new[] { "a", "b" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CycleFinder_ReturnsOverlappingCyclesRegardlessOfLinkOrder(bool reverseLinks)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            foreach (string guid in new[] { "prefix", "a", "b", "c", "tail" })
            {
                graph.Nodes.Add(new DialogueActionNodeData { Guid = guid });
            }
            graph.NodeLinks.Add(Link("prefix", "Next", "a"));
            graph.NodeLinks.Add(Link("a", "Next", "b"));
            graph.NodeLinks.Add(Link("b", "Next", "a"));
            graph.NodeLinks.Add(Link("a", "Next", "c"));
            graph.NodeLinks.Add(Link("c", "Next", "b"));
            graph.NodeLinks.Add(Link("c", "Next", "tail"));
            if (reverseLinks)
            {
                graph.NodeLinks.Reverse();
            }

            HashSet<string> cycleNodes = new GraphValidationIndex(graph).FindCycleNodes(_ => true);

            Assert.That(cycleNodes, Is.EquivalentTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void CycleFinder_DetectsSelfLoopButNotIsolatedNode()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueActionNodeData { Guid = "self" });
            graph.Nodes.Add(new DialogueActionNodeData { Guid = "isolated" });
            graph.NodeLinks.Add(Link("self", "Next", "self"));

            HashSet<string> cycleNodes = new GraphValidationIndex(graph).FindCycleNodes(_ => true);

            Assert.That(cycleNodes, Is.EquivalentTo(new[] { "self" }));
        }

        [Test]
        public void CycleFinder_DoesNotTraverseExcludedNodes()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueActionNodeData { Guid = "a" });
            graph.Nodes.Add(new DialogueWaitNodeData { Guid = "wait", DurationSeconds = 1 });
            graph.Nodes.Add(new DialogueActionNodeData { Guid = "b" });
            graph.NodeLinks.Add(Link("a", "Next", "wait"));
            graph.NodeLinks.Add(Link("wait", "Next", "b"));
            graph.NodeLinks.Add(Link("b", "Next", "a"));
            var index = new GraphValidationIndex(graph);

            Assert.That(index.FindCycleNodes(_ => true), Is.EquivalentTo(new[] { "a", "wait", "b" }));
            Assert.That(index.FindCycleNodes(node => node is DialogueActionNodeData), Is.Empty);
            Assert.That(index.FindCycleNodes(_ => false), Is.Empty);
        }

        [Test]
        public void DialogueValidator_ReportsEveryNodeInOverlappingImmediateCycles()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueConditionNodeData { Guid = "a" });
            graph.Nodes.Add(new DialogueActionNodeData { Guid = "b" });
            graph.Nodes.Add(new DialogueActionNodeData { Guid = "c" });
            graph.NodeLinks.Add(Link("entry", DialoguePortNames.Next, "a"));
            graph.NodeLinks.Add(Link("a", DialoguePortNames.True, "b"));
            graph.NodeLinks.Add(Link("a", DialoguePortNames.False, "c"));
            graph.NodeLinks.Add(Link("b", DialoguePortNames.Next, "a"));
            graph.NodeLinks.Add(Link("c", DialoguePortNames.Next, "b"));

            IReadOnlyList<GraphValidationIssue> issues = GraphValidator.Validate(graph);

            Assert.That(issues.Where(issue => issue.IssueKind == "DIALOGUE_IMMEDIATE_CYCLE").Select(issue => issue.NodeGuid),
                Is.EquivalentTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void GraphValidationIndex_IndexesStructurallyValidNodesAndLinks()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            graph.Nodes.Add(new DialogueEndNodeData { Guid = "end" });
            graph.NodeLinks.Add(Link("entry", DialoguePortNames.Next, "end"));

            Assert.That(GraphValidator.ValidateStructure(graph), Is.Empty);

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

            IReadOnlyList<GraphValidationIssue> issues = GraphValidator.Validate(graph);

            Assert.That(issues.Select(issue => issue.IssueKind), Is.EquivalentTo(new[] { issueKind }));
        }

        [Test]
        public void GraphViewSerializer_AllowsEditingGraphWithDomainError()
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry" });
            var graphView = new UniversalGraphView();

            Assert.That(GraphValidator.Validate(graph).Any(issue => issue.IssueKind == "DIALOGUE_OUTPUT_COUNT"), Is.True);
            Assert.DoesNotThrow(() => GraphViewSerializer.LoadGraph(graphView, graph));
            Assert.That(graphView.nodes.ToList(), Has.Count.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GraphValidator_ReportsMissingCollectionWithoutDomainErrors(bool missingNodes)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            GraphAssetMigrator.Migrate(graph);
            if (missingNodes)
            {
                graph.Nodes = null;
            }
            else
            {
                graph.NodeLinks = null;
            }

            IReadOnlyList<GraphValidationIssue> issues = GraphValidator.Validate(graph);

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

            IReadOnlyList<GraphValidationIssue> issues = GraphValidator.Validate(graph);

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
                graph.Nodes.Add(new QuestObjectiveNodeData { Guid = guid, EventKey = "Collect", RequiredAmount = 1 });
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
                IReadOnlyList<GraphValidationIssue> issues = GraphValidator.Validate(first);
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

            IReadOnlyList<GraphValidationIssue> issues = GraphValidator.Validate(quest);

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

        private static System.Type BuildValidatorType => typeof(GraphValidator).Assembly.GetType(
            "UniversalGraph.Editor.GraphAllValidator", true);

        private static string CreateValidationFolder()
        {
            string guid = AssetDatabase.CreateFolder("Assets", "GraphBuildValidatorTests-" + System.Guid.NewGuid().ToString("N"));
            return AssetDatabase.GUIDToAssetPath(guid);
        }

        private static int ValidateProject(out List<(LogType Type, string Message)> logs)
        {
            int errors = -1;
            logs = CaptureValidationLogs(() => errors = (int)BuildValidatorType
                .GetMethod("ValidateAll", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null));
            return errors;
        }

        private static List<(LogType Type, string Message)> CaptureValidationLogs(System.Action action)
        {
            var logs = new List<(LogType Type, string Message)>();
            bool originalIgnore = LogAssert.ignoreFailingMessages;
            try
            {
                // 전체 검사에서 기존 프로젝트 오류도 출력되므로, 테스트 에셋 경로의 로그를 따로 단언합니다.
                LogAssert.ignoreFailingMessages = true;
                Application.logMessageReceived += Capture;
                action();
            }
            finally
            {
                Application.logMessageReceived -= Capture;
                LogAssert.ignoreFailingMessages = originalIgnore;
            }
            return logs;

            void Capture(string message, string stackTrace, LogType type)
            {
                logs.Add((type, message));
            }
        }

        private DialogueContainer CreateValidDialogue(bool emptyText = false)
        {
            DialogueContainer graph = CreateAsset<DialogueContainer>();
            graph.Nodes.Add(new DialogueEntryNodeData { Guid = "entry", EntryId = "Default" });
            graph.Nodes.Add(new DialogueLineNodeData { Guid = "line", DialogueText = emptyText ? string.Empty : "Test dialogue" });
            graph.Nodes.Add(new DialogueEndNodeData { Guid = "end" });
            graph.NodeLinks.Add(Link("entry", DialoguePortNames.Next, "line"));
            graph.NodeLinks.Add(Link("line", DialoguePortNames.Next, "end"));
            return graph;
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            if (asset is GraphContainer graph)
            {
                GraphAssetMigrator.Migrate(graph);
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
