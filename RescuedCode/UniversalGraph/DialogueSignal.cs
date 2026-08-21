using System;
using UnityEngine;

namespace UniversalGraph
{
	public static class DialogueSignal
	{
		internal static event Action<string> Published;

		[RuntimeInitializeOnLoadMethod]
		private static void ResetStaticState()
		{
			DialogueSignal.Published = null;
		}

		public static void Publish(string signalKey)
		{
			if (string.IsNullOrWhiteSpace(signalKey))
			{
				Debug.LogWarning((object)"[Dialogue] 鍮꾩뼱 ?덈뒗 Signal Key??諛쒗뻾?????놁뒿?덈떎.");
			}
			else
			{
				DialogueSignal.Published?.Invoke(signalKey.Trim());
			}
		}
	}
}
