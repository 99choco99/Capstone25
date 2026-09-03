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
        public static void ReportObjectiveProgress(IQuestController controller, string type, int targetId, int amount)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "QuestRunner를 만들려면 Quest Controller가 필요합니다.");
            }

            QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
            if (string.IsNullOrWhiteSpace(type) || amount <= 0 || registry == null)
            {
                return;
            }

            type = type.Trim();

            foreach (QuestProgress progress in controller.QuestProgress.Values
                         .Where(item => item != null && item.state == QuestState.InProgress)
                         .ToArray())
            {
                if (!registry.TryBuildQuestIndex(
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
                    if (progress.state != QuestState.InProgress)
                    {
                        break;
                    }

                    if (!progress.activeNodeGuids.Contains(activeGuid))
                    {
                        continue;
                    }

                    if (!flowIndex.Nodes.TryGetValue(activeGuid, out NodeBaseData activeNode)
                        || activeNode is not QuestObjectiveNodeData objective
                        || objective.ObjectiveType != type
                        || objective.TargetId != targetId)
                    {
                        continue;
                    }

                    changed |= ApplyObjectiveProgress(
                        controller,
                        container,
                        progress,
                        flowIndex,
                        objective,
                        amount,
                        out bool executionSucceeded);

                    if (!executionSucceeded
                        || progress.state != QuestState.InProgress)
                    {
                        break;
                    }
                }

                if (changed)
                {
                    controller.InvokeStatusChanged(container, progress);
                }
            }
        }

        /// <summary>현재 활성화된 목표 하나를 GUID로 지정해 진행시킵니다.</summary>
        public static bool AdvanceObjective(
            IQuestController controller,
            int questId,
            string objectiveNodeGuid,
            int amount = 1)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "Quest 목표를 진행할 Controller가 필요합니다.");
            }

            if (string.IsNullOrWhiteSpace(objectiveNodeGuid) || amount <= 0)
            {
                return false;
            }

            QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
            controller.QuestProgress.TryGetValue(questId, out QuestProgress progress);
            if (registry == null
                || progress == null
                || progress.state != QuestState.InProgress
                || !registry.TryBuildQuestIndex(
                    questId,
                    out QuestContainer container,
                    out QuestGraphIndex flowIndex))
            {
                return false;
            }

            progress.EnsureCollections();
            if (!progress.activeNodeGuids.Contains(objectiveNodeGuid)
                || !flowIndex.Nodes.TryGetValue(objectiveNodeGuid, out NodeBaseData nodeData)
                || nodeData is not QuestObjectiveNodeData objective)
            {
                return false;
            }

            bool changed = ApplyObjectiveProgress(
                controller,
                container,
                progress,
                flowIndex,
                objective,
                amount,
                out bool executionSucceeded);
            if (changed)
            {
                controller.InvokeStatusChanged(container, progress);
            }

            return changed && executionSucceeded;
        }

        /// <summary>그래프에서 제공된 Quest가 여전히 수락 가능한지 확인하고 시작합니다.</summary>
        public static bool TryStartQuest(IQuestController controller, QuestOffer offer)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "Quest를 시작할 Controller가 필요합니다.");
            }

            if (offer == null)
            {
                throw new ArgumentNullException(nameof(offer), "시작할 Quest Offer가 필요합니다.");
            }

            QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
            if (registry == null
                || !registry.TryGetDefinition(offer.QuestId, out QuestContainer registered)
                || registered != offer.Definition
                || !QuestInteractionQuery.TryRefreshOffer(controller, offer, out QuestOffer refreshed)
                || !refreshed.IsAvailable)
            {
                return false;
            }

            return StartQuestFlow(controller, refreshed.QuestId);
        }

        /// <summary>
        /// Offer 조건을 거치지 않고 Quest를 명시적으로 시작합니다.
        /// 컷신, 튜토리얼과 다른 Quest처럼 게임 흐름이 시작을 이미 결정한 경우에 사용합니다.
        /// </summary>
        public static bool ForceStartQuest(IQuestController controller, int questId)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "Quest를 시작할 Controller가 필요합니다.");
            }

            return StartQuestFlow(controller, questId);
        }

        /// <summary>Quest 상태와 모든 노드 진행 기록을 시작 전 상태로 초기화합니다.</summary>
        public static bool ResetQuest(IQuestController controller, int questId)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "Quest를 초기화할 Controller가 필요합니다.");
            }

            controller.QuestProgress.TryGetValue(questId, out QuestProgress progress);
            QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
            if (progress == null || registry == null || !registry.TryGetDefinition(questId, out QuestContainer container))
            {
                return false;
            }

            ResetProgress(progress);
            progress.state = QuestState.NotStarted;
            controller.InvokeStatusChanged(container, progress);
            ResumeWaitingQuests(controller, questId);
            return true;
        }

        /// <summary>누적 진행 기록은 유지하고 상태를 변경하며, 진행이 끝나는 상태에서는 활성 노드를 정리합니다.</summary>
        public static bool SetQuestState(IQuestController controller, int questId, QuestState state)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "Quest 상태를 변경할 Controller가 필요합니다.");
            }

            if (!Enum.IsDefined(typeof(QuestState), state))
            {
                return false;
            }

            if (state == QuestState.NotStarted)
            {
                return ResetQuest(controller, questId);
            }

            controller.QuestProgress.TryGetValue(questId, out QuestProgress progress);
            QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
            if (progress == null
                || registry == null
                || !registry.TryGetDefinition(questId, out QuestContainer container))
            {
                return false;
            }

            progress.EnsureCollections();
            if (state == QuestState.InProgress && progress.activeNodeGuids.Count == 0)
            {
                return false;
            }

            progress.state = state;
            if (state != QuestState.InProgress)
            {
                progress.activeNodeGuids.Clear();
            }

            controller.InvokeStatusChanged(container, progress);
            ResumeWaitingQuests(controller, questId);
            return true;
        }

        /// <summary>저장 데이터 적용 뒤 활성 Quest의 상태 변경 알림을 다시 보냅니다.</summary>
        public static void NotifyRestoredQuests(IQuestController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "Quest 진행 데이터를 복원할 Controller가 필요합니다.");
            }

            QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
            if (registry == null)
            {
                throw new InvalidOperationException(
                    "Quest 복원 알림을 보내기 전에 QuestDefinitionRegistry.Initialize를 호출해야 합니다.");
            }

            int notifiedCount = 0;
            foreach (QuestProgress progress in controller.QuestProgress.Values
                         .Where(item => item != null
                                        && (item.state == QuestState.InProgress
                                            || item.state == QuestState.CanComplete))
                         .ToArray())
            {
                progress.EnsureCollections();
                QuestContainer definition = registry.GetDefinition(progress.questId);
                if (definition == null)
                {
                    Debug.LogWarning($"[Quest] 저장 데이터가 알 수 없는 Quest ID {progress.questId}를 참조합니다.");
                    continue;
                }

                controller.InvokeStatusChanged(definition, progress);
                notifiedCount++;
            }

            Debug.Log($"[Quest] 활성 Quest 기록 {notifiedCount}개의 복원 알림을 보냈습니다.");
        }

        /// <summary>Quest 실행 상태를 초기화하고 명시적인 Quest Start 노드에서 흐름을 시작합니다.</summary>
        private static bool StartQuestFlow(IQuestController controller, int questId)
        {
            if (questId <= 0)
            {
                return false;
            }

            QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
            if (registry == null
                || !registry.TryBuildQuestIndex(
                    questId,
                    out QuestContainer container,
                    out QuestGraphIndex flowIndex))
            {
                return false;
            }

            QuestStartNodeData[] starts = flowIndex.Nodes.Values.OfType<QuestStartNodeData>().ToArray();
            if (starts.Length != 1)
            {
                Debug.LogError(
                    $"[Quest] '{container.name}'에는 Quest Start 노드가 정확히 하나 필요하지만 {starts.Length}개 있습니다.",
                    container);
                return false;
            }

            QuestStartNodeData startData = starts[0];
            QuestProgress progress = GetOrCreateProgress(controller, container);
            if (progress.state == QuestState.InProgress || progress.state == QuestState.CanComplete)
            {
                return false;
            }

            ResetProgress(progress);
            progress.state = QuestState.InProgress;
            ResumeWaitingQuests(controller, questId);

            bool executionSucceeded = RunFromOutputs(
                controller,
                container,
                progress,
                flowIndex,
                startData.Guid,
                null);

            controller.InvokeStatusChanged(container, progress);
            return executionSucceeded;
        }

        /// <summary>필요할 때 Quest 진행 기록을 만들어 게임 코드의 사전 초기화 부담을 없앱니다.</summary>
        private static QuestProgress GetOrCreateProgress(IQuestController controller, QuestContainer container)
        {
            controller.QuestProgress.TryGetValue(container.QuestId, out QuestProgress progress);
            if (progress != null)
            {
                progress.EnsureCollections();
                return progress;
            }

            progress = new QuestProgress(container);
            controller.QuestProgress[container.QuestId] = progress;
            return progress;
        }

        private static void ResetProgress(QuestProgress progress)
        {
            progress.EnsureCollections();
            progress.activeNodeGuids.Clear();
            progress.nodeProgressCounts.Clear();
            progress.completedNodeGuids.Clear();
            progress.completedGateInputs.Clear();
        }

        /// <summary>목표 진행량을 반영하고 완료되면 연결된 Quest 흐름을 계속 실행합니다.</summary>
        private static bool ApplyObjectiveProgress(
            IQuestController controller,
            QuestContainer container,
            QuestProgress progress,
            QuestGraphIndex flowIndex,
            QuestObjectiveNodeData objective,
            int amount,
            out bool executionSucceeded)
        {
            executionSucceeded = true;
            int requiredAmount = Math.Max(1, objective.RequiredAmount);
            progress.nodeProgressCounts.TryGetValue(objective.Guid, out int currentAmount);
            int nextAmount = (int)Math.Min(requiredAmount, (long)currentAmount + amount);
            if (nextAmount == currentAmount)
            {
                return false;
            }

            progress.nodeProgressCounts[objective.Guid] = nextAmount;
            if (nextAmount < requiredAmount)
            {
                return true;
            }

            progress.activeNodeGuids.Remove(objective.Guid);
            MarkCompleted(progress, objective.Guid);
            executionSucceeded = RunFromOutputs(
                controller,
                container,
                progress,
                flowIndex,
                objective.Guid,
                null);
            return true;
        }

        /// <summary>상태가 바뀐 Quest를 기다리던 진행 그래프를 다시 확인합니다.</summary>
        private static void ResumeWaitingQuests(IQuestController controller, int changedQuestId)
        {
            QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
            if (registry == null)
            {
                return;
            }

            controller.QuestProgress.TryGetValue(changedQuestId, out QuestProgress changedProgress);
            if (changedProgress == null)
            {
                return;
            }

            foreach (QuestProgress progress in controller.QuestProgress.Values
                         .Where(item => item != null && item.state == QuestState.InProgress)
                         .ToArray())
            {
                if (!registry.TryBuildQuestIndex(
                        progress.questId,
                        out QuestContainer container,
                        out QuestGraphIndex flowIndex))
                {
                    continue;
                }

                progress.EnsureCollections();
                bool changed = false;
                int resumedNodeCount = 0;
                bool resumeAnotherNode;
                do
                {
                    resumeAnotherNode = false;
                    foreach (string activeGuid in progress.activeNodeGuids.ToArray())
                    {
                        if (progress.state != QuestState.InProgress
                            || !progress.activeNodeGuids.Contains(activeGuid))
                        {
                            break;
                        }

                        if (!flowIndex.Nodes.TryGetValue(activeGuid, out NodeBaseData nodeData)
                            || nodeData is not QuestWaitForQuestNodeData waitForQuest
                            || waitForQuest.TargetQuestId != changedQuestId
                            || changedProgress.state != waitForQuest.RequiredState)
                        {
                            continue;
                        }

                        progress.activeNodeGuids.Remove(activeGuid);
                        MarkCompleted(progress, activeGuid);
                        bool executionSucceeded = RunFromOutputs(
                            controller,
                            container,
                            progress,
                            flowIndex,
                            nodeData.Guid,
                            null);
                        changed = true;
                        resumeAnotherNode = executionSucceeded && progress.state == QuestState.InProgress;
                        break;
                    }

                    resumedNodeCount++;
                }
                while (resumeAnotherNode && resumedNodeCount <= MaxImmediateNodeSteps);

                if (changed)
                {
                    controller.InvokeStatusChanged(container, progress);
                }
            }
        }
    }
}
