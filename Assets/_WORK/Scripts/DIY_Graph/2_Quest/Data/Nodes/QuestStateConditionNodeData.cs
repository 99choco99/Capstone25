using System;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>다른 Quest의 현재 상태에 따라 진행 또는 상호작용 조회 흐름을 분기합니다.</summary>
	[Serializable]
	public sealed class QuestStateConditionNodeData : NodeBaseData
	{
		[Tooltip("상태를 검사할 Quest의 고정 ID입니다.")]
		public int QuestId;

		[Tooltip("True 출력으로 진행할 Quest 상태입니다.")]
		public QuestState TargetState;
	}
}
