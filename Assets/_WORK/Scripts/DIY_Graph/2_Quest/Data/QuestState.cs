namespace UniversalGraph
{
	/// <summary>Quest 그래프와 게임 연동 코드가 함께 사용하는 진행 단계입니다.</summary>
	public enum QuestState
	{
		Locked,
		Ready,
		InProgress,
		CanComplete,
		TurnedIn,
		Failed
	}
}
