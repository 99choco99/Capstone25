using UnityEngine;

namespace UniversalGraph
{
	internal static class DialogueBuiltInActions
	{
		[DialogueAction("dialogue.debug-log", Target = DialogueTarget.Global)]
		private static void Log(string message)
		{
			Debug.Log((object)("[Dialogue Graph] " + message));
		}

		[DialogueAction("dialogue.debug-values", Target = DialogueTarget.Global)]
		private static void LogValues(string message, int count, bool enabled, Object objectValue, DialogueContext context)
		{
			string text = ((objectValue != (object)null) ? (((UnityEngine.Object)objectValue).name + " (" + objectValue.GetType().Name + ")") : "null");
			string text2 = ((context?.Speaker != (object)null) ? context.Speaker.name : "null");
			string text3 = ((context?.Interactor != (object)null) ? context.Interactor.name : "null");
			Debug.Log((object)($"[Dialogue Graph] 메시지={message}, 횟수={count}, 활성화={enabled}, " + "오브젝트=" + text + ", 화자=" + text2 + ", 상호작용자=" + text3));
		}
	}
}



