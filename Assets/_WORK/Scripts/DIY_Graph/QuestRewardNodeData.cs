using System;

namespace UniversalGraph
{
	[Serializable]
	public class QuestRewardNodeData : NodeBaseData
	{
		public bool UseDefaultReward = true;

		public int BonusExp;

		public int BonusGold;
	}
}
