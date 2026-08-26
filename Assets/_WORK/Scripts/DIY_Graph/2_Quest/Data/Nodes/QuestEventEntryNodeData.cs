using System;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>Quest 진행을 시작하는 명시적인 시작점입니다.</summary>
	[Serializable]
	public sealed class QuestStartNodeData : NodeBaseData
	{
	}

	/// <summary>
	/// NPC 상호작용 대화를 찾을 때 사용하는 시작점입니다. 대화 경로 탐색이 실수로 Quest 진행을
	/// 시작하지 않도록 <see cref="QuestStartNodeData"/>와 분리되어 있습니다.
	/// </summary>
	[Serializable]
	public sealed class QuestEventEntryNodeData : NodeBaseData
	{
		[Tooltip("프로젝트에서 정의한 상호작용 대상 ID입니다. 모든 대상과 일치시키려면 비워 두세요.")]
		public string TargetId = "";
	}
}
