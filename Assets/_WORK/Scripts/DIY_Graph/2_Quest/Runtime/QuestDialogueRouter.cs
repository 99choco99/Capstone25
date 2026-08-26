using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>
    /// Quest 그래프에 저장된 조회 전용 대화 경로를 평가합니다. 고정 상호작용 대상 문자열과
    /// <see cref="IQuestController"/>에만 의존하며 프로젝트의 Player, NPC, UI나 씬 클래스는 참조하지 않습니다.
    /// </summary>
    public static class QuestDialogueRouter
    {
        private const int MaxRouteSteps = 128;

        /// <summary>
        /// 상호작용 시작점이 주어진 대상 ID 중 하나와 일치하는 모든 유효한 대화 요청을 반환합니다.
        /// 비어 있는 ID와 <c>Any</c>는 모든 대상과 일치합니다.
        /// </summary>
        public static List<DialogueRequest> Evaluate(
            IEnumerable<QuestContainer> questGraphs,
            IQuestController controller,
            IEnumerable<string> targetIds)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "대화 경로를 조회할 Quest Controller가 필요합니다.");
            }

            if (questGraphs == null)
            {
                return new List<DialogueRequest>();
            }

            var targetSet = new HashSet<string>(
                targetIds?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
                ?? Enumerable.Empty<string>());
            var requests = new List<DialogueRequest>();

            foreach (QuestContainer graph in questGraphs)
            {
                if (graph == null)
                {
                    continue;
                }

                if (!GraphAssetMigrator.TryMigrate(graph, out _, out string migrationError))
                {
                    Debug.LogError($"[Quest Dialogue] {migrationError}", graph);
                    continue;
                }

                if (!QuestGraphIndex.TryCreate(graph, out QuestGraphIndex index, out string indexError))
                {
                    Debug.LogError($"[Quest Dialogue] {indexError}", graph);
                    continue;
                }

                foreach (QuestEventEntryNodeData entry in index.Nodes.Values.OfType<QuestEventEntryNodeData>())
                {
                    if (MatchesTarget(entry.TargetId, targetSet))
                    {
                        TraverseRoute(graph, controller, index, entry, requests);
                    }
                }
            }

            return requests
                .OrderByDescending(request => request.Priority)
                .ThenBy(request => request.TopicName, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>상호작용 대상 ID 하나를 받을 때 사용하는 간편 오버로드입니다.</summary>
        public static List<DialogueRequest> Evaluate(
            IEnumerable<QuestContainer> questGraphs,
            IQuestController controller,
            string targetId)
        {
            return Evaluate(questGraphs, controller, new[] { targetId });
        }

        private static void TraverseRoute(
            QuestContainer graph,
            IQuestController controller,
            QuestGraphIndex index,
            QuestEventEntryNodeData entry,
            ICollection<DialogueRequest> requests)
        {
            var pending = new Queue<NodeBaseData>();
            EnqueueTargets(index, pending, entry.Guid, "Next");
            var visited = new HashSet<string>();

            int steps = 0;
            while (pending.Count > 0)
            {
                if (++steps > MaxRouteSteps)
                {
                    Debug.LogError($"[Quest Dialogue] '{graph.name}'의 경로 탐색이 {MaxRouteSteps}단계를 초과했습니다.", graph);
                    return;
                }

                NodeBaseData node = pending.Dequeue();
                if (!visited.Add(node.Guid))
                {
                    Debug.LogWarning($"[Quest Dialogue] '{graph.name}'의 노드 '{node.Guid}'에서 순환이 발견되었습니다.", graph);
                    continue;
                }

                switch (node)
                {
                    case DialogueRequestNodeData requestNode when requestNode.DialogueReference.GraphAsset != null:
                        requests.Add(new DialogueRequest(
                            requestNode.DialogueReference,
                            requestNode.TopicName,
                            requestNode.Priority,
                            graph.questId.ToString()));
                        break;

                    case QuestStateConditionNodeData stateCondition:
                        QuestProgress progress = controller.GetQuestStatus(stateCondition.QuestId);
                        bool stateMatches = progress != null && progress.state == stateCondition.TargetState;
                        EnqueueTargets(index, pending, node.Guid, stateMatches ? "True" : "False");
                        break;

                    case QuestConditionBranchNodeData customCondition:
                        QuestProgress routeProgress = controller.GetQuestStatus(graph.questId);
                        var context = new QuestExecutionContext(controller, graph, routeProgress, customCondition);
                        bool evaluated = QuestEventRegistry.TryEvaluateCondition(
                            customCondition.Condition,
                            controller,
                            context,
                            out bool result,
                            out bool registered);
                        if (!evaluated && !registered && controller is IQuestConditionResolver resolver)
                        {
                            evaluated = resolver.TryEvaluateCondition(customCondition, out result);
                        }

                        if (evaluated)
                        {
                            EnqueueTargets(index, pending, node.Guid, result ? "True" : "False");
                        }
                        else
                        {
                            Debug.LogWarning(
                                registered
                                    ? $"[Quest Dialogue] 등록된 Condition '{customCondition.Condition.Key}' 실행에 실패했습니다."
                                    : $"[Quest Dialogue] Condition '{customCondition.Condition.Key}'을 처리한 Handler가 없습니다.",
                                graph);
                        }
                        break;

                    default:
                        Debug.LogWarning(
                            $"[Quest Dialogue] 노드 타입 '{node.GetType().Name}'은 조회 전용 경로에서 안전하지 않아 탐색을 종료합니다.",
                            graph);
                        break;
                }
            }
        }

        private static void EnqueueTargets(
            QuestGraphIndex index,
            Queue<NodeBaseData> pending,
            string guid,
            string port)
        {
            if (!index.OutgoingByPort.TryGetValue((guid, port), out List<NodeLinkData> outgoing))
            {
                return;
            }

            foreach (NodeLinkData link in outgoing)
            {
                pending.Enqueue(index.Nodes[link.TargetNodeGuid]);
            }
        }

        private static bool MatchesTarget(string entryTargetId, ISet<string> targetIds)
        {
            return string.IsNullOrWhiteSpace(entryTargetId)
                   || entryTargetId.Trim() == "Any"
                   || targetIds.Contains(entryTargetId.Trim());
        }

    }
}
