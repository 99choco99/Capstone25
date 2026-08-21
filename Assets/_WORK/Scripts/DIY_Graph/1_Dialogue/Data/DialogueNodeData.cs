using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UniversalGraph
{
	[Serializable]
	[MovedFrom(true, "UniversalGraph", "Assembly-CSharp", "DialogueNodeData")]
	public sealed class DialogueNodeData : NodeBaseData
	{
		public string SpeakerName;

		public string DialogueText;

		public string EventKey;

		public string EventParam;

		public List<DialogueArgumentData> EventArguments = new List<DialogueArgumentData>();

		public List<DialogueChoiceData> Choices = new List<DialogueChoiceData>();
	}
}
