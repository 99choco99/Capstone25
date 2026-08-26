using System;
using UnityEngine;

namespace UniversalGraph
{
	[Serializable]
	public sealed class DialogueStartNodeData : NodeBaseData
	{
		public const string DefaultEntryId = "Entry";

		[SerializeField]
		private string entryId = DefaultEntryId;

		/// <summary>StartNode의 id</summary>
		public string EntryId
		{
			get => string.IsNullOrWhiteSpace(entryId) ? DefaultEntryId : entryId.Trim();
			set => entryId = string.IsNullOrWhiteSpace(value) ? DefaultEntryId : value.Trim();
		}
	}
}
