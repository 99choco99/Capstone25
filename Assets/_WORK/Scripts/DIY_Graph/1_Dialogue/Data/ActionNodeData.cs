using System;
using System.Collections.Generic;

namespace UniversalGraph
{
	[Serializable]
	public sealed class ActionNodeData : NodeBaseData
	{
		public string EventKey;

		public string EventParam;

		public List<DialogueArgumentData> EventArguments = new List<DialogueArgumentData>();
	}
}
