using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Quest 노드 대기열과 즉시 실행 흐름을 담당합니다.</summary>
    public static partial class QuestRunner
    {
        private const int MaxImmediateNodeSteps = 256;

        private readonly struct FlowStep
        {
            public FlowStep(NodeBaseData node, string sourceNodeGuid)
            {
                Node = node;
                SourceNodeGuid = sourceNodeGuid;
            }

            public NodeBaseData Node { get; }
            public string SourceNodeGuid { get; }
        }

        /// <summary>출발 출력 하나에서 도달 가능한 즉시 노드를 재귀 호출 없이 처리합니다.</summary>
        private static void RunFromOutputs(
            IQuestController controller,
            QuestContainer container,
            QuestProgress progress,
            QuestGraphIndex flowIndex,
            string sourceGuid,
            string sourcePort)
        {
            var queue = new Queue<FlowStep>();
            EnqueueOutputs(flowIndex, queue, sourceGuid, sourcePort);

            int stepCount = 0;
            while (queue.Count > 0)
            {
                if (++stepCount > MaxImmediateNodeSteps)
                {
                    Debug.LogError(
                        $"[Quest] '{container.name}'에서 즉시 실행 단계가 {MaxImmediateNodeSteps}회를 초과했습니다. " +
                        "그래프에 Condition/Action 순환이 있는지 확인하세요.",
                        container);
                    return;
                }

                FlowStep step = queue.Dequeue();
                NodeBaseData node = step.Node;
                progress.currentNodeGuid = node.Guid;

                if (node is QuestStartNodeData || node is QuestEventEntryNodeData)
                {
                    EnqueueOutputs(flowIndex, queue, node.Guid, null);
                }
                else if (node is QuestObjectiveNodeData objective)
                {
                    if (IsCompleted(progress, node.Guid))
                    {
                        EnqueueOutputs(flowIndex, queue, node.Guid, null);
                    }
                    else
                    {
                        ActivateObjective(progress, objective);
                    }
                }
                else if (node is QuestConditionBranchNodeData condition)
                {
                    if (!TryEvaluateCustomCondition(
                            controller,
                            container,
                            progress,
                            condition,
                            out bool result,
                            out bool handlerFound))
                    {
                        Debug.LogError(
                            handlerFound
                                ? $"[Quest] '{container.name}'의 Condition '{condition.Condition.Key}' 실행에 실패했습니다."
                                : $"[Quest] '{container.name}'의 Condition '{condition.Condition.Key}'을 처리한 Handler가 없습니다.",
                            container);
                        continue;
                    }

                    EnqueueOutputs(flowIndex, queue, node.Guid, result ? "True" : "False");
                }
                else if (node is QuestStateConditionNodeData stateCondition)
                {
                    QuestProgress inspected = controller.GetQuestStatus(stateCondition.QuestId);
                    bool result = inspected != null && inspected.state == stateCondition.TargetState;
                    EnqueueOutputs(flowIndex, queue, node.Guid, result ? "True" : "False");
                }
                else if (node is QuestAndGateNodeData gate)
                {
                    ProcessAndGate(progress, flowIndex, queue, gate, step.SourceNodeGuid);
                }
                else if (node is QuestStateChangeNodeData stateChange)
                {
                    if (MarkCompleted(progress, node.Guid))
                    {
                        progress.state = stateChange.NewState;
                    }

                    EnqueueOutputs(flowIndex, queue, node.Guid, null);
                }
                else if (node is QuestActionTriggerNodeData action)
                {
                    if (!IsCompleted(progress, node.Guid))
                    {
                        if (!ExecuteAction(controller, container, progress, action))
                        {
                            continue;
                        }

                        MarkCompleted(progress, node.Guid);
                    }

                    EnqueueOutputs(flowIndex, queue, node.Guid, null);
                }
                else if (node is QuestFailNodeData)
                {
                    MarkCompleted(progress, node.Guid);
                    progress.state = QuestState.Failed;
                    progress.activeNodeGuids.Clear();
                    return;
                }
                else if (node is QuestRewardNodeData reward)
                {
                    if (!IsCompleted(progress, node.Guid))
                    {
                        if (!ExecuteRewardAction(controller, container, progress, reward))
                        {
                            continue;
                        }

                        MarkCompleted(progress, node.Guid);
                        progress.state = QuestState.CanComplete;
                        controller.TurnInQuest(progress.questId);
                    }

                    if (progress.state == QuestState.InProgress || progress.state == QuestState.CanComplete)
                    {
                        EnqueueOutputs(flowIndex, queue, node.Guid, null);
                    }
                }
                else if (node is QuestSubGraphNodeData subGraph)
                {
                    if (IsCompleted(progress, node.Guid))
                    {
                        EnqueueOutputs(flowIndex, queue, node.Guid, null);
                        continue;
                    }

                    QuestProgress subProgress = controller.GetQuestStatus(subGraph.SubQuestId);
                    if (subProgress == null)
                    {
                        Debug.LogError(
                            $"[Quest] 하위 Quest ID {subGraph.SubQuestId}의 진행 기록이 없습니다.",
                            container);
                        continue;
                    }

                    if (subProgress.state == QuestState.TurnedIn)
                    {
                        MarkCompleted(progress, node.Guid);
                        EnqueueOutputs(flowIndex, queue, node.Guid, null);
                        continue;
                    }

                    if (!progress.activeNodeGuids.Contains(node.Guid))
                    {
                        progress.activeNodeGuids.Add(node.Guid);
                    }

                    if (subProgress.state != QuestState.InProgress
                        && subProgress.state != QuestState.CanComplete)
                    {
                        StartQuestGraph(controller, subGraph.SubQuestId);
                    }
                }
                else if (node is DialogueRequestNodeData)
                {
                    Debug.LogWarning(
                        $"[Quest] Dialogue Request 노드 '{node.Guid}'는 대화 경로의 종점이므로 " +
                        "Quest 진행을 앞으로 이동시키지 않습니다.",
                        container);
                }
                else
                {
                    Debug.LogError(
                        $"[Quest] '{container.name}'에서 지원하지 않는 노드 타입 '{node.GetType().FullName}'을 발견했습니다.",
                        container);
                }
            }
        }

        private static void ProcessAndGate(
            QuestProgress progress,
            QuestGraphIndex flowIndex,
            Queue<FlowStep> queue,
            QuestAndGateNodeData gate,
            string sourceNodeGuid)
        {
            if (IsCompleted(progress, gate.Guid))
            {
                EnqueueOutputs(flowIndex, queue, gate.Guid, null);
                return;
            }

            string arrivalKey = $"{gate.Guid}|{sourceNodeGuid}";
            if (!progress.completedGateInputs.Contains(arrivalKey))
            {
                progress.completedGateInputs.Add(arrivalKey);
            }

            string prefix = gate.Guid + "|";
            int arrivals = progress.completedGateInputs.Count(key => key.StartsWith(prefix, StringComparison.Ordinal));
            flowIndex.DistinctIncomingSourceCounts.TryGetValue(gate.Guid, out int incomingSourceCount);
            int requiredArrivals = Math.Max(1, incomingSourceCount);
            if (arrivals < requiredArrivals)
            {
                return;
            }

            MarkCompleted(progress, gate.Guid);
            EnqueueOutputs(flowIndex, queue, gate.Guid, null);
        }

        /// <summary>조건에 맞는 모든 도착 노드를 즉시 실행 대기열에 추가합니다.</summary>
        private static void EnqueueOutputs(
            QuestGraphIndex flowIndex,
            Queue<FlowStep> queue,
            string sourceGuid,
            string sourcePort)
        {
            List<NodeLinkData> links;
            if (string.IsNullOrWhiteSpace(sourcePort))
            {
                if (!flowIndex.OutgoingLinks.TryGetValue(sourceGuid, out links))
                {
                    return;
                }
            }
            else if (!flowIndex.OutgoingByPort.TryGetValue((sourceGuid, sourcePort), out links))
            {
                return;
            }

            foreach (NodeLinkData link in links)
            {
                queue.Enqueue(new FlowStep(flowIndex.Nodes[link.TargetNodeGuid], sourceGuid));
            }
        }

        private static void ActivateObjective(QuestProgress progress, QuestObjectiveNodeData objective)
        {
            if (!progress.activeNodeGuids.Contains(objective.Guid))
            {
                progress.activeNodeGuids.Add(objective.Guid);
            }

            progress.nodeProgressCounts.TryAdd(objective.Guid, 0);
        }

        private static bool MarkCompleted(QuestProgress progress, string nodeGuid)
        {
            if (progress.completedNodeGuids.Contains(nodeGuid))
            {
                return false;
            }

            progress.completedNodeGuids.Add(nodeGuid);
            return true;
        }

        private static bool IsCompleted(QuestProgress progress, string nodeGuid)
        {
            return progress.completedNodeGuids.Contains(nodeGuid);
        }
    }
}
