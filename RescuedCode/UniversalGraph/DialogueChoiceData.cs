using System;
using System.Collections.Generic;

namespace UniversalGraph
{
	[Serializable]
	public class DialogueChoiceData
	{
		public string PortName;

		public string ChoiceText;

		public string ChoiceEventKey;

		public string ChoiceEventParam;

		public List<DialogueArgumentData> ChoiceEventArguments = new List<DialogueArgumentData>();
	}
}
