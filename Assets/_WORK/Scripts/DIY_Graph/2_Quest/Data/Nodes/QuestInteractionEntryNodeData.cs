using System;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>
    /// Interaction 시 퀘스트의 진행 여부를 제안 하는 노드의 데이터
	/// </summary>
	[Serializable]
	public sealed class QuestInteractionEntryNodeData : NodeBaseData
	{
		[SerializeField]
		private string targetId = string.Empty;

		public string TargetId
		{
			get => targetId?.Trim() ?? string.Empty;
			set => targetId = value?.Trim() ?? string.Empty;
		}
	}
}
