using System;

namespace UniversalGraph
{
	[Serializable]
	public class QuestStateChangeNodeData : NodeBaseData
	{
		public string QuestId;

		public QuestState NewState;
	}
}
