using System;
using UnityEngine;

namespace UniversalGraph
{
	[Serializable]
	public sealed class DialogueRequestNodeData : NodeBaseData
	{
		[Tooltip("DialogueReference")]
		public DialogueReference DialogueReference;

		[Tooltip("?щ윭 ?\u0080?붽? 寃뱀튌 ??UI???쒖떆???좏깮吏\u0080 二쇱젣 ?대쫫")]
		public string TopicName = "Default";

		[Tooltip("?곗꽑?쒖쐞 (?レ옄媛\u0080 ?믪쓣?섎줉 ?곗꽑沅?媛\u0080吏?")]
		public int Priority = 0;
	}
}
