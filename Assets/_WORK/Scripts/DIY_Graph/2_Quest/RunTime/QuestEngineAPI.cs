using System.Linq;
using System.Text;
using UnityEngine;
using UniversalGraph;

public static class QuestEngineAPI
{
	public static QuestProgress[] GetActiveQuests(IQuestController controller)
	{
		return controller.QuestProgress.Values.Where((QuestProgress p) => p.state == QuestState.InProgress || p.state == QuestState.CanComplete).ToArray();
	}

	public static string GetQuestName(int questId)
	{
		QuestContainer questTemplate = QuestManager.Instance.GetQuestTemplate(questId);
		return ((object)questTemplate != (object)null) ? questTemplate.questName : "Unknown Quest";
	}

	public static string GetQuestDescription(int questId)
	{
		QuestContainer questTemplate = QuestManager.Instance.GetQuestTemplate(questId);
		return ((object)questTemplate != (object)null) ? questTemplate.description : "";
	}

	public static string GetCurrentObjectiveText(IQuestController controller, int questId)
	{
		QuestProgress questStatus = controller.GetQuestStatus(questId);
		if (questStatus == null || questStatus.activeNodeGuids.Count == 0)
		{
			return "紐⑺몴 ?놁쓬";
		}
		QuestContainer questTemplate = QuestManager.Instance.GetQuestTemplate(questId);
		if ((object)questTemplate == (object)null)
		{
			return "紐⑺몴 ?놁쓬";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string guid in questStatus.activeNodeGuids)
		{
			NodeBaseData nodeBaseData = questTemplate.Nodes.FirstOrDefault((NodeBaseData n) => n.Guid == guid);
			if (nodeBaseData is QuestObjectiveNodeData questObjectiveNodeData)
			{
				int num = (questStatus.nodeProgressCounts.ContainsKey(guid) ? questStatus.nodeProgressCounts[guid] : 0);
				if (!string.IsNullOrEmpty(questObjectiveNodeData.ObjectiveDescription))
				{
					stringBuilder.AppendLine($"- {questObjectiveNodeData.ObjectiveDescription} ({num}/{questObjectiveNodeData.RequiredAmount})");
				}
				else
				{
					stringBuilder.AppendLine($"- {questObjectiveNodeData.ObjectiveType} ({num}/{questObjectiveNodeData.RequiredAmount})");
				}
			}
			else if (nodeBaseData is QuestRewardNodeData)
			{
				stringBuilder.AppendLine("- 蹂댁긽 ?섎졊 媛\u0080??");
			}
		}
		return (stringBuilder.Length > 0) ? stringBuilder.ToString().TrimEnd() : "吏꾪뻾 以?..";
	}

	public static void ResumeLoadedQuests(IQuestController controller)
	{
		int num = 0;
		foreach (QuestProgress item in controller.QuestProgress.Values.Where((QuestProgress p) => p.state == QuestState.InProgress))
		{
			if (item.activeNodeGuids.Count > 0)
			{
				controller.InvokeStatusChanged(QuestManager.Instance.GetQuestTemplate(item.questId), item);
				num++;
			}
		}
		Debug.Log((object)$"[QuestEngineAPI] {num}媛쒖쓽 ?섏뒪??吏꾪뻾 ?곹깭瑜?蹂듦뎄 諛??ш??숉뻽?듬땲??");
	}
}


