using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>
    /// Quest 그래프에 저장된 조회 전용 상호작용 경로를 평가합니다. 고정 상호작용 대상 문자열과
    /// <see cref="IQuestController"/>에만 의존하며 프로젝트의 Player, NPC, UI나 씬 클래스는 참조하지 않습니다.
    /// 이 경로에서 호출하는 Condition은 후보를 읽는 동안 게임 상태를 변경하지 않아야 합니다.
    /// </summary>
    internal static class QuestInteractionQuery
    {
        private const int MaxInteractionSteps = 128;

        /// <summary>
        /// 상호작용 시작점이 주어진 대상 ID 중 하나와 일치하는 모든 유효한 대화 후보를 반환합니다.
        /// 비어 있는 ID는 모든 대상과 일치합니다.
        /// </summary>
        internal static List<DialogueCandidate> GetDialogueCandidates(
            IEnumerable<QuestContainer> questGraphs,
            IQuestController controller,
            IEnumerable<string> targetIds)
        {
            var candidates = new List<DialogueCandidate>();
            CollectInteractionResults(questGraphs, controller, targetIds, candidates, null);
            return candidates;
        }

        /// <summary>상호작용 시작점에서 도달 가능한 모든 Quest 수락 후보를 반환합니다.</summary>
        internal static List<QuestOffer> GetQuestOffers(
            IEnumerable<QuestContainer> questGraphs,
            IQuestController controller,
            IEnumerable<string> targetIds)
        {
            var offers = new List<QuestOffer>();
            CollectInteractionResults(questGraphs, controller, targetIds, null, offers);
            return offers;
        }

        /// <summary>후보를 만든 같은 시작점부터 조건을 다시 평가하고 현재 결과를 반환합니다.</summary>
        internal static bool TryRefreshOffer(
            IQuestController controller,
            QuestOffer offer,
            out QuestOffer refreshed)
        {
            refreshed = null;
            QuestContainer graph = offer.Definition;
            if (!TryCreateIndex(graph, out QuestGraphIndex index)
                || !index.Nodes.TryGetValue(offer.SourceEntryGuid, out NodeBaseData entryData)
                || entryData is not QuestInteractionEntryNodeData entry)
            {
                return false;
            }

            var offers = new List<QuestOffer>();
            TraverseInteraction(graph, controller, index, entry, null, offers);
            refreshed = offers.FirstOrDefault(candidate => candidate.SourceNodeGuid == offer.SourceNodeGuid);
            return refreshed != null;
        }

        private static void CollectInteractionResults(
            IEnumerable<QuestContainer> questGraphs,
            IQuestController controller,
            IEnumerable<string> targetIds,
            ICollection<DialogueCandidate> candidates,
            ICollection<QuestOffer> offers)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "상호작용 경로를 조회할 Quest Controller가 필요합니다.");
            }

            if (questGraphs == null)
            {
                return;
            }

            var targetSet = new HashSet<string>(
                targetIds?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
                ?? Enumerable.Empty<string>());

            foreach (QuestContainer graph in questGraphs)
            {
                if (graph == null || !TryCreateIndex(graph, out QuestGraphIndex index))
                {
                    continue;
                }

                foreach (QuestInteractionEntryNodeData entry in graph.Nodes.OfType<QuestInteractionEntryNodeData>())
                {
                    if (MatchesTarget(entry.TargetId, targetSet))
                    {
                        TraverseInteraction(graph, controller, index, entry, candidates, offers);
                    }
                }
            }
        }

        private static void TraverseInteraction(
            QuestContainer graph,
            IQuestController controller,
            QuestGraphIndex index,
            QuestInteractionEntryNodeData entry,
            ICollection<DialogueCandidate> candidates,
            ICollection<QuestOffer> offers)
        {
            var pending = new Queue<NodeBaseData>();
            EnqueueTargets(index, pending, entry.Guid, QuestPortNames.Next);
            var visited = new HashSet<string>();

            int steps = 0;
            while (pending.Count > 0)
            {
                if (++steps > MaxInteractionSteps)
                {
                    Debug.LogError(
                        $"[Quest Interaction] '{graph.name}'의 경로 탐색이 {MaxInteractionSteps}단계를 초과했습니다.",
                        graph);
                    return;
                }

                NodeBaseData nodeData = pending.Dequeue();
                if (!visited.Add(nodeData.Guid))
                {
                    Debug.LogWarning(
                        $"[Quest Interaction] '{graph.name}'의 노드 '{nodeData.Guid}'에서 순환이 발견되었습니다.",
                        graph);
                    continue;
                }

                switch (nodeData)
                {
                    case DialogueCandidateNodeData candidateNode when candidateNode.EntryPoint.GraphAsset != null:
                        candidates?.Add(new DialogueCandidate(
                            candidateNode.EntryPoint,
                            candidateNode.DisplayName,
                            candidateNode.Priority));
                        break;

                    case QuestOfferNodeData offerNode:
                        offers?.Add(new QuestOffer(
                            graph,
                            offerNode.DialogueEntryPoint,
                            offerNode.Priority,
                            offerNode.IsAvailable,
                            offerNode.BlockReason,
                            entry.Guid,
                            offerNode.Guid));
                        break;

                    case QuestStateConditionNodeData stateCondition:
                        controller.QuestProgress.TryGetValue(stateCondition.QuestId, out QuestProgress progress);
                        QuestState currentState = progress?.state ?? QuestState.NotStarted;
                        bool stateMatches = currentState == stateCondition.TargetState;
                        EnqueueTargets(
                            index,
                            pending,
                            nodeData.Guid,
                            stateMatches ? QuestPortNames.True : QuestPortNames.False);
                        break;

                    case QuestConditionNodeData customCondition:
                        controller.QuestProgress.TryGetValue(graph.QuestId, out QuestProgress routeProgress);
                        var executionContext = new QuestExecutionContext(
                            controller,
                            graph,
                            routeProgress,
                            customCondition);
                        bool evaluated = QuestMethodInvoker.TryEvaluateCondition(
                            customCondition.Condition,
                            controller,
                            executionContext,
                            out bool result,
                            out bool registered);

                        if (evaluated)
                        {
                            EnqueueTargets(
                                index,
                                pending,
                                nodeData.Guid,
                                result ? QuestPortNames.True : QuestPortNames.False);
                        }
                        else
                        {
                            Debug.LogWarning(
                                registered
                                    ? $"[Quest Interaction] 등록된 Condition '{customCondition.Condition?.Key}' 실행에 실패했습니다."
                                    : $"[Quest Interaction] 등록되지 않은 Condition '{customCondition.Condition?.Key}'입니다.",
                                graph);
                        }
                        break;

                    default:
                        Debug.LogWarning(
                            $"[Quest Interaction] 노드 타입 '{nodeData.GetType().Name}'은 조회 전용 경로에서 안전하지 않아 탐색을 종료합니다.",
                            graph);
                        break;
                }
            }
        }

        private static bool TryCreateIndex(QuestContainer graph, out QuestGraphIndex index)
        {
            index = null;
            if (!GraphAssetMigrator.TryMigrate(graph, out _, out string migrationError))
            {
                Debug.LogError($"[Quest Interaction] {migrationError}", graph);
                return false;
            }

            if (QuestGraphIndex.TryCreate(graph, out index, out string indexError))
            {
                return true;
            }

            Debug.LogError($"[Quest Interaction] {indexError}", graph);
            return false;
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
                   || targetIds.Contains(entryTargetId.Trim());
        }

    }
}
