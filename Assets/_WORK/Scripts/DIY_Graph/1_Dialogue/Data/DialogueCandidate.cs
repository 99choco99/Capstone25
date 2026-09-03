namespace UniversalGraph
{
    /// <summary>현재 조건에서 선택해 시작할 수 있는 대화 그래프 후보 하나</summary>
    public class DialogueCandidate
    {
		public DialogueEntryPoint EntryPoint { get; }

		public string DisplayName { get; }

		public int Priority { get; }

		/// <summary>UI 표시 이름과 선택 우선순위를 가진 대화 후보를 만듭니다.</summary>
		public DialogueCandidate(DialogueEntryPoint entryPoint, string displayName, int priority)
		{
			EntryPoint = entryPoint;
			DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Default" : displayName.Trim();
			Priority = priority;
		}
	}
}
