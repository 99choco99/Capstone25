using UnityEngine;

namespace UniversalGraph
{
	/// <summary>
	/// Speaker와 Interactor Attribute 대상을 찾을 때 사용하는 대화 세션별 런타임 객체입니다.
	/// 게임 전용 기능은 이 객체들의 컴포넌트 또는 Action의 명시적 인수를 통해 접근합니다.
	/// </summary>
	public class DialogueContext
	{
		public GameObject Speaker { get; }

		public GameObject Interactor { get; }

		/// <summary>상호작용 한번에 하나씩 사용할 데이터 박스</summary>
		public DialogueContext(GameObject speaker, GameObject interactor)
		{
			Speaker = speaker;
			Interactor = interactor;
		}
	}
}
