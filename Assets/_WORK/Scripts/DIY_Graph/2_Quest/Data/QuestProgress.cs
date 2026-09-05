using System;
using System.Collections.Generic;

namespace UniversalGraph
{
	[Serializable]
	public class QuestProgress
	{
	/// <summary>이 진행 기록이 가리키는 Quest 정의의 고정 ID입니다.</summary>
	public int questId;

	/// <summary>현재 Quest 진행 단계입니다.</summary>
	public QuestState state;

	/// <summary>초기화·재시작 전의 실행이 새 진행 기록을 덮어쓰지 않도록 구분하는 런타임 번호입니다.</summary>
	[NonSerialized]
	internal int runVersion;

	/// <summary>외부 진행을 기다리며 현재 흐름을 막고 있는 목표 또는 하위 Quest 노드입니다.</summary>
	public List<string> activeNodeGuids = new List<string>();

	/// <summary>노드 GUID별 런타임 진행 수치입니다. 저장 기능에서 이 Dictionary를 명시적으로 변환해야 합니다.</summary>
	public Dictionary<string, int> nodeProgressCounts = new Dictionary<string, int>();

	/// <summary>이미 실행한 일회성 흐름 노드입니다. 불러오기 후 보상이나 Action이 중복 실행되는 것을 막습니다.</summary>
	public List<string> completedNodeGuids = new List<string>();

	/// <summary>AND Gate가 소비한 중복 없는 입력 분기 도착 기록입니다.</summary>
	public List<string> completedGateInputs = new List<string>();

	public QuestProgress()
	{
	}

	public QuestProgress(QuestContainer data)
	{
		if (data == null)
		{
			throw new ArgumentNullException(nameof(data), "복원할 Quest 진행 데이터가 필요합니다.");
		}

		questId = data.QuestId;
		state = QuestState.NotStarted;
	}

	/// <summary>구형 저장 데이터나 Serializer가 null로 만든 컬렉션 필드를 복구합니다.</summary>
	public void EnsureCollections()
	{
		activeNodeGuids ??= new List<string>();
		nodeProgressCounts ??= new Dictionary<string, int>();
		completedNodeGuids ??= new List<string>();
		completedGateInputs ??= new List<string>();
	}
	}
}
