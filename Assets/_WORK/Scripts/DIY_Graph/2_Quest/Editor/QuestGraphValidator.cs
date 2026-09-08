using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>런타임 로더 없이 Quest ID 참조를 검증하기 위한 프로젝트 에셋 캐시입니다.</summary>
    internal static class QuestAssetIndex
    {
        private static IReadOnlyList<QuestContainer> quests;

        static QuestAssetIndex()
        {
            EditorApplication.projectChanged += Invalidate;
        }

        public static IReadOnlyList<QuestContainer> Quests => quests ??= LoadQuests();

        /// <summary>주어진 고정 ID를 사용하는 Quest 에셋이 프로젝트에 이미 있는지 반환합니다.</summary>
        public static bool ContainsId(int questId)
        {
            return Quests.Any(quest => quest != null && quest.QuestId == questId);
        }

        private static void Invalidate()
        {
            quests = null;
        }

        private static IReadOnlyList<QuestContainer> LoadQuests()
        {
            return AssetDatabase.FindAssets("t:QuestContainer")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<QuestContainer>)
                .Where(quest => quest != null)
                .OrderBy(quest => quest.QuestId)
                .ThenBy(quest => quest.name, StringComparer.Ordinal)
                .ToArray();
        }
    }

    /// <summary>Play Mode에 들어가기 전에 Quest 진행과 대화 경로 문제를 보고합니다.</summary>
    public sealed class QuestGraphValidator : GraphValidatorBase<QuestContainer>
    {
        /// <summary>Quest 흐름, 참조, 바인딩, 도달 가능 여부와 완료 경로를 검사합니다.</summary>
        protected override void Validate(
            QuestContainer container,
            GraphValidationIndex index,
            ICollection<GraphValidationIssue> issues)
        {
            ValidateQuestMetadata();

            if (index.Nodes.Count == 0)
            {
                AddError(
                    "QUEST_EMPTY_GRAPH",
                    "이 Quest에 그래프 흐름이 없습니다. 고정 ID를 지정하고 Quest Start 노드를 추가하세요.");
                return;
            }

            QuestStartNodeData[] starts = index.Nodes.OfType<QuestStartNodeData>().ToArray();
            QuestInteractionEntryNodeData[] interactionEntries = index.Nodes.OfType<QuestInteractionEntryNodeData>().ToArray();
            HashSet<string> progressionReachable = index.GetReachableNode(starts.Select(node => node.Guid));
            HashSet<string> interactionReachable = index.GetReachableNode(interactionEntries.Select(node => node.Guid));

            if (starts.Length != 1)
            {
                AddError(
                    "QUEST_START_COUNT",
                    $"Quest 그래프에는 Quest Start 노드가 정확히 하나 필요하지만 {starts.Length}개 발견되었습니다.");
            }

            foreach (NodeBaseData node in index.Nodes)
            {
                bool inProgression = progressionReachable.Contains(node.Guid);
                bool inInteraction = interactionReachable.Contains(node.Guid);

                if (inInteraction && !IsInteractionRouteNode(node))
                {
                    AddError(
                        "QUEST_ROUTE_UNSAFE_NODE",
                        $"'{node.GetType().Name}'은 Quest 흐름을 변경하므로 상호작용 대화 경로에서 사용할 수 없습니다.",
                        node.Guid);
                }

                switch (node)
                {
                    case QuestStartNodeData start:
                        RequireAtLeastOneOutput(start.Guid, QuestPortNames.Next);
                        break;

                    case QuestInteractionEntryNodeData entry:
                        RequireExactlyOneOutput(entry.Guid, QuestPortNames.Next);
                        break;

                    case QuestObjectiveNodeData objective:
                        if (string.IsNullOrWhiteSpace(objective.EventKey))
                        {
                            AddError("QUEST_OBJECTIVE_KEY", "이벤트 키가 필요합니다.", objective.Guid);
                        }
                        if (objective.RequiredAmount < 1)
                        {
                            AddError(
                                "QUEST_OBJECTIVE_AMOUNT",
                                "Objective Required Amount는 1 이상이어야 합니다.",
                                objective.Guid);
                        }
                        if (inProgression)
                        {
                            RequireAtLeastOneOutput(objective.Guid, QuestPortNames.Next);
                        }
                        break;

                    case QuestConditionNodeData condition:
                        ValidateMethodBinding(
                            condition.Guid,
                            MethodKind.Condition,
                            condition.Condition,
                            "Custom Condition",
                            required: true);
                        ValidateConditionOutputs(condition.Guid, inProgression);
                        break;

                    case QuestStateConditionNodeData stateCondition:
                        ValidateQuestReference(stateCondition.QuestId, "Quest 상태 조건", stateCondition.Guid);
                        ValidateConditionOutputs(stateCondition.Guid, inProgression);
                        break;

                    case QuestAndGateNodeData gate:
                        int connectedSources = index.GetLinkInTargetPorts(gate.Guid)
                            .Select(link => link.StartNodeGuid)
                            .Distinct()
                            .Count();
                        if (connectedSources < 2)
                        {
                            AddWarning(
                                "QUEST_REDUNDANT_AND",
                                $"AND Gate: 서로 다른 입력이 2개 미만입니다 (현재 {connectedSources}개).",
                                gate.Guid);
                        }
                        if (inProgression)
                        {
                            RequireAtLeastOneOutput(gate.Guid, QuestPortNames.Next);
                        }
                        break;

                    case QuestActionNodeData action:
                        ValidateMethodBinding(
                            action.Guid,
                            MethodKind.Action,
                            action.Action,
                            "Quest Action",
                            required: true);
                        if (inProgression)
                        {
                            RequireAtLeastOneOutput(action.Guid, QuestPortNames.Next);
                        }
                        break;

                    case QuestStateChangeNodeData stateChange:
                        if (stateChange.NewState != QuestState.InProgress
                            && stateChange.NewState != QuestState.CanComplete
                            && stateChange.NewState != QuestState.TurnedIn)
                        {
                            AddError(
                                "QUEST_STATE_CHANGE_TARGET",
                                "State Change는 InProgress, CanComplete, TurnedIn만 선택할 수 있습니다. " +
                                "실패는 Fail 노드, 초기화는 QuestRunner.ResetQuest를 사용하세요.",
                                stateChange.Guid);
                        }

                        int stateOutputCount = index.GetLinkInStartPort(
                            stateChange.Guid,
                            QuestPortNames.Next).Count;
                        if (stateChange.NewState != QuestState.InProgress && stateOutputCount > 0)
                        {
                            AddError(
                                "QUEST_TERMINAL_STATE_OUTPUT",
                                $"{stateChange.NewState}은 현재 Quest 흐름을 끝내므로 나가는 연결선을 가질 수 없습니다.",
                                stateChange.Guid);
                        }
                        else if (inProgression
                                 && stateChange.NewState == QuestState.InProgress
                                 && stateOutputCount == 0)
                        {
                            AddError(
                                "QUEST_STATE_DEAD_END",
                                "InProgress 상태로 유지하는 State Change는 다음 진행 노드에 연결되어야 합니다.",
                                stateChange.Guid);
                        }
                        break;

                    case QuestRewardNodeData reward:
                        ValidateMethodBinding(
                            reward.Guid,
                            MethodKind.Action,
                            reward.RewardAction,
                            "Reward Action");
                        if (inProgression)
                        {
                            RequireAtLeastOneOutput(reward.Guid, QuestPortNames.Next);
                        }
                        break;

                    case QuestFailNodeData fail:
                        if (index.GetLinkInStartPort(fail.Guid).Count > 0)
                        {
                            AddError("QUEST_FAIL_OUTPUT", "Fail은 종점이므로 나가는 연결선을 가질 수 없습니다.", fail.Guid);
                        }
                        break;

                    case QuestWaitForQuestNodeData waitForQuest:
                        ValidateQuestReference(waitForQuest.TargetQuestId, "대기할 Quest", waitForQuest.Guid);
                        if (waitForQuest.TargetQuestId == container.QuestId)
                        {
                            AddWarning("QUEST_SELF_DEPENDENCY", "자기 Quest 대기: 지정한 상태에 도달할 수 있는지 확인하세요.", waitForQuest.Guid);
                        }
                        else if (inProgression)
                        {
                            ValidateWaitDependencyCycle(waitForQuest.TargetQuestId, waitForQuest.Guid);
                        }
                        if (inProgression)
                        {
                            RequireAtLeastOneOutput(waitForQuest.Guid, QuestPortNames.Next);
                        }
                        break;

                    case DialogueCandidateNodeData candidate:
                        ValidateDialogueCandidate(candidate);
                        if (inProgression)
                        {
                            AddError(
                                "QUEST_DIALOGUE_IN_PROGRESS_FLOW",
                                "Dialogue Candidate는 대화 경로의 종점이므로 Quest 진행을 앞으로 이동시킬 수 없습니다.",
                                candidate.Guid);
                        }
                        break;

                    case QuestOfferNodeData offer:
                        ValidateQuestOffer(offer);
                        if (inProgression)
                        {
                            AddError(
                                "QUEST_OFFER_IN_PROGRESS_FLOW",
                                "Quest Offer는 상호작용 경로의 종점이므로 Quest 진행 흐름에서 사용할 수 없습니다.",
                                offer.Guid);
                        }
                        break;

                    default:
                        AddError(
                            "QUEST_UNSUPPORTED_NODE",
                            $"QuestRunner가 노드 타입 '{node.GetType().Name}'을 지원하지 않습니다.",
                            node.Guid);
                        break;
                }
            }

            HashSet<string> reachable = new(progressionReachable);
            reachable.UnionWith(interactionReachable);
            if (progressionReachable.Count > 0)
            {
                foreach (NodeBaseData node in index.Nodes.Where(node => !reachable.Contains(node.Guid)))
                {
                    AddWarning(
                        "QUEST_UNREACHABLE",
                        "Start 또는 Interaction Entry에서 도달할 수 없는 노드입니다.",
                        node.Guid);
                }
            }

            foreach (string nodeGuid in index.FindCycleNodes(_ => true))
            {
                AddError(
                    "QUEST_CYCLE",
                    "완료된 대기 노드는 다시 방문할 때 즉시 실행되므로 Quest 그래프에는 단방향 순환이 있을 수 없습니다.",
                    nodeGuid);
            }

            void ValidateQuestMetadata()
            {
                if (container.QuestId <= 0)
                {
                    AddError(
                        "QUEST_ID",
                        "양수인 고정 Quest ID를 지정하세요.");
                }

                QuestContainer[] duplicates = QuestAssetIndex.Quests
                    .Where(quest => quest != null && quest != container && quest.QuestId == container.QuestId)
                    .ToArray();
                if (duplicates.Length > 0)
                {
                    AddError(
                        "QUEST_DUPLICATE_ID",
                        $"Quest ID {container.QuestId}를 다음 에셋도 사용하고 있습니다: {string.Join(", ", duplicates.Select(quest => quest.name))}.");
                }
            }

            void ValidateConditionOutputs(string nodeGuid, bool requiredForProgression)
            {
                ValidateConditionalPort(QuestPortNames.True);
                ValidateConditionalPort(QuestPortNames.False);

                void ValidateConditionalPort(string portName)
                {
                    int count = index.GetLinkInStartPort(nodeGuid, portName).Count;
                    if (count > 1)
                    {
                        AddError("QUEST_CONDITION_OUTPUT", $"{portName}: 최대 1개 연결 가능 (현재 {count}개)", nodeGuid);
                    }
                    else if (requiredForProgression && count == 0)
                    {
                        AddError(
                            "QUEST_CONDITION_DEAD_END",
                            $"{portName}: 연결 1개 필요 (현재 0개)",
                            nodeGuid);
                    }
                }
            }

            void ValidateQuestReference(int questId, string label, string nodeGuid)
            {
                if (questId <= 0 || !QuestAssetIndex.ContainsId(questId))
                {
                    AddError("QUEST_MISSING_REFERENCE", $"{label}이 존재하지 않는 Quest ID {questId}를 참조합니다.", nodeGuid);
                }
            }

            void ValidateWaitDependencyCycle(int targetQuestId, string nodeGuid)
            {
                if (!CanReachQuest(targetQuestId, container.QuestId, new HashSet<int>()))
                {
                    return;
                }

                AddWarning(
                    "QUEST_WAIT_DEPENDENCY_CYCLE",
                    $"Quest {container.QuestId} ↔ {targetQuestId}: 서로 기다리며 멈추지 않는지 확인하세요.",
                    nodeGuid);

                bool CanReachQuest(int currentQuestId, int destinationQuestId, ISet<int> visitedQuestIds)
                {
                    if (currentQuestId == destinationQuestId)
                    {
                        return true;
                    }

                    if (!visitedQuestIds.Add(currentQuestId))
                    {
                        return false;
                    }

                    QuestContainer definition = QuestAssetIndex.Quests.FirstOrDefault(
                        quest => quest != null && quest.QuestId == currentQuestId);
                    if (definition == null)
                    {
                        return false;
                    }

                    GraphValidationIssue structureError = GraphValidator.ValidateStructure(definition)
                        .FirstOrDefault(issue => issue.Severity == GraphValidationSeverity.Error);
                    if (structureError != null)
                    {
                        AddError("QUEST_REFERENCE_STRUCTURE", $"Quest {currentQuestId}: {structureError.Message}", nodeGuid);
                        return false;
                    }

                    var definitionContext = new GraphValidationIndex(definition);
                    IEnumerable<string> startGuids = definitionContext.Nodes
                        .OfType<QuestStartNodeData>()
                        .Select(start => start.Guid);
                    HashSet<string> reachableGuids = definitionContext.GetReachableNode(startGuids);
                    foreach (QuestWaitForQuestNodeData dependency in definitionContext.Nodes
                                 .OfType<QuestWaitForQuestNodeData>()
                                 .Where(wait => reachableGuids.Contains(wait.Guid)))
                    {
                        if (CanReachQuest(dependency.TargetQuestId, destinationQuestId, visitedQuestIds))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            void ValidateMethodBinding(
                string nodeGuid,
                MethodKind kind,
                MethodBindingData binding,
                string label,
                bool required = false)
            {
                if (binding == null)
                {
                    string issueKind = "QUEST_REWARD_DATA";
                    if (required)
                    {
                        issueKind = kind == MethodKind.Action ? "QUEST_ACTION_DATA" : "QUEST_CONDITION_DATA";
                    }
                    AddError(issueKind, $"{label} 호출 정보가 없습니다.", nodeGuid);
                    return;
                }

                if (string.IsNullOrWhiteSpace(binding.Key))
                {
                    if (required)
                    {
                        string issueKind = kind == MethodKind.Action ? "QUEST_ACTION_KEY" : "QUEST_CONDITION_KEY";
                        AddError(issueKind, $"{label} Key가 필요합니다.", nodeGuid);
                    }
                    return;
                }

                if (!QuestMethodCatalog.GetMethodDescriptor(kind, binding.Key, out QuestMethodDescriptor descriptor))
                {
                    AddError(
                        "QUEST_METHOD_KEY",
                        $"등록되지 않은 Attribute {kind} 키 '{binding.Key}'입니다.",
                        nodeGuid);
                    return;
                }

                if (MethodArgumentCodec.TryDecodeAllArgumentData(binding.Arguments, descriptor, out _, out string error))
                {
                    return;
                }

                AddError(
                    "QUEST_METHOD_ARGUMENTS",
                    $"{label} '{binding.Key}': {error}",
                    nodeGuid);
            }

            void ValidateDialogueCandidate(DialogueCandidateNodeData candidate)
            {
                ValidateDialogueEntryPoint(candidate.EntryPoint, candidate.Guid, "Dialogue Candidate");

                if (index.GetLinkInStartPort(candidate.Guid).Count > 0)
                {
                    AddError(
                        "QUEST_DIALOGUE_OUTPUT",
                        "Dialogue Candidate는 조회 결과를 만드는 종점이므로 나가는 연결선을 가질 수 없습니다.",
                        candidate.Guid);
                }
            }

            void ValidateQuestOffer(QuestOfferNodeData offer)
            {
                if (offer.DialogueEntryPoint.GraphAsset != null)
                {
                    ValidateDialogueEntryPoint(offer.DialogueEntryPoint, offer.Guid, "Quest Offer");
                }

                if (index.GetLinkInStartPort(offer.Guid).Count > 0)
                {
                    AddError(
                        "QUEST_OFFER_OUTPUT",
                        "Quest Offer는 조회 결과를 만드는 종점이므로 나가는 연결선을 가질 수 없습니다.",
                        offer.Guid);
                }
            }

            void ValidateDialogueEntryPoint(
                DialogueEntryPoint entryPoint,
                string nodeGuid,
                string label)
            {
                DialogueContainer graph = entryPoint.GraphAsset;
                if (graph == null)
                {
                    AddError("QUEST_DIALOGUE_GRAPH", $"{label}에 Dialogue Graph 에셋이 없습니다.", nodeGuid);
                    return;
                }

                GraphValidationIssue structureError = GraphValidator.ValidateStructure(graph)
                    .FirstOrDefault(issue => issue.Severity == GraphValidationSeverity.Error);
                if (structureError != null)
                {
                    AddError("QUEST_DIALOGUE_GRAPH", $"{label} '{graph.name}': {structureError.Message}", nodeGuid);
                    return;
                }

                if (!graph.FindEntryNode(
                        entryPoint.EntryId,
                        out DialogueEntryNodeData entry,
                        out string error))
                {
                    AddError("QUEST_DIALOGUE_ENTRY", $"{label}: {error}", nodeGuid);
                    return;
                }

                int count = graph.NodeLinks.Count(link => link.StartNodeGuid == entry.Guid && link.StartPortName == DialoguePortNames.Next);
                if (count != 1)
                {
                    AddError(
                        "QUEST_DIALOGUE_ENTRY",
                        $"{label} Entry '{entry.EntryId}' Next: 연결 1개 필요 (현재 {count}개)",
                        nodeGuid);
                }
            }

            void RequireAtLeastOneOutput(string nodeGuid, string portName)
            {
                if (index.GetLinkInStartPort(nodeGuid, portName).Count == 0)
                {
                    AddError("QUEST_MISSING_OUTPUT", $"{portName}: 연결 필요", nodeGuid);
                }
            }

            void RequireExactlyOneOutput(string nodeGuid, string portName)
            {
                int count = index.GetLinkInStartPort(nodeGuid, portName).Count;
                if (count != 1)
                {
                    AddError("QUEST_OUTPUT_COUNT", $"{portName}: 연결 1개 필요 (현재 {count}개)", nodeGuid);
                }
            }

            void AddError(
                string issueKind,
                string message,
                string nodeGuid = null)
            {
                issues.Add(new GraphValidationIssue(
                    GraphValidationSeverity.Error,
                    issueKind,
                    message,
                    nodeGuid));
            }

            void AddWarning(string issueKind, string message, string nodeGuid = null)
            {
                issues.Add(new GraphValidationIssue(GraphValidationSeverity.Warning, issueKind, message, nodeGuid));
            }
        }

        private static bool IsInteractionRouteNode(NodeBaseData node)
        {
            return node is QuestInteractionEntryNodeData
                   || node is QuestStateConditionNodeData
                   || node is QuestConditionNodeData
                   || node is DialogueCandidateNodeData
                   || node is QuestOfferNodeData;
        }
    }
}
