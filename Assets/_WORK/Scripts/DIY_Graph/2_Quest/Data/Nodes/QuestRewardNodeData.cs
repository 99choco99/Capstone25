using System;

namespace UniversalGraph
{
	[Serializable]
	public class QuestRewardNodeData : NodeBaseData
	{
		/// <summary>Quest 완료 처리 직전에 실행할 선택적인 Attribute 기반 보상 Action입니다.</summary>
		/// <summary>선택적 보상 Action에 전달할 타입 기반 인수입니다.</summary>
		public MethodCallData RewardAction = new();
	}
}
