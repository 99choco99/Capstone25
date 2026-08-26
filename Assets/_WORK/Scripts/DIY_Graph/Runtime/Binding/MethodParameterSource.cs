namespace UniversalGraph
{
	/// <summary>메서드 인수가 그래프 저장값인지 Runtime 실행 문맥에서 주입되는 값인지 구분합니다.</summary>
	public enum MethodParameterSource
	{
		Serialized,
		DialogueContext,
		QuestContext
	}
}
