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
            return Quests.Any(quest => quest != null && quest.questId == questId);
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
                .OrderBy(quest => quest.questId)
                .ThenBy(quest => quest.name, StringComparer.Ordinal)
                .ToArray();
        }
    }

    /// <summary>Play Mode에 들어가기 전에 Quest 진행과 대화 경로 문제를 보고합니다.</summary>
    public sealed class QuestGraphValidator : GraphValidator<QuestContainer>
    {
        /// <summary>Quest 흐름, 참조, 바인딩, 도달 가능 여부와 완료 경로를 검사합니다.</summary>
        protected override void Validate(
            QuestContainer container,
            GraphValidationContext context,
            ICollection<GraphValidationIssue> issues)
        {
            ValidateQuestMetadata();

            if (context.Nodes.Count == 0)
            {
                AddError(
                    "QUEST_EMPTY_GRAPH",
                    "이 Quest에 그래프 흐름이 없습니다. 고정 ID를 지정하고 Quest Start 노드를 추가하세요.");
                return;
            }

            QuestStartNodeData[] starts = context.Nodes.OfType<QuestStartNodeData>().ToArray();
            QuestEventEntryNodeData[] conversationEntries = context.Nodes.OfType<QuestEventEntryNodeData>().ToArray();
            HashSet<string> progressionReachable = context.GetReachableNodeGuids(starts.Select(node => node.Guid));
            HashSet<string> conversationReachable = context.GetReachableNodeGuids(conversationEntries.Select(node => node.Guid));

            if (starts.Length != 1)
            {
                AddError(
                    "QUEST_START_COUNT",
                    $"Quest 그래프에는 Quest Start 노드가 정확히 하나 필요하지만 {starts.Length}개 발견되었습니다.");
            }

            foreach (NodeBaseData node in context.Nodes.Where(node => node != null))
            {
                bool inProgression = progressionReachable.Contains(node.Guid);
                bool inConversation = conversationReachable.Contains(node.Guid);

                if (inConversation && !IsConversationRouteNode(node))
                {
                    AddError(
                        "QUEST_ROUTE_UNSAFE_NODE",
                        $"'{node.GetType().Name}'은 Quest 흐름을 변경하므로 상호작용 대화 경로에서 사용할 수 없습니다.",
                        node.Guid);
                }

                switch (node)
                {
                    case QuestStartNodeData start:
                        RequireAtLeastOneOutput(start.Guid, "Next", "Quest Start는 첫 번째 진행 노드에 연결되어야 합니다.");
                        break;

                    case QuestEventEntryNodeData entry:
                        RequireExactlyOneOutput(
                            entry.Guid,
                            "Next",
                            "Interaction Dialogue Entry는 경로 노드 하나에 정확히 연결되어야 합니다.");
                        break;

                    case QuestObjectiveNodeData objective:
                        if (string.IsNullOrWhiteSpace(objective.ObjectiveType))
                        {
                            AddError("QUEST_OBJECTIVE_KEY", "Objective Event Type이 필요합니다.", objective.Guid);
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
                            RequireAtLeastOneOutput(objective.Guid, "Next", "완료된 Objective는 다른 노드로 이어져야 합니다.");
                        }
                        break;

                    case QuestConditionBranchNodeData condition:
                        if (string.IsNullOrWhiteSpace(condition.Condition.Key))
                        {
                            AddError("QUEST_CONDITION_KEY", "Custom Condition Key가 필요합니다.", condition.Guid);
                        }
                        else
                        {
                            ValidateMethodCall(
                                MethodKind.Condition,
                                condition.Condition,
                                condition.Guid);
                        }
                        ValidateConditionOutputs(condition.Guid, inProgression);
                        break;

                    case QuestStateConditionNodeData stateCondition:
                        ValidateQuestReference(stateCondition.QuestId, "Quest 상태 조건", stateCondition.Guid);
                        ValidateConditionOutputs(stateCondition.Guid, inProgression);
                        break;

                    case QuestAndGateNodeData gate:
                        int connectedSources = context.GetIncoming(gate.Guid)
                            .Select(link => link.StartNodeGuid)
                            .Distinct()
                            .Count();
                        if (connectedSources < 2)
                        {
                            AddWarning(
                                "QUEST_REDUNDANT_AND",
                                $"AND Gate에 서로 다른 입력 Branch가 {connectedSources}개 있습니다. 두 개 이상 연결하거나 Gate를 제거하세요.",
                                gate.Guid);
                        }
                        if (inProgression)
                        {
                            RequireAtLeastOneOutput(gate.Guid, "Next", "AND Gate는 연결된 모든 Branch가 도착한 뒤 다음 노드로 이어져야 합니다.");
                        }
                        break;

                    case QuestActionTriggerNodeData action:
                        if (string.IsNullOrWhiteSpace(action.Action.Key))
                        {
                            AddError("QUEST_ACTION_KEY", "Quest Action Key가 필요합니다.", action.Guid);
                        }
                        else
                        {
                            ValidateMethodCall(
                                MethodKind.Action,
                                action.Action,
                                action.Guid);
                        }
                        if (inProgression)
                        {
                            RequireAtLeastOneOutput(action.Guid, "Next", "Quest Action은 다른 노드로 이어져야 합니다.");
                        }
                        break;

                    case QuestStateChangeNodeData stateChange:
                        if (inProgression
                            && context.GetOutgoing(stateChange.Guid, "Next").Count == 0
                            && stateChange.NewState != QuestState.Failed
                            && stateChange.NewState != QuestState.TurnedIn)
                        {
                            AddWarning(
                                "QUEST_STATE_DEAD_END",
                                $"Next 연결 없이 상태를 {stateChange.NewState}(으)로 변경하면 진행이 여기서 멈춥니다.",
                                stateChange.Guid);
                        }
                        break;

                    case QuestRewardNodeData reward:
                        if (!string.IsNullOrWhiteSpace(reward.RewardAction.Key))
                        {
                            ValidateMethodCall(
                                MethodKind.Action,
                                reward.RewardAction,
                                reward.Guid);
                        }
                        if (context.GetOutgoing(reward.Guid).Count > 0)
                        {
                            AddWarning(
                                "QUEST_REWARD_OUTPUT",
                                "Reward는 일반적으로 Quest를 완료 처리하므로 나가는 연결선의 실행을 보장할 수 없습니다.",
                                reward.Guid);
                        }
                        break;

                    case QuestFailNodeData fail:
                        if (context.GetOutgoing(fail.Guid).Count > 0)
                        {
                            AddError("QUEST_FAIL_OUTPUT", "Fail은 종점이므로 나가는 연결선을 가질 수 없습니다.", fail.Guid);
                        }
                        break;

                    case QuestSubGraphNodeData subQuest:
                        ValidateQuestReference(subQuest.SubQuestId, "하위 Quest", subQuest.Guid);
                        if (subQuest.SubQuestId == container.questId)
                        {
                            AddError("QUEST_SELF_SUBQUEST", "Quest는 자신을 하위 Quest로 사용할 수 없습니다.", subQuest.Guid);
                        }
                        if (inProgression)
                        {
                            RequireAtLeastOneOutput(subQuest.Guid, "Next", "하위 Quest가 완료되면 다음 노드로 이어져야 합니다.");
                        }
                        break;

                    case DialogueRequestNodeData request:
                        ValidateDialogueRequest(request);
                        if (inProgression)
                        {
                            AddError(
                                "QUEST_DIALOGUE_IN_PROGRESS_FLOW",
                                "Dialogue Request는 대화 경로의 종점이므로 Quest 진행을 앞으로 이동시킬 수 없습니다.",
                                request.Guid);
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
            reachable.UnionWith(conversationReachable);
            foreach (NodeBaseData node in context.Nodes.Where(node => node != null && !reachable.Contains(node.Guid)))
            {
                AddWarning(
                    "QUEST_UNREACHABLE",
                    "Quest Start 또는 Interaction Dialogue Entry에서 이 노드에 도달할 수 없습니다.",
                    node.Guid);
            }

            foreach (string nodeGuid in GraphValidatorRegistry.FindCycleNodes(context, _ => true))
            {
                AddError(
                    "QUEST_CYCLE",
                    "완료된 대기 노드는 다시 방문할 때 즉시 실행되므로 Quest 그래프에는 단방향 순환이 있을 수 없습니다.",
                    nodeGuid);
            }

            void ValidateQuestMetadata()
            {
                if (container.questId <= 0)
                {
                    AddError(
                        "QUEST_ID",
                        "양수인 고정 Quest ID를 지정하세요.");
                }

                if (string.IsNullOrWhiteSpace(container.questName))
                {
                    AddWarning("QUEST_NAME", "Quest 이름이 비어 있습니다.");
                }

                QuestContainer[] duplicates = QuestAssetIndex.Quests
                    .Where(quest => quest != null && quest != container && quest.questId == container.questId)
                    .ToArray();
                if (duplicates.Length > 0)
                {
                    AddError(
                        "QUEST_DUPLICATE_ID",
                        $"Quest ID {container.questId}를 다음 에셋도 사용하고 있습니다: {string.Join(", ", duplicates.Select(quest => quest.name))}.");
                }

                var seenPrerequisites = new HashSet<int>();
                foreach (int prerequisiteId in container.prerequisiteQuestIds ?? Enumerable.Empty<int>())
                {
                    if (!seenPrerequisites.Add(prerequisiteId))
                    {
                        AddError("QUEST_DUPLICATE_PREREQUISITE", $"선행 Quest ID {prerequisiteId}가 중복되었습니다.");
                    }
                    else if (prerequisiteId == container.questId)
                    {
                        AddError("QUEST_SELF_PREREQUISITE", "Quest는 자신을 선행 조건으로 요구할 수 없습니다.");
                    }
                    else if (!QuestAssetIndex.ContainsId(prerequisiteId))
                    {
                        AddError("QUEST_MISSING_PREREQUISITE", $"선행 Quest ID {prerequisiteId}가 존재하지 않습니다.");
                    }
                }
            }

            void ValidateConditionOutputs(string nodeGuid, bool requiredForProgression)
            {
                ValidateConditionalPort("True");
                ValidateConditionalPort("False");

                void ValidateConditionalPort(string portName)
                {
                    int count = context.GetOutgoing(nodeGuid, portName).Count;
                    if (count > 1)
                    {
                        AddError("QUEST_CONDITION_OUTPUT", $"{portName}에는 연결선 하나만 허용되지만 {count}개 발견되었습니다.", nodeGuid);
                    }
                    else if (requiredForProgression && count == 0)
                    {
                        AddError(
                            "QUEST_CONDITION_DEAD_END",
                            $"진행 경로가 이 Condition에 도달할 수 있으므로 {portName}을 연결해야 합니다.",
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

            void ValidateMethodCall(
                MethodKind kind,
                MethodCallData methodCall,
                string nodeGuid)
            {
                // 알 수 없는 키는 명시적인 구형 연결 인터페이스에서 처리할 수 있으므로 허용합니다.
                if (!QuestMethodCatalog.TryGetMethod(kind, methodCall.Key, out QuestMethodDescriptor descriptor))
                {
                    return;
                }

                if (MethodArgumentCodec.TryValidateArguments(methodCall.Arguments, descriptor, out string error))
                {
                    return;
                }

                AddError(
                    "QUEST_METHOD_ARGUMENTS",
                    $"Attribute {kind} '{methodCall.Key}'의 인수가 올바르지 않습니다: {error}",
                    nodeGuid);
            }

            void ValidateDialogueRequest(DialogueRequestNodeData request)
            {
                DialogueContainer graph = request.DialogueReference.GraphAsset;
                if (graph == null)
                {
                    AddError("QUEST_DIALOGUE_GRAPH", "Dialogue Request에 Dialogue Graph 에셋이 없습니다.", request.Guid);
                    return;
                }

                if (!graph.FindEntryNode(
                        request.DialogueReference.EntryId,
                        out DialogueStartNodeData entry,
                        out string error))
                {
                    AddError("QUEST_DIALOGUE_ENTRY", $"Dialogue Request가 올바르지 않습니다: {error}", request.Guid);
                }
                else
                {
                    var dialogueContext = new GraphValidationContext(graph);
                    IReadOnlyList<NodeLinkData> entryLinks = dialogueContext.GetOutgoing(entry.Guid, "Next");
                    if (entryLinks.Count != 1)
                    {
                        AddError(
                            "QUEST_DIALOGUE_ENTRY",
                            $"Dialogue Request의 Entry '{entry.EntryId}'는 Next 연결이 정확히 하나여야 하지만 " +
                            $"{entryLinks.Count}개 발견되었습니다.",
                            request.Guid);
                    }
                    else if (!dialogueContext.TryGetNode(entryLinks[0].TargetNodeGuid, out _))
                    {
                        AddError(
                            "QUEST_DIALOGUE_ENTRY",
                            $"Dialogue Request의 Entry '{entry.EntryId}'가 존재하지 않는 첫 노드를 참조합니다.",
                            request.Guid);
                    }
                }

                if (string.IsNullOrWhiteSpace(request.TopicName))
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Warning,
                        "QUEST_DIALOGUE_TOPIC",
                        "Dialogue Topic 이름이 비어 있습니다.",
                        request.Guid));
                }
            }

            void RequireAtLeastOneOutput(string nodeGuid, string portName, string message)
            {
                if (context.GetOutgoing(nodeGuid, portName).Count == 0)
                {
                    AddError("QUEST_MISSING_OUTPUT", message, nodeGuid);
                }
            }

            void RequireExactlyOneOutput(string nodeGuid, string portName, string message)
            {
                int count = context.GetOutgoing(nodeGuid, portName).Count;
                if (count != 1)
                {
                    AddError("QUEST_OUTPUT_COUNT", $"{message} {count}개 발견되었습니다.", nodeGuid);
                }
            }

            void AddError(
                string code,
                string message,
                string nodeGuid = null)
            {
                issues.Add(new GraphValidationIssue(
                    GraphValidationSeverity.Error,
                    code,
                    message,
                    nodeGuid));
            }

            void AddWarning(string code, string message, string nodeGuid = null)
            {
                issues.Add(new GraphValidationIssue(GraphValidationSeverity.Warning, code, message, nodeGuid));
            }
        }

        private static bool IsConversationRouteNode(NodeBaseData node)
        {
            return node is QuestEventEntryNodeData
                   || node is QuestStateConditionNodeData
                   || node is QuestConditionBranchNodeData
                   || node is DialogueRequestNodeData;
        }
    }
}
