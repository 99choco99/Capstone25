using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>Quest 그래프를 직접 탐색하면 안 되는 UI와 게임 코드에 읽기 기능을 제공하는 API입니다.</summary>
	public static class QuestEngineAPI
	{
    /// <summary>진행 중이거나 완료 보고가 가능한 Quest를 반환합니다.</summary>
    public static QuestProgress[] GetActiveQuests(IQuestController controller)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller), "Quest 제목을 조회할 Controller가 필요합니다.");
        }

        return controller.QuestProgress.Values
            .Where(progress => progress != null
                               && (progress.state == QuestState.InProgress
                                   || progress.state == QuestState.CanComplete))
            .ToArray();
    }

    /// <summary>등록된 Quest의 표시 이름을 반환하며, 없으면 고정 대체 문구를 반환합니다.</summary>
    public static string GetQuestName(int questId)
    {
        QuestContainer template = QuestManager.Instance?.GetQuestTemplate(questId);
        return template != null ? template.questName : "알 수 없는 Quest";
    }

    /// <summary>등록된 Quest의 설명을 반환하며, 없으면 빈 문자열을 반환합니다.</summary>
    public static string GetQuestDescription(int questId)
    {
        QuestContainer template = QuestManager.Instance?.GetQuestTemplate(questId);
        return template != null ? template.description : string.Empty;
    }

    /// <summary>현재 활성화된 모든 목표 노드를 간단한 표시 문자열로 만듭니다.</summary>
    public static string GetCurrentObjectiveText(IQuestController controller, int questId)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller), "Quest 목표를 조회할 Controller가 필요합니다.");
        }

        QuestProgress progress = controller.GetQuestStatus(questId);
        QuestManager manager = QuestManager.Instance;
        if (progress == null
            || manager == null
            || !manager.TryBuildQuestIndex(questId, out _, out QuestGraphIndex index))
        {
            return "목표 없음";
        }

        progress.EnsureCollections();
        if (progress.activeNodeGuids.Count == 0)
        {
            return "목표 없음";
        }

        var text = new StringBuilder();
        foreach (string guid in progress.activeNodeGuids)
        {
            if (!index.Nodes.TryGetValue(guid, out NodeBaseData node))
            {
                continue;
            }

            if (node is QuestObjectiveNodeData objective)
            {
                progress.nodeProgressCounts.TryGetValue(guid, out int count);
                string label = string.IsNullOrWhiteSpace(objective.ObjectiveDescription)
                    ? objective.ObjectiveType
                    : objective.ObjectiveDescription;
                text.AppendLine($"- {label} ({count}/{Math.Max(1, objective.RequiredAmount)})");
            }
            else if (node is QuestSubGraphNodeData subGraph)
            {
                text.AppendLine($"- 하위 Quest {subGraph.SubQuestId} 완료");
            }
        }

        return text.Length > 0 ? text.ToString().TrimEnd() : "진행 중";
    }

    /// <summary>저장된 진행 상태를 Controller에 불러온 뒤 상태 변경 알림을 다시 보냅니다.</summary>
    public static void ResumeLoadedQuests(IQuestController controller)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller), "Quest 진행 데이터를 복원할 Controller가 필요합니다.");
        }

        int resumedCount = 0;
        foreach (QuestProgress progress in controller.QuestProgress.Values
                     .Where(item => item != null && item.state == QuestState.InProgress))
        {
            progress.EnsureCollections();
            QuestContainer template = QuestManager.Instance?.GetQuestTemplate(progress.questId);
            if (template == null)
            {
                Debug.LogWarning($"[Quest] 저장 데이터가 알 수 없는 Quest ID {progress.questId}를 참조합니다.");
                continue;
            }

            controller.InvokeStatusChanged(template, progress);
            resumedCount++;
        }

        Debug.Log($"[Quest] 진행 중인 Quest 기록 {resumedCount}개를 복원했습니다.");
    }
	}
}
