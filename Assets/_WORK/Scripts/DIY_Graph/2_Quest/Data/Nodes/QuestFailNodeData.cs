using System;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>현재 Quest를 실패 상태로 종료합니다.</summary>
	[Serializable]
	public class QuestFailNodeData : NodeBaseData
	{
		[Tooltip("게임에서 필요에 따라 표시하거나 기록할 실패 이유입니다.")]
		public string FailReason;
	}
}
