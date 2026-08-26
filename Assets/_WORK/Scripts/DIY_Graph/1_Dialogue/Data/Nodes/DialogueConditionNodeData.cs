using System;

namespace UniversalGraph
{
	[Serializable]
	public sealed class DialogueConditionNodeData : NodeBaseData
	{
		public MethodCallData Condition = new();
	}
}
