using System;
using UnityEngine;

namespace UniversalGraph
{
	[Serializable]
	public class QuestObjectiveNodeData : NodeBaseData
	{
		public string ObjectiveType;

		public int TargetId;

		[Tooltip("?ㅻ툕?앺듃瑜?吏곸젒 ?뚯뼱???볦쑝?몄슂. (TargetId瑜?紐곕씪???⑸땲??")]
		public Object TargetPrefab;

		public int RequiredAmount;

		public string ObjectiveDescription;
	}
}
