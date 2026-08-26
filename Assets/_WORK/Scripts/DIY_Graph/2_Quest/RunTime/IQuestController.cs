using System.Collections.Generic;

namespace UniversalGraph
{
	/// <summary>
	/// 이식 가능한 Quest Runner가 특정 Player, 인벤토리, 저장 시스템이나 UI에 의존하지 않고
	/// 플레이어별 Quest 상태를 읽고 바꿀 수 있도록 게임 프로젝트가 구현하는 연결 규약입니다.
	/// </summary>
	public interface IQuestController
	{
		/// <summary>고정 Quest ID로 찾을 수 있는 변경 가능한 진행 기록입니다.</summary>
		Dictionary<int, QuestProgress> QuestProgress { get; }

		/// <summary>Quest 하나의 진행 기록을 반환하며, 알 수 없는 ID이면 null을 반환합니다.</summary>
		QuestProgress GetQuestStatus(int questId);

		/// <summary>Quest 상태 또는 목표 진행량이 바뀌었음을 게임에 알립니다.</summary>
		void InvokeStatusChanged(QuestContainer container, QuestProgress progress);

		/// <summary>게임이 프로젝트 전용 보상을 지급하고 완료 가능한 Quest를 최종 처리하게 합니다.</summary>
		void TurnInQuest(int questId);
	}

	/// <summary>
	/// 프로젝트 전용 Condition 노드를 처리하는 선택적 호환 연결 규약입니다.
	/// 구현하지 않아도 내장 노드만 사용하는 Quest는 실행할 수 있습니다.
	/// </summary>
	public interface IQuestConditionResolver
	{
		/// <summary>프로젝트 전용 Condition을 평가하며, 지원하지 않는 키이면 false를 반환합니다.</summary>
		bool TryEvaluateCondition(QuestConditionBranchNodeData condition, out bool result);
	}

	/// <summary>전역 Action 이벤트를 사용하지 않고 Quest Action 노드를 처리하는 선택적 호환 연결 규약입니다.</summary>
	public interface IQuestActionReceiver
	{
		/// <summary>프로젝트 전용 Action을 실행하며, 지원하지 않는 Action이면 false를 반환합니다.</summary>
		bool TryExecuteAction(QuestActionTriggerNodeData action);
	}
}
