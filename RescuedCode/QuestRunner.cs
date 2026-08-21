using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniversalGraph;

public static class QuestRunner
{
	public static void ProcessEvent(IQuestController controller, string type, int targetId, int amount)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		foreach (QuestProgress item in controller.QuestProgress.Values.Where((QuestProgress p) => p.state == QuestState.InProgress))
		{
			QuestContainer questTemplate = QuestManager.Instance.GetQuestTemplate(item.questId);
			if ((Object)questTemplate == (Object)null || item.activeNodeGuids.Count == 0)
			{
				continue;
			}
			foreach (string activeGuid in item.activeNodeGuids.ToList())
			{
				NodeBaseData nodeBaseData = questTemplate.Nodes.FirstOrDefault((NodeBaseData n) => n.Guid == activeGuid);
				if (nodeBaseData is QuestObjectiveNodeData questObjectiveNodeData && string.Equals(questObjectiveNodeData.ObjectiveType, type, StringComparison.OrdinalIgnoreCase) && questObjectiveNodeData.TargetId == targetId)
				{
					if (!item.nodeProgressCounts.ContainsKey(activeGuid))
					{
						item.nodeProgressCounts[activeGuid] = 0;
					}
					item.nodeProgressCounts[activeGuid] += amount;
					if (item.nodeProgressCounts[activeGuid] >= questObjectiveNodeData.RequiredAmount)
					{
						item.activeNodeGuids.Remove(activeGuid);
						AdvanceToNextNodes(controller, questTemplate, item, questObjectiveNodeData);
					}
					else
					{
						controller.InvokeStatusChanged(questTemplate, item);
					}
				}
			}
		}
	}

	public static void StartQuestGraph(IQuestController controller, int questId)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		QuestProgress questStatus = controller.GetQuestStatus(questId);
		QuestContainer questTemplate = QuestManager.Instance.GetQuestTemplate(questId);
		if (questStatus == null || (Object)questTemplate == (Object)null)
		{
			return;
		}
		questStatus.state = QuestState.InProgress;
		questStatus.activeNodeGuids.Clear();
		questStatus.nodeProgressCounts.Clear();
		NodeBaseData nodeBaseData = questTemplate.Nodes.FirstOrDefault((NodeBaseData n) => n is QuestEventEntryNodeData);
		if (nodeBaseData != null)
		{
			AdvanceToNextNodes(controller, questTemplate, questStatus, nodeBaseData);
			return;
		}
		NodeBaseData nodeBaseData2 = questTemplate.Nodes.FirstOrDefault((NodeBaseData n) => n is QuestObjectiveNodeData);
		if (nodeBaseData2 != null)
		{
			questStatus.activeNodeGuids.Add(nodeBaseData2.Guid);
			controller.InvokeStatusChanged(questTemplate, questStatus);
		}
	}

	private static void AdvanceToNextNodes(IQuestController controller, QuestContainer container, QuestProgress progress, NodeBaseData currentNode)
	{
		List<NodeLinkData> list = container.NodeLinks.Where((NodeLinkData l) => l.BaseNodeGuid == currentNode.Guid).ToList();
		if (list.Count == 0)
		{
			return;
		}
		foreach (NodeLinkData link in list)
		{
			if (currentNode is QuestConditionBranchNodeData branch)
			{
				string text = (EvaluateCondition(controller, branch) ? "True" : "False");
				if (link.PortName != text)
				{
					continue;
				}
			}
			NodeBaseData nodeBaseData = container.Nodes.FirstOrDefault((NodeBaseData n) => n.Guid == link.TargetNodeGuid);
			if (nodeBaseData != null)
			{
				ProcessNode(controller, container, progress, nodeBaseData);
			}
		}
	}

	private static void ProcessNode(IQuestController controller, QuestContainer container, QuestProgress progress, NodeBaseData node)
	{
		if (node is QuestAndGateNodeData questAndGateNodeData)
		{
			if (!progress.nodeProgressCounts.ContainsKey(questAndGateNodeData.Guid))
			{
				progress.nodeProgressCounts[questAndGateNodeData.Guid] = 0;
			}
			progress.nodeProgressCounts[questAndGateNodeData.Guid]++;
			if (progress.nodeProgressCounts[questAndGateNodeData.Guid] >= questAndGateNodeData.RequiredInputCount)
			{
				AdvanceToNextNodes(controller, container, progress, questAndGateNodeData);
			}
		}
		else if (node is QuestStateChangeNodeData questStateChangeNodeData)
		{
			progress.state = questStateChangeNodeData.NewState;
			controller.InvokeStatusChanged(container, progress);
			AdvanceToNextNodes(controller, container, progress, node);
		}
		else if (node is QuestActionTriggerNodeData questActionTriggerNodeData)
		{
			QuestEventManager.TriggerAction(questActionTriggerNodeData.ActionId);
			AdvanceToNextNodes(controller, container, progress, node);
		}
		else if (node is QuestFailNodeData)
		{
			progress.state = QuestState.Failed;
			progress.activeNodeGuids.Clear();
			controller.InvokeStatusChanged(container, progress);
		}
		else if (node is QuestRewardNodeData)
		{
			controller.TurnInQuest(progress.questId);
			AdvanceToNextNodes(controller, container, progress, node);
		}
		else if (node is QuestSubGraphNodeData questSubGraphNodeData)
		{
			if (!progress.activeNodeGuids.Contains(node.Guid))
			{
				progress.activeNodeGuids.Add(node.Guid);
				StartQuestGraph(controller, questSubGraphNodeData.SubQuestId);
			}
			controller.InvokeStatusChanged(container, progress);
		}
		else if (node is QuestObjectiveNodeData)
		{
			if (!progress.activeNodeGuids.Contains(node.Guid))
			{
				progress.activeNodeGuids.Add(node.Guid);
			}
			controller.InvokeStatusChanged(container, progress);
		}
	}

	private static bool EvaluateCondition(IQuestController controller, QuestConditionBranchNodeData branch)
	{
		return true;
	}

	public static void NotifyQuestCompleted(IQuestController controller, int completedQuestId)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		foreach (QuestProgress item in controller.QuestProgress.Values.Where((QuestProgress p) => p.state == QuestState.InProgress))
		{
			QuestContainer questTemplate = QuestManager.Instance.GetQuestTemplate(item.questId);
			if ((Object)questTemplate == (Object)null || item.activeNodeGuids.Count == 0)
			{
				continue;
			}
			foreach (string activeGuid in item.activeNodeGuids.ToList())
			{
				NodeBaseData nodeBaseData = questTemplate.Nodes.FirstOrDefault((NodeBaseData n) => n.Guid == activeGuid);
				if (nodeBaseData is QuestSubGraphNodeData questSubGraphNodeData && questSubGraphNodeData.SubQuestId == completedQuestId)
				{
					item.activeNodeGuids.Remove(activeGuid);
					AdvanceToNextNodes(controller, questTemplate, item, questSubGraphNodeData);
				}
			}
		}
	}
}
