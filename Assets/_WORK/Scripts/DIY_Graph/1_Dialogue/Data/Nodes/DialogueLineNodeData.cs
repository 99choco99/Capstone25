using System;

namespace UniversalGraph
{
	[Serializable]
	public sealed class DialogueLineNodeData : NodeBaseData
	{
		public string SpeakerName;

		public string DialogueText;

        //대화시 
		public MethodBindingData EnterAction = new();
	}
}
