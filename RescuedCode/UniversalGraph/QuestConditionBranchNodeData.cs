using System;
using UnityEngine;

namespace UniversalGraph
{
	[Serializable]
	public class QuestConditionBranchNodeData : NodeBaseData
	{
		[Tooltip("寃\u0080?ы븷 議곌굔???\u0080??(?? Level, Gold, Item)")]
		public string ConditionType;

		public int TargetId;

		public int RequiredValue;
	}
}
