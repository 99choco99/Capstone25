namespace UniversalGraph
{
	public class DialogueRequest
	{
		public DialogueReference Reference { get; }

		public string TopicName { get; }

		public int Priority { get; }

		public string SourceQuestId { get; }

		public DialogueRequest(DialogueReference reference, string topicName, int priority, string sourceQuestId = "")
		{
			Reference = reference;
			TopicName = topicName;
			Priority = priority;
			SourceQuestId = sourceQuestId;
		}
	}
}
