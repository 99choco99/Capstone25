using System;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>Dialogue 그래프의 이름 있는 시작점을 대화 후보로 제공하는 Quest 흐름 종착 노드입니다.</summary>
	[Serializable]
	public sealed class DialogueRequestNodeData : NodeBaseData
	{
		[Tooltip("이 경로가 제공할 Dialogue 그래프와 이름이 지정된 진입점입니다.")]
		public DialogueReference DialogueReference;

		[Tooltip("여러 대화 요청을 사용할 수 있을 때 표시할 주제 이름입니다.")]
		public string TopicName = "Default";

		[Tooltip("기본 대화 선택 정책은 값이 높은 요청을 먼저 선택합니다.")]
		public int Priority = 0;
	}
}
