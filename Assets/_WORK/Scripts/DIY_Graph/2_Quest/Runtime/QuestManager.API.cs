using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>게임과 UI에서 Quest 진행을 제어하거나 정보를 조회하는 공개 API입니다.</summary>
    public static partial class QuestManager
    {
        //============================== 등록 ==============================

        /// <summary>게임에서 준비한 Quest 그래프 목록을 등록합니다. 진행 기록은 초기화하지 않습니다.</summary>
        public static void Initialize(IEnumerable<QuestContainer> containers)
        {
            // 생성에 성공한 등록부만 교체하므로 오류가 나면 기존 등록 목록을 유지합니다.
            QuestContainerRegistry.Instance = new QuestContainerRegistry(containers);
        }

        //============================== 조회(Interaction Entry쪽) ==============================

        /// <summary>게임에서 등록한 Quest 그래프를 원래 등록 순서로 반환합니다.</summary>
        public static IReadOnlyList<QuestContainer> RegisteredQuests => Registry.Containers;

        /// <summary>등록한 Quest 그래프 하나를 ID로 찾기</summary>
        public static bool GetQuest(int questId, out QuestContainer container)
        {
            return Registry.GetContainer(questId, out container);
        }

        /// <summary>상호작용 대상 ID와 일치하는 Quest 선택 항목을 원본 순서로 반환합니다.</summary>
        public static QuestSuggestion[] GetQuestSuggestions(IQuestController controller, string interactionTargetId)
        {
            return GetQuestSuggestions(controller, new[] { interactionTargetId });
        }

        /// <summary>여러 상호작용 대상 ID와 일치하는 Quest 선택 항목을 한 번에 조회합니다.</summary>
        public static QuestSuggestion[] GetQuestSuggestions(IQuestController controller, IEnumerable<string> interactionTargetIds)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
            }

            return QuestInteractionQuery
                .GetQuestSuggestions(Registry, controller, interactionTargetIds)
                .ToArray();
        }

        //==============================조회(Quest Start )=================================

        /// <summary>등록된 Quest 그래프에서 상호작용 대상 ID와 일치하는 모든 대화 후보를 반환합니다.</summary>
        public static DialogueCandidateNodeData[] GetDialogueCandidates(IQuestController controller, string interactionTargetId)
        {
            return GetDialogueCandidates(controller, new[] { interactionTargetId });
        }

        /// <summary>여러 상호작용 대상 ID와 일치하는 대화 후보를 한 번에 조회합니다.</summary>
        public static DialogueCandidateNodeData[] GetDialogueCandidates(IQuestController controller, IEnumerable<string> interactionTargetIds)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
            }

            return QuestInteractionQuery
                .GetDialogueCandidates(Registry, controller, interactionTargetIds)
                .ToArray();
        }

        /// <summary>진행 중이거나 완료 보고가 가능한 Quest를 반환합니다.</summary>
        public static QuestProgress[] GetActiveQuests(IQuestController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
            }

            return controller.QuestProgress.Values
                .Where(progress => progress != null && (progress.state == QuestState.InProgress || progress.state == QuestState.CanComplete))
                .ToArray();
        }

        /// <summary>현재 활성화된 목표를 반환합니다.</summary>
        public static QuestObjectiveProgress[] GetCurrentObjectives(IQuestController controller, int questId)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
            }

            controller.QuestProgress.TryGetValue(questId, out QuestProgress progress);
            if (!Registry.GetQuestGraphIndex(questId, out _, out QuestGraphIndex index)
                || progress == null)
            {
                return Array.Empty<QuestObjectiveProgress>();
            }

            List<QuestObjectiveProgress> objectives = new ();
            foreach (string guid in progress.ActiveNodeGuids)
            {
                if (!index.Nodes.TryGetValue(guid, out NodeBaseData nodeData))
                {
                    continue;
                }

                if (nodeData is QuestObjectiveNodeData objectiveData)
                {
                    progress.NodeProgressCounts.TryGetValue(guid, out int count);
                    objectives.Add(new QuestObjectiveProgress(questId, objectiveData, count));
                }
            }

            return objectives.ToArray();
        }

        //=========================== 시작 및 수락 ===========================

        /// <summary>그래프에서 제공된 Quest가 여전히 수락 가능한지 확인하고 시작합니다.</summary>
        public static bool AcceptQuest(IQuestController controller, QuestSuggestion suggestion)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
            }

            if (suggestion == null)
            {
                throw new ArgumentNullException(nameof(suggestion), "수락할 Quest 선택 항목가 필요합니다.");
            }

            if (!QuestInteractionQuery.TryRefreshQuestSuggestion(Registry, controller, suggestion, out QuestSuggestion refreshedSuggestion)
                || !refreshedSuggestion.IsAvailable)
            {
                return false;
            }

            return StartQuestFlow(controller, refreshedSuggestion.QuestId);
        }

        /// <summary>
        /// Quest를 명시적으로 시작합니다. <para></para>
        /// 게임 흐름이 시작을 이미 결정한 경우에 사용합니다.
        /// </summary>
        /// <remarks>NotStarted 상태에서만 시작합니다. 재시작하려면 먼저 ResetQuest를 호출하세요.</remarks>
        public static bool StartQuest(IQuestController controller, int questId)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
            }

            return StartQuestFlow(controller, questId);
        }

        //=========================== 목표 진행 =============================

        /// <summary>게임 이벤트 하나를 조건이 일치하는 모든 활성 목표에 적용합니다.</summary>
        public static void ReportObjectiveProgress(IQuestController controller, string eventKey, int objectiveTargetId, int amount)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
            }

            // 반복 도중 게임 코드가 등록부를 교체해도 이번 호출은 처음 가져온 등록부를 사용합니다.
            QuestContainerRegistry registry = Registry;
            if (string.IsNullOrWhiteSpace(eventKey) || amount <= 0)
            {
                return;
            }

            eventKey = eventKey.Trim();

            // 이번 이벤트가 시작될 때의 목표와 실행 번호를 보관합니다. 처리 중 새로 열린 목표에는 같은 이벤트를 다시 적용하지 않습니다.
            var targets = controller.QuestProgress.Values
                .Where(item => item != null && item.state == QuestState.InProgress)
                .Select(progress => (Progress: progress, RunVersion: progress.runVersion, ActiveNodeGuids: progress.ActiveNodeGuids.ToArray()))
                .ToArray();

            foreach (var target in targets)
            {
                QuestProgress progress = target.Progress;
                if (!registry.GetQuestGraphIndex(progress.questId, out QuestContainer container, out QuestGraphIndex index))
                {
                    continue;
                }

                int runVersion = target.RunVersion;
                bool changed = false;
                foreach (string activeGuid in target.ActiveNodeGuids)
                {
                    if (!CanContinueQuest(controller, progress, runVersion))
                    {
                        break;
                    }

                    if (!progress.ActiveNodeGuids.Contains(activeGuid))
                    {
                        continue;
                    }

                    if (!index.Nodes.TryGetValue(activeGuid, out NodeBaseData activeNodeData)
                        || activeNodeData is not QuestObjectiveNodeData objectiveData
                        || objectiveData.EventKey != eventKey
                        || objectiveData.TargetId != objectiveTargetId)
                    {
                        continue;
                    }

                    changed |= ApplyObjectiveProgress(
                        controller,
                        container,
                        progress,
                        index,
                        objectiveData,
                        amount,
                        out bool executionSucceeded);

                    if (!executionSucceeded
                        || progress.state != QuestState.InProgress)
                    {
                        break;
                    }
                }

                if (changed && IsCurrentRun(controller, progress, runVersion))
                {
                    controller.OnQuestProgressChanged(container, progress);
                }
            }
        }

        /// <summary>현재 활성화된 목표 하나를 GUID로 지정해 진행시킵니다.</summary>
        public static bool AdvanceObjective(IQuestController controller, int questId, string objectiveNodeGuid, int amount = 1)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
            }

            if (string.IsNullOrWhiteSpace(objectiveNodeGuid) || amount <= 0)
            {
                return false;
            }

            controller.QuestProgress.TryGetValue(questId, out QuestProgress progress);
            if (!Registry.GetQuestGraphIndex(
                    questId,
                    out QuestContainer container,
                    out QuestGraphIndex index)
                || progress == null
                || progress.state != QuestState.InProgress)
            {
                return false;
            }

            if (!progress.ActiveNodeGuids.Contains(objectiveNodeGuid)
                || !index.Nodes.TryGetValue(objectiveNodeGuid, out NodeBaseData nodeData)
                || nodeData is not QuestObjectiveNodeData objectiveData)
            {
                return false;
            }

            int runVersion = progress.runVersion;
            bool changed = ApplyObjectiveProgress(
                controller,
                container,
                progress,
                index,
                objectiveData,
                amount,
                out bool executionSucceeded);
            if (changed && IsCurrentRun(controller, progress, runVersion))
            {
                controller.OnQuestProgressChanged(container, progress);
            }

            return changed && executionSucceeded;
        }

        //========================= 상태 변경 · 복원 =========================

        /// <summary>Quest 상태와 모든 노드 진행 기록을 시작 전 상태로 초기화</summary>
        public static bool ResetQuest(IQuestController controller, int questId)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
            }

            controller.QuestProgress.TryGetValue(questId, out QuestProgress progress);
            if (!Registry.GetContainer(questId, out QuestContainer container) || progress == null)
            {
                return false;
            }

            ResetProgress(progress);
            progress.state = QuestState.NotStarted;
            int runVersion = progress.runVersion;

            // 게임 알림에서 다시 시작하기 전에 NotStarted 대기를 먼저 처리합니다.
            ResumeDependentQuests(controller, questId);

            if (IsCurrentRun(controller, progress, runVersion))
            {
                controller.OnQuestProgressChanged(container, progress);
            }
            return true;
        }

        /// <summary>누적 진행 기록은 유지하고 상태를 변경하며, 진행이 끝나는 상태에서는 활성 노드를 정리합니다.</summary>
        public static bool SetQuestState(IQuestController controller, int questId, QuestState state)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
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
            if (!Registry.GetContainer(questId, out QuestContainer container)
                || progress == null)
            {
                return false;
            }

            if (state == QuestState.InProgress && progress.ActiveNodeGuids.Count == 0)
            {
                return false;
            }

            progress.state = state;
            if (state != QuestState.InProgress)
            {
                progress.ActiveNodeGuids.Clear();
            }

            int runVersion = progress.runVersion;

            // 게임 알림이 상태를 다시 바꾸기 전에 의존 Quest의 대기를 처리합니다.
            ResumeDependentQuests(controller, questId);

            if (IsCurrentRun(controller, progress, runVersion))
            {
                controller.OnQuestProgressChanged(container, progress);
            }
            return true;
        }

        /// <summary>게임 데이터 복원 후 Quest 간 대기를 재평가하고 활성 Quest의 상태를 알립니다.</summary>
        public static void ResumeRestoredQuests(IQuestController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "IQuestController를 구현한 객체를 controller에 전달하세요.");
            }

            // 복원 알림에서 등록부를 교체해도 이번 호출은 처음 가져온 등록부를 사용합니다.
            QuestContainerRegistry registry = Registry;

            foreach (int questId in controller.QuestProgress.Keys.ToArray())
            {
                // 앞선 알림이 진행 기록을 교체했을 수 있으므로 현재 데이터를 읽습니다.
                if (!controller.QuestProgress.TryGetValue(questId, out QuestProgress progress)
                    || progress == null
                    || (progress.state != QuestState.InProgress && progress.state != QuestState.CanComplete))
                {
                    continue;
                }

                registry.GetContainer(progress.questId, out QuestContainer container);
                if (container == null)
                {
                    Debug.LogWarning($"[Quest] 저장 데이터가 알 수 없는 Quest ID {progress.questId}를 참조합니다.");
                    continue;
                }

                controller.OnQuestProgressChanged(container, progress);
            }

            // 복원된 상태를 먼저 표시한 뒤 정상 진행을 재개합니다. 다른 게임 데이터도 복원된 뒤여야 합니다.
            foreach (int questId in registry.Containers.Select(container => container.QuestId).ToArray())
            {
                ResumeDependentQuests(controller, questId);
            }
        }
    }
}
