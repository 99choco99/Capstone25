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
            public FlowStep(NodeBaseData nodeData, string sourceNodeGuid)
            {
                NodeData = nodeData;
                SourceNodeGuid = sourceNodeGuid;
            }

            public NodeBaseData NodeData { get; }
            public string SourceNodeGuid { get; }
        }

        /// <summary>출발 출력 하나에서 도달 가능한 즉시 노드를 재귀 호출 없이 처리합니다.</summary>
        private static bool RunFromOutputs(
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
                    return StopAfterExecutionError(controller, progress);
                }

                FlowStep step = queue.Dequeue();
                NodeBaseData nodeData = step.NodeData;
                if (nodeData is QuestStartNodeData || nodeData is QuestInteractionEntryNodeData)
                {
                    EnqueueOutputs(flowIndex, queue, nodeData.Guid, null);
                }
                else if (nodeData is QuestObjectiveNodeData objective)
                {
                    if (IsCompleted(progress, nodeData.Guid))
                    {
                        EnqueueOutputs(flowIndex, queue, nodeData.Guid, null);
                    }
                    else
                    {
                        ActivateObjective(progress, objective);
                    }
                }
                else if (nodeData is QuestConditionNodeData condition)
                {
                    var executionContext = new QuestExecutionContext(controller, container, progress, condition);
                    if (!QuestMethodInvoker.TryEvaluateCondition(
                            condition.Condition,
                            controller,
                            executionContext,
                            out bool result,
                            out bool handlerFound))
                    {
                        Debug.LogError(
                            handlerFound
                                ? $"[Quest] '{container.name}'의 Condition '{condition.Condition?.Key}' 실행에 실패했습니다."
                                : $"[Quest] '{container.name}'의 Condition '{condition.Condition?.Key}'을 처리한 Handler가 없습니다.",
                            container);
                        return StopAfterExecutionError(controller, progress);
                    }

                    EnqueueOutputs(
                        flowIndex,
                        queue,
                        nodeData.Guid,
                        result ? QuestPortNames.True : QuestPortNames.False);
                }
                else if (nodeData is QuestStateConditionNodeData stateCondition)
                {
                    controller.QuestProgress.TryGetValue(stateCondition.QuestId, out QuestProgress inspected);
                    QuestState currentState = inspected?.state ?? QuestState.NotStarted;
                    bool result = currentState == stateCondition.TargetState;
                    EnqueueOutputs(
                        flowIndex,
                        queue,
                        nodeData.Guid,
                        result ? QuestPortNames.True : QuestPortNames.False);
                }
                else if (nodeData is QuestAndGateNodeData gate)
                {
                    ProcessAndGate(progress, flowIndex, queue, gate, step.SourceNodeGuid);
                }
                else if (nodeData is QuestStateChangeNodeData stateChange)
                {
                    if (stateChange.NewState != QuestState.InProgress
                        && stateChange.NewState != QuestState.CanComplete
                        && stateChange.NewState != QuestState.TurnedIn)
                    {
                        Debug.LogError(
                            $"[Quest] State Change 노드는 상태를 {stateChange.NewState}(으)로 변경할 수 없습니다. " +
                            "실패는 Fail 노드, 초기화는 QuestRunner.ResetQuest를 사용하세요.",
                            container);
                        return StopAfterExecutionError(controller, progress);
                    }

                    if (MarkCompleted(progress, nodeData.Guid))
                    {
                        progress.state = stateChange.NewState;
                        if (stateChange.NewState != QuestState.InProgress)
                        {
                            progress.activeNodeGuids.Clear();
                        }

                        ResumeWaitingQuests(controller, progress.questId);
                    }

                    if (stateChange.NewState != QuestState.InProgress)
                    {
                        return true;
                    }

                    EnqueueOutputs(flowIndex, queue, nodeData.Guid, null);
                }
                else if (nodeData is QuestActionNodeData action)
                {
                    if (!IsCompleted(progress, nodeData.Guid))
                    {
                        if (!ExecuteAction(
                                controller,
                                container,
                                progress,
                                action,
                                action.Action,
                                "Action"))
                        {
                            return StopAfterExecutionError(controller, progress);
                        }

                        MarkCompleted(progress, nodeData.Guid);
                    }

                    EnqueueOutputs(flowIndex, queue, nodeData.Guid, null);
                }
                else if (nodeData is QuestFailNodeData)
                {
                    MarkCompleted(progress, nodeData.Guid);
                    progress.state = QuestState.Failed;
                    progress.activeNodeGuids.Clear();
                    ResumeWaitingQuests(controller, progress.questId);
                    return true;
                }
                else if (nodeData is QuestRewardNodeData reward)
                {
                    if (!IsCompleted(progress, nodeData.Guid))
                    {
                        if (!ExecuteAction(
                                controller,
                                container,
                                progress,
                                reward,
                                reward.RewardAction,
                                "Reward Action"))
                        {
                            return StopAfterExecutionError(controller, progress);
                        }

                        MarkCompleted(progress, nodeData.Guid);
                    }

                    EnqueueOutputs(flowIndex, queue, nodeData.Guid, null);
                }
                else if (nodeData is QuestWaitForQuestNodeData waitForQuest)
                {
                    if (IsCompleted(progress, nodeData.Guid))
                    {
                        EnqueueOutputs(flowIndex, queue, nodeData.Guid, null);
                        continue;
                    }

                    controller.QuestProgress.TryGetValue(waitForQuest.TargetQuestId, out QuestProgress targetProgress);
                    QuestState targetState = targetProgress?.state ?? QuestState.NotStarted;
                    if (targetState == waitForQuest.RequiredState)
                    {
                        MarkCompleted(progress, nodeData.Guid);
                        EnqueueOutputs(flowIndex, queue, nodeData.Guid, null);
                        continue;
                    }

                    if (!progress.activeNodeGuids.Contains(nodeData.Guid))
                    {
                        progress.activeNodeGuids.Add(nodeData.Guid);
                    }
                }
                else if (nodeData is DialogueCandidateNodeData || nodeData is QuestOfferNodeData)
                {
                    Debug.LogError(
                        $"[Quest] {nodeData.GetType().Name} '{nodeData.Guid}'는 상호작용 경로의 종점이므로 " +
                        "Quest 진행을 앞으로 이동시키지 않습니다.",
                        container);
                    return StopAfterExecutionError(controller, progress);
                }
                else
                {
                    Debug.LogError(
                        $"[Quest] '{container.name}'에서 지원하지 않는 노드 타입 '{nodeData.GetType().FullName}'을 발견했습니다.",
                        container);
                    return StopAfterExecutionError(controller, progress);
                }
            }

            if (progress.state == QuestState.InProgress && progress.activeNodeGuids.Count == 0)
            {
                Debug.LogError(
                    $"[Quest] '{container.name}'의 진행 경로가 활성 목표나 대기 노드 없이 끝났습니다.",
                    container);
                return StopAfterExecutionError(controller, progress);
            }

            return true;
        }

        private static bool StopAfterExecutionError(IQuestController controller, QuestProgress progress)
        {
            progress.state = QuestState.Failed;
            progress.activeNodeGuids.Clear();
            ResumeWaitingQuests(controller, progress.questId);
            return false;
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
