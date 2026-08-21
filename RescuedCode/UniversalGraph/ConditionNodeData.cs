using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UniversalGraph
{
	[Serializable]
	[MovedFrom(true, "UniversalGraph", "Assembly-CSharp", "ConditionNodeData")]
	public sealed class ConditionNodeData : NodeBaseData
	{
		public string ConditionEventKey;

		public string ConditionEventParam;

		public List<DialogueArgumentData> ConditionEventArguments = new List<DialogueArgumentData>();
	}
}
