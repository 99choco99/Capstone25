using System;
using UnityEngine;

namespace UniversalGraph
{
	[Serializable]
	public struct DialogueReference
	{
		[Tooltip("?\u0080??洹몃옒???먯뀑")]
		public DialogueContainer GraphAsset;

		[Tooltip("?대떦 洹몃옒???댁쓽 ?뱀젙 吏꾩엯??ID (湲곕낯媛? Default)")]
		public string EntryId;

		public DialogueReference(DialogueContainer graphAsset, string entryId)
		{
			GraphAsset = graphAsset;
			EntryId = (string.IsNullOrEmpty(entryId) ? "Default" : entryId);
		}
	}
}
