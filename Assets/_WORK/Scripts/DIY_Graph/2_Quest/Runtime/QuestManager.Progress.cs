using System;
using System.Linq;

namespace UniversalGraph
{
    /// <summary>
    /// Quest 진행 그래프를 실행하는 런타임 해석기
    /// <para>설계도를 읽어서 실제 진행 기록을 바꾸는 실행기</para>
    /// </summary>
    public static partial class QuestManager
    {

        //==================================진행도 초기화================================

        /// <summary>
        /// 퀘스트 진행도 초기화
        /// </summary>
        private static void ResetProgress(QuestProgress progress)
        {
            progress.runVersion++;
            progress.ActiveNodeGuids.Clear();
            progress.NodeProgressCounts.Clear();
            progress.CompletedNodeGuids.Clear();
            progress.CompletedGateInputs.Clear();
        }

        //================================= 유효성 검사 =================================

        /// <summary>지금도 같은 진행 기록, 같은 실행 회차인가?</summary>
        private static bool IsCurrentRun(IQuestController controller, QuestProgress originalProgress, int runVersion)
        {
            return originalProgress.runVersion == runVersion
                   && controller.QuestProgress.TryGetValue(originalProgress.questId, out QuestProgress currentProgress)
                   && ReferenceEquals(currentProgress, originalProgress);
        }

        /// <summary>게임 Action이 퀘스트를 종료, 초기화했다면 이전 흐름을 중단</summary>
        private static bool CanContinueQuest(IQuestController controller, QuestProgress progress, int runVersion)
        {
            return progress.state == QuestState.InProgress && IsCurrentRun(controller, progress, runVersion);
        }

        
        //=============================== 진행 ===================================

        /// <summary>목표 진행량을 반영하고 완료되면 연결된 Quest 흐름을 계속 실행합니다.</summary>
        private static bool ApplyObjectiveProgress(
            IQuestController controller,
            QuestContainer container,
            QuestProgress progress,
            QuestGraphIndex index, QuestObjectiveNodeData objectiveData, int amount, out bool executionSucceeded)
        {
            executionSucceeded = true;
            int requiredAmount = objectiveData.RequiredAmount;
            progress.NodeProgressCounts.TryGetValue(objectiveData.Guid, out int currentAmount);
            int nextAmount = (int)Math.Min(requiredAmount, (long)currentAmount + amount);
            if (nextAmount == currentAmount)
            {
                return false;
            }

            progress.NodeProgressCounts[objectiveData.Guid] = nextAmount;
            if (nextAmount < requiredAmount)
            {
                return true;
            }

            CompleteNode(progress, objectiveData.Guid);
            executionSucceeded = ExecuteNextNode(
                controller,
                container,
                progress,
                index,
                objectiveData.Guid);
            return true;
        }

        /// <summary>해당 퀘스트의 변화를 기다리고 있던 퀘스트들에게 변화됐음을 알리는 함수</summary>
        private static void ResumeDependentQuests(IQuestController controller, int questId)
        {
            QuestContainerRegistry registry = QuestContainerRegistry.Instance;

            // 실행 오류일 때
            if (controller.QuestProgress.TryGetValue(questId, out QuestProgress changedProgress)
                && changedProgress?.state == QuestState.ExecutionError)
            {
                return;
            }

            foreach (QuestProgress progress in controller.QuestProgress.Values
                         .Where(progress => progress != null && progress.state == QuestState.InProgress)
                         .ToArray())
            {
                if (!registry.GetQuestGraphIndex(progress.questId, out QuestContainer container, out QuestGraphIndex index))
                {
                    continue;
                }

                int runVersion = progress.runVersion;
                bool resumed = false;
                foreach (string activeGuid in progress.ActiveNodeGuids.ToArray())
                {
                    if (!CanContinueQuest(controller, progress, runVersion))
                    {
                        break;
                    }

                    //현재 activeNode 중인 WaitForQuestNode를 찾기
                    if (!progress.ActiveNodeGuids.Contains(activeGuid)
                        || !index.Nodes.TryGetValue(activeGuid, out NodeBaseData nodeData)
                        || nodeData is not WaitForQuestNodeData waitForQuestData)
                    {
                        continue;
                    }

                    if (waitForQuestData.TargetQuestId != questId)
                    {
                        continue;
                    }

                    //현재 상태를 다시 읽고 원하는 상태 변화인지 체크
                    controller.QuestProgress.TryGetValue(questId, out QuestProgress targetProgress);
                    if ((targetProgress?.state ?? QuestState.NotStarted) != waitForQuestData.RequiredState)
                    {
                        continue;
                    }

                    CompleteNode(progress, activeGuid);
                    resumed = true;

                    if (!ExecuteNextNode(controller, container, progress, index, nodeData.Guid))
                    {
                        break;
                    }
                }

                if (resumed && IsCurrentRun(controller, progress, runVersion))
                {
                    controller.OnQuestProgressChanged(container, progress);
                }
            }
        }
    }
}
