using System;
using UnityEngine;

namespace UniversalGraph
{
	[Serializable]
	public sealed class QuestStateConditionNodeData : NodeBaseData
	{
		[Tooltip("?뺤씤???섏뒪??ID")]
		public int QuestId;

		[Tooltip("?쇱튂?섎뒗吏\u0080 ?뺤씤???섏뒪???곹깭")]
		public QuestState TargetState;
	}
}
