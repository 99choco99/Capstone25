using System;

namespace UniversalGraph
{
	[Serializable]
	public sealed class DialogueNodeData : NodeBaseData
	{
		public string SpeakerName;

		public string DialogueText;

        //대화시 
		public MethodCallData Event = new();
	}
}
