using System;
using UnityEngine.Scripting.APIUpdating;

namespace UniversalGraph
{
	[Serializable]
	[MovedFrom(true, "UniversalGraph", "Assembly-CSharp", "StartNodeData")]
	public sealed class StartNodeData : NodeBaseData
	{
		public const string DefaultEntryId = "Default";

		public string EntryId = "Default";

		public string GetNormalizedEntryId()
		{
			return NormalizeEntryId(EntryId);
		}

		public static string NormalizeEntryId(string entryId)
		{
			return string.IsNullOrWhiteSpace(entryId) ? "Default" : entryId.Trim();
		}
	}
}
