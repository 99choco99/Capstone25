using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalGraph
{
	/// <summary>UI와 게임 코드가 Quest 그래프를 직접 탐색하지 않고 필요한 데이터를 읽는 API입니다.</summary>
	public static class QuestQueries
	{
    /// <summary>등록된 Quest 그래프에서 대상과 일치하는 수락 후보를 원본 순서로 반환합니다.</summary>
    public static QuestOffer[] GetQuestOffers(IQuestController controller, string targetId)
    {
        return GetQuestOffers(controller, new[] { targetId });
    }

    /// <summary>여러 대상 ID와 일치하는 수락 후보를 한 번에 조회합니다.</summary>
    public static QuestOffer[] GetQuestOffers(
        IQuestController controller,
        IEnumerable<string> targetIds)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller), "Quest 후보를 조회할 Controller가 필요합니다.");
        }

        QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
        if (registry == null)
        {
            throw new InvalidOperationException(
                "Quest 후보를 조회하기 전에 QuestDefinitionRegistry.Initialize를 호출해야 합니다.");
        }

        return QuestInteractionQuery
            .GetQuestOffers(registry, controller, targetIds)
            .ToArray();
    }

    /// <summary>등록된 Quest 그래프에서 대상과 일치하는 모든 대화 후보를 반환합니다.</summary>
    public static DialogueCandidate[] GetDialogueCandidates(IQuestController controller, string targetId)
    {
        return GetDialogueCandidates(controller, new[] { targetId });
    }

    /// <summary>여러 대상 ID와 일치하는 대화 후보를 한 번에 조회합니다.</summary>
    public static DialogueCandidate[] GetDialogueCandidates(
        IQuestController controller,
        IEnumerable<string> targetIds)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller), "대화 후보를 조회할 Controller가 필요합니다.");
        }

        QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
        if (registry == null)
        {
            throw new InvalidOperationException(
                "대화 후보를 조회하기 전에 QuestDefinitionRegistry.Initialize를 호출해야 합니다.");
        }

        return QuestInteractionQuery
            .GetDialogueCandidates(registry, controller, targetIds)
            .ToArray();
    }

    /// <summary>진행 중이거나 완료 보고가 가능한 Quest를 반환합니다.</summary>
    public static QuestProgress[] GetActiveQuests(IQuestController controller)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller), "활성 Quest를 조회할 Controller가 필요합니다.");
        }

        return controller.QuestProgress.Values
            .Where(progress => progress != null
                               && (progress.state == QuestState.InProgress
                                   || progress.state == QuestState.CanComplete))
            .ToArray();
    }

    /// <summary>현재 활성화된 목표를 UI와 게임 코드가 직접 가공할 수 있는 데이터로 반환합니다.</summary>
    public static QuestObjectiveProgress[] GetCurrentObjectives(IQuestController controller, int questId)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller), "Quest 목표를 조회할 Controller가 필요합니다.");
        }

        controller.QuestProgress.TryGetValue(questId, out QuestProgress progress);
        QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
        if (progress == null
            || registry == null
            || !registry.TryGetQuestIndex(questId, out _, out QuestGraphIndex index))
        {
            return Array.Empty<QuestObjectiveProgress>();
        }

        progress.EnsureCollections();
        if (progress.activeNodeGuids.Count == 0)
        {
            return Array.Empty<QuestObjectiveProgress>();
        }

        var objectives = new List<QuestObjectiveProgress>();
        foreach (string guid in progress.activeNodeGuids)
        {
            if (!index.Nodes.TryGetValue(guid, out NodeBaseData node))
            {
                continue;
            }

            if (node is QuestObjectiveNodeData objective)
            {
                progress.nodeProgressCounts.TryGetValue(guid, out int count);
                objectives.Add(new QuestObjectiveProgress(questId, objective, count));
            }
        }

        return objectives.ToArray();
    }

	}
}
