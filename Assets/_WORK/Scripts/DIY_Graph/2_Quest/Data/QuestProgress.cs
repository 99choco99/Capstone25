using System;
using System.Collections.Generic;

namespace UniversalGraph
{
	[Serializable]
	public class QuestProgress
	{
	/// <summary>어떤 퀘스트에 대한 것인지</summary>
	public int questId;

	/// <summary>현재 Quest 진행 단계</summary>
	public QuestState state;

	/// <summary>초기화·재시작 전의 실행이 새 진행 기록을 덮어쓰지 않도록 구분하는 런타임 번호입니다.</summary>
	[NonSerialized]
	internal int runVersion;

	/// <summary>외부 진행을 기다리며 현재 흐름을 막고 있는 목표 또는 하위 Quest 노드입니다.</summary>
	public List<string> activeNodeGuids = new();

	/// <summary>노드 GUID별 런타임 진행 수치</summary>
	public Dictionary<string, int> nodeProgressCounts = new();

	/// <summary>이미 실행한 일회성 흐름 노드. <para>
	/// </para>불러오기 후 보상이나 Action이 중복 실행되는 것을 막기 위함</summary>
	public List<string> completedNodeGuids = new();

	/// <summary>AND Gate가 소비한 중복 없는 입력 분기 도착 기록입니다.</summary>
	public List<string> completedGateInputs = new();

	public QuestProgress() { }

	public QuestProgress(QuestContainer data)
	{
		if (data == null)
		{
			throw new ArgumentNullException(nameof(data), "복원할 Quest 진행 데이터가 필요합니다.");
		}

		questId = data.QuestId;
		state = QuestState.NotStarted;
	}

	/// <summary>Serializer가 null로 만든 컬렉션 필드를 복구합니다.</summary>
	public void EnsureCollections()
	{
		activeNodeGuids ??= new List<string>();
		nodeProgressCounts ??= new Dictionary<string, int>();
		completedNodeGuids ??= new List<string>();
		completedGateInputs ??= new List<string>();
	}
	}
}
