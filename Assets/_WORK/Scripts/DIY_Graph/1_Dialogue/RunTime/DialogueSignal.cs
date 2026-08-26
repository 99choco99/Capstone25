using System;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>Wait Signal 대화 노드가 사용하는 현재 프로세스 범위의 신호 통로입니다.</summary>
	public static class DialogueSignal
	{
		internal static event Action<string> Published;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			DialogueSignal.Published = null;
		}

		/// <summary>앞뒤 공백을 제거한 고정 키를 현재 대화 세션에 전달합니다.</summary>
		public static void Publish(string signalKey)
		{
			if (string.IsNullOrWhiteSpace(signalKey))
			{
				Debug.LogWarning("[Dialogue] 빈 Signal 키는 무시했습니다.");
			}
			else
			{
				Published?.Invoke(signalKey.Trim());
			}
		}
	}
}


