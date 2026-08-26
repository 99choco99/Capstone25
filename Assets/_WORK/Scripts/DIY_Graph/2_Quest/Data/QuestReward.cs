using System;

namespace UniversalGraph
{
	/// <summary>게임의 Quest Controller가 해석하여 지급하는 기본 보상 정보입니다.</summary>
	[Serializable]
	public class QuestReward
	{
		public int exp;

		public int itemId;

		public int amount;

		public int gold;
	}
}
