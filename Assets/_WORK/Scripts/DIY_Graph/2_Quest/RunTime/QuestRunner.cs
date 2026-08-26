using System;
using System.Linq;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>
    /// Quest 진행 그래프를 항상 같은 결과로 실행하는 런타임 해석기입니다. 공개 메서드는 게임 이벤트를
    /// 진행 데이터에 연결하고, 실제 노드 해석은 같은 partial 클래스의 책임별 파일로 위임합니다.
    /// </summary>
    public static partial class QuestRunner
    {
        /// <summary>게임 이벤트 하나를 조건이 일치하는 모든 활성 목표에 적용합니다.</summary>
        public static void ProcessEvent(IQuestController controller, string type, int targetId, int amount)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "QuestRunner를 만들려면 Quest Controller가 필요합니다.");
            }

            QuestManager manager = QuestManager.Instance;
            if (string.IsNullOrWhiteSpace(type) || amount <= 0 || manager == null)
            {
                return;
            }

            foreach (QuestProgress progress in controller.QuestProgress.Values
                         .Where(item => item != null && item.state == QuestState.InProgress)
                         .ToArray())
            {
                if (!manager.TryBuildQuestIndex(
                        progress.questId,
                        out QuestContainer container,
                        out QuestGraphIndex flowIndex))
                {
                    continue;
                }

                progress.EnsureCollections();
                bool changed = false;
                foreach (string activeGuid in progress.activeNodeGuids.ToArray())
                {
                    if (!flowIndex.Nodes.TryGetValue(activeGuid, out NodeBaseData activeNode)
                        || activeNode is not QuestObjectiveNodeData objective
                        || objective.ObjectiveType != type
                        || objective.TargetId != targetId)
                    {
                        continue;
                    }

                    int requiredAmount = Math.Max(1, objective.RequiredAmount);
                    progress.nodeProgressCounts.TryGetValue(activeGuid, out int currentAmount);
                    long increased = (long)currentAmount + amount;
                    progress.nodeProgressCounts[activeGuid] = (int)Math.Min(requiredAmount, increased);
                    changed = true;

                    if (increased < requiredAmount)
                    {
                        continue;
                    }

                    progress.activeNodeGuids.Remove(activeGuid);
                    MarkCompleted(progress, activeGuid);
                    RunFromOutputs(controller, container, progress, flowIndex, objective.Guid, null);
                }

                if (changed)
                {
                    controller.InvokeStatusChanged(container, progress);
                }
            }
        }

        /// <summary>Quest 하나를 초기화하고 명시적인 Quest Start 노드에서 시작합니다.</summary>
        public static void StartQuestGraph(IQuestController controller, int questId)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "Quest를 시작할 Controller가 필요합니다.");
            }

            QuestProgress progress = controller.GetQuestStatus(questId);
            QuestManager manager = QuestManager.Instance;
            if (progress == null
                || manager == null
                || !manager.TryBuildQuestIndex(
                    questId,
                    out QuestContainer container,
                    out QuestGraphIndex flowIndex))
            {
                return;
            }

            progress.EnsureCollections();
            if (progress.state == QuestState.InProgress
                || progress.state == QuestState.CanComplete
                || progress.state == QuestState.TurnedIn)
            {
                return;
            }

            NodeBaseData entry = ResolveStartNode(container, flowIndex);
            if (entry == null)
            {
                Debug.LogError($"[Quest] '{container.name}'에 Quest Start 노드가 없습니다.", container);
                return;
            }

            progress.state = QuestState.InProgress;
            progress.currentNodeGuid = entry.Guid;
            progress.activeNodeGuids.Clear();
            progress.nodeProgressCounts.Clear();
            progress.completedNodeGuids.Clear();
            progress.completedGateInputs.Clear();

            if (entry is QuestObjectiveNodeData objective)
            {
                ActivateObjective(progress, objective);
            }
            else
            {
                RunFromOutputs(controller, container, progress, flowIndex, entry.Guid, null);
            }

            controller.InvokeStatusChanged(container, progress);
        }

        /// <summary>완료된 하위 Quest를 기다리던 상위 Quest 그래프를 다시 진행합니다.</summary>
        public static void NotifyQuestCompleted(IQuestController controller, int completedQuestId)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "Quest를 재개할 Controller가 필요합니다.");
            }

            QuestManager manager = QuestManager.Instance;
            if (manager == null)
            {
                return;
            }

            foreach (QuestProgress progress in controller.QuestProgress.Values
                         .Where(item => item != null && item.state == QuestState.InProgress)
                         .ToArray())
            {
                if (!manager.TryBuildQuestIndex(
                        progress.questId,
                        out QuestContainer container,
                        out QuestGraphIndex flowIndex))
                {
                    continue;
                }

                progress.EnsureCollections();
                bool changed = false;
                foreach (string activeGuid in progress.activeNodeGuids.ToArray())
                {
                    if (!flowIndex.Nodes.TryGetValue(activeGuid, out NodeBaseData node)
                        || node is not QuestSubGraphNodeData subGraph
                        || subGraph.SubQuestId != completedQuestId)
                    {
                        continue;
                    }

                    progress.activeNodeGuids.Remove(activeGuid);
                    MarkCompleted(progress, activeGuid);
                    RunFromOutputs(controller, container, progress, flowIndex, node.Guid, null);
                    changed = true;
                }

                if (changed)
                {
                    controller.InvokeStatusChanged(container, progress);
                }
            }
        }
    }
}
