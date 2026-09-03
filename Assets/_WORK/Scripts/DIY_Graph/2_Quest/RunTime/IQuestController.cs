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
		IDictionary<int, QuestProgress> QuestProgress { get; }

		/// <summary>Quest 상태 또는 목표 진행량이 바뀌었음을 게임에 알립니다.</summary>
		void InvokeStatusChanged(QuestContainer container, QuestProgress progress);
	}
}
