using System;

namespace UniversalGraph
{
	/// <summary>게임 코드와 UI가 그래프를 직접 탐색하지 않고 읽을 수 있는 현재 목표 정보입니다.</summary>
	public sealed class QuestObjectiveProgress
	{
		internal QuestObjectiveProgress(
			int questId,
			QuestObjectiveNodeData definition,
			int currentAmount)
		{
			QuestId = questId;
			NodeGuid = definition.Guid;
			ObjectiveType = definition.ObjectiveType;
			TargetId = definition.TargetId;
			TargetPrefab = definition.TargetPrefab;
			Description = definition.ObjectiveDescription;
			CurrentAmount = currentAmount;
			RequiredAmount = Math.Max(1, definition.RequiredAmount);
		}

		public int QuestId { get; }
		public string NodeGuid { get; }
		public string ObjectiveType { get; }
		public int TargetId { get; }
		public UnityEngine.Object TargetPrefab { get; }
		public string Description { get; }
		public int CurrentAmount { get; }
		public int RequiredAmount { get; }
	}
}
