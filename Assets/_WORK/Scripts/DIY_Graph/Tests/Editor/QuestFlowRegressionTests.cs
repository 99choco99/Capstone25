using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniversalGraph.Tests
{
    public sealed class QuestFlowRegressionTests
    {
        private readonly List<QuestContainer> graphs = new();

        [TearDown]
        public void TearDown()
        {
            QuestDefinitionRegistry.Initialize(Array.Empty<QuestContainer>());
            foreach (QuestContainer graph in graphs)
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
            graphs.Clear();
        }

        [TestCase(8, 2)]
        [TestCase(2, 20)]
        public void ConvergingRewardBranches_DoNotMultiplyImmediateSteps(int layers, int width)
        {
            QuestContainer graph = CreateQuest();
            graph.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            string[] previous = { "start" };
            for (int layer = 0; layer < layers; layer++)
            {
                var current = new string[width];
                for (int branch = 0; branch < width; branch++)
                {
                    string guid = $"reward-{layer}-{branch}";
                    current[branch] = guid;
                    graph.Nodes.Add(new QuestRewardNodeData { Guid = guid });
                    foreach (string source in previous)
                    {
                        Connect(graph, source, guid);
                    }
                }
                previous = current;
            }
            graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test", RequiredAmount = 1 });
            graph.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            foreach (string source in previous)
            {
                Connect(graph, source, "goal");
            }
            Connect(graph, "goal", "end");
            QuestDefinitionRegistry.Initialize(new[] { graph });
            var controller = new TestController();

            Assert.That(graph.Nodes, Has.Count.EqualTo(layers * width + 3));
            Assert.That(QuestRunner.StartQuest(controller, graph.QuestId), Is.True);
            QuestProgress progress = controller.QuestProgress[graph.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.activeNodeGuids, Is.EqualTo(new[] { "goal" }));
            Assert.That(progress.completedNodeGuids, Has.Count.EqualTo(layers * width));
        }

        [Test]
        public void AndGate_RecordsEveryDistinctArrivalBeforeContinuing()
        {
            QuestContainer graph = CreateQuest();
            graph.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "first", EventKey = "Test", RequiredAmount = 1 });
            graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "second", EventKey = "Test", RequiredAmount = 1 });
            graph.Nodes.Add(new QuestAndGateNodeData { Guid = "gate" });
            graph.Nodes.Add(new QuestStateChangeNodeData { Guid = "end", NewState = QuestState.TurnedIn });
            Connect(graph, "start", "first");
            Connect(graph, "start", "second");
            Connect(graph, "first", "gate");
            Connect(graph, "second", "gate");
            Connect(graph, "gate", "end");
            QuestDefinitionRegistry.Initialize(new[] { graph });
            var controller = new TestController();
            Assert.That(QuestRunner.StartQuest(controller, graph.QuestId), Is.True);

            Assert.That(QuestRunner.AdvanceObjective(controller, graph.QuestId, "first"), Is.True);
            QuestProgress progress = controller.QuestProgress[graph.QuestId];
            Assert.That(progress.state, Is.EqualTo(QuestState.InProgress));
            Assert.That(progress.completedGateInputs, Is.EqualTo(new[] { "gate|first" }));
            Assert.That(progress.completedNodeGuids, Does.Not.Contain("gate"));

            Assert.That(QuestRunner.AdvanceObjective(controller, graph.QuestId, "second"), Is.True);
            Assert.That(progress.completedGateInputs, Is.EquivalentTo(new[] { "gate|first", "gate|second" }));
            Assert.That(progress.completedNodeGuids, Does.Contain("gate"));
            Assert.That(progress.state, Is.EqualTo(QuestState.TurnedIn));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedCondition_ReevaluatesUnlessItsOneShotPredecessorAlreadyCompleted(bool sharedReward)
        {
            QuestContainer graph = CreateQuest();
            QuestContainer inspected = CreateQuest();
            graph.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "first", EventKey = "Test", RequiredAmount = 1 });
            graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "second", EventKey = "Test", RequiredAmount = 1 });
            graph.Nodes.Add(new QuestStateConditionNodeData
            {
                Guid = "condition",
                QuestId = inspected.QuestId,
                TargetState = QuestState.NotStarted
            });
            graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "true-goal", EventKey = "Test", RequiredAmount = 1 });
            graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "false-goal", EventKey = "Test", RequiredAmount = 1 });
            Connect(graph, "start", "first");
            Connect(graph, "start", "second");
            string target = "condition";
            if (sharedReward)
            {
                graph.Nodes.Add(new QuestRewardNodeData { Guid = "reward" });
                Connect(graph, "reward", "condition");
                target = "reward";
            }
            Connect(graph, "first", target);
            Connect(graph, "second", target);
            Connect(graph, "condition", "true-goal", QuestPortNames.True);
            Connect(graph, "condition", "false-goal", QuestPortNames.False);
            QuestDefinitionRegistry.Initialize(new[] { graph, inspected });
            var controller = new TestController();
            Assert.That(QuestRunner.StartQuest(controller, graph.QuestId), Is.True);
            Assert.That(QuestRunner.AdvanceObjective(controller, graph.QuestId, "first"), Is.True);
            controller.QuestProgress[inspected.QuestId] = new QuestProgress(inspected) { state = QuestState.TurnedIn };

            Assert.That(QuestRunner.AdvanceObjective(controller, graph.QuestId, "second"), Is.True);
            QuestProgress progress = controller.QuestProgress[graph.QuestId];
            Assert.That(progress.completedNodeGuids, Does.Not.Contain("condition"));
            Assert.That(progress.activeNodeGuids, Does.Contain("true-goal"));
            Assert.That(progress.activeNodeGuids.Contains("false-goal"), Is.EqualTo(!sharedReward));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ImmediateConditionCycle_StopsEvenIfAnotherObjectiveIsActive(bool parallelObjective)
        {
            QuestContainer graph = CreateQuest();
            graph.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            graph.Nodes.Add(new QuestStateConditionNodeData
            {
                Guid = "cycle",
                QuestId = graph.QuestId,
                TargetState = QuestState.InProgress
            });
            Connect(graph, "start", "cycle");
            Connect(graph, "cycle", "cycle", QuestPortNames.True);
            if (parallelObjective)
            {
                graph.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test", RequiredAmount = 1 });
                Connect(graph, "start", "goal");
            }
            QuestDefinitionRegistry.Initialize(new[] { graph });
            var controller = new TestController();
            LogAssert.Expect(LogType.Error, new Regex("즉시 실행 단계가 256회를 초과"));

            Assert.That(QuestRunner.StartQuest(controller, graph.QuestId), Is.False);
            Assert.That(controller.QuestProgress[graph.QuestId].state, Is.EqualTo(QuestState.ExecutionError));
            Assert.That(controller.QuestProgress[graph.QuestId].activeNodeGuids, Is.Empty);
        }

        [TestCase(QuestState.Failed, false)]
        [TestCase(QuestState.ExecutionError, true)]
        public void ExecutionError_DoesNotWakeGameplayFailureWaiters(QuestState requiredState, bool shouldResume)
        {
            QuestContainer graph = CreateQuest();
            graph.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            graph.Nodes.Add(new QuestActionNodeData { Guid = "invalid-action" });
            Connect(graph, "start", "invalid-action");
            QuestContainer waiting = CreateQuest();
            waiting.Nodes.Add(new QuestStartNodeData { Guid = "start" });
            waiting.Nodes.Add(new QuestWaitForQuestNodeData
            {
                Guid = "wait",
                TargetQuestId = graph.QuestId,
                RequiredState = requiredState
            });
            waiting.Nodes.Add(new QuestObjectiveNodeData { Guid = "goal", EventKey = "Test", RequiredAmount = 1 });
            Connect(waiting, "start", "wait");
            Connect(waiting, "wait", "goal");
            QuestDefinitionRegistry.Initialize(new[] { graph, waiting });
            var controller = new TestController();
            Assert.That(QuestRunner.StartQuest(controller, waiting.QuestId), Is.True);
            LogAssert.Expect(LogType.Error, "[Quest] Action 키가 비어 있습니다.");

            Assert.That(QuestRunner.StartQuest(controller, graph.QuestId), Is.False);
            Assert.That(controller.QuestProgress[graph.QuestId].state, Is.EqualTo(QuestState.ExecutionError));
            Assert.That(controller.QuestProgress[waiting.QuestId].activeNodeGuids,
                Is.EqualTo(new[] { shouldResume ? "goal" : "wait" }));
        }

        [Test]
        public void InteractionQuery_FollowsValidPathsLongerThan128Nodes()
        {
            QuestContainer graph = CreateQuest();
            graph.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc" });
            string previous = "entry";
            for (int i = 0; i < 150; i++)
            {
                string guid = $"condition-{i}";
                graph.Nodes.Add(new QuestStateConditionNodeData
                {
                    Guid = guid,
                    QuestId = graph.QuestId,
                    TargetState = QuestState.NotStarted
                });
                Connect(graph, previous, guid, i == 0 ? QuestPortNames.Next : QuestPortNames.True);
                previous = guid;
            }
            graph.Nodes.Add(new QuestOfferNodeData { Guid = "offer" });
            Connect(graph, previous, "offer", QuestPortNames.True);
            QuestDefinitionRegistry.Initialize(new[] { graph });

            QuestOffer[] offers = QuestQueries.GetQuestOffers(new TestController(), "npc");

            Assert.That(offers, Has.Length.EqualTo(1));
            Assert.That(offers[0].QuestId, Is.EqualTo(graph.QuestId));
        }

        [Test]
        public void InteractionQuery_StopsAtVisitedNodesWithoutAStepLimit()
        {
            QuestContainer graph = CreateQuest();
            graph.Nodes.Add(new QuestInteractionEntryNodeData { Guid = "entry", TargetId = "npc" });
            graph.Nodes.Add(new QuestStateConditionNodeData
            {
                Guid = "cycle",
                QuestId = graph.QuestId,
                TargetState = QuestState.NotStarted
            });
            Connect(graph, "entry", "cycle");
            Connect(graph, "cycle", "cycle", QuestPortNames.True);
            QuestDefinitionRegistry.Initialize(new[] { graph });

            Assert.That(QuestQueries.GetQuestOffers(new TestController(), "npc"), Is.Empty);
        }

        private QuestContainer CreateQuest()
        {
            QuestContainer graph = ScriptableObject.CreateInstance<QuestContainer>();
            graph.QuestId = graphs.Count + 1;
            graph.name = $"Flow Regression {graph.QuestId}";
            graphs.Add(graph);
            return graph;
        }

        private static void Connect(QuestContainer graph, string source, string target, string port = QuestPortNames.Next)
        {
            graph.NodeLinks.Add(new NodeLinkData
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

            public void InvokeStatusChanged(QuestContainer container, QuestProgress progress)
            {
            }
        }
    }
}
