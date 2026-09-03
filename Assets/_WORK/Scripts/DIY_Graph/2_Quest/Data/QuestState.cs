namespace UniversalGraph
{
	/// <summary>Quest 그래프와 게임 연동 코드가 함께 사용하는 진행 단계입니다.</summary>
	public enum QuestState
	{
		NotStarted = 0,
		// 기존 저장 데이터와 그래프 에셋의 숫자를 유지하기 위해 1은 사용하지 않습니다.
		InProgress = 2,
		CanComplete = 3,
		TurnedIn = 4,
		Failed = 5
	}
}
