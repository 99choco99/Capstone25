namespace UniversalGraph
{
	/// <summary>Quest 또는 게임 흐름에서 선택 후보로 만든 대화 요청 하나입니다.</summary>
	public class DialogueRequest
	{
		public DialogueReference Reference { get; }

		public string TopicName { get; }

		public int Priority { get; }

		public string SourceQuestId { get; }

		/// <summary>UI 주제명과 우선순위, 선택적인 출처 식별자를 가진 대화 후보를 만듭니다.</summary>
		public DialogueRequest(DialogueReference reference, string topicName, int priority, string sourceQuestId = "")
		{
			Reference = reference;
			TopicName = string.IsNullOrWhiteSpace(topicName) ? "Default" : topicName.Trim();
			Priority = priority;
			SourceQuestId = sourceQuestId ?? string.Empty;
		}
	}
}
