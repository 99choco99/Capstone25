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
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Expected O, but got Unknown
			string text = ((objectValue != (Object)null) ? (objectValue.name + " (" + ((object)objectValue).GetType().Name + ")") : "null");
			string text2 = (((Object)(context?.Speaker) != (Object)null) ? ((Object)context.Speaker).name : "null");
			string text3 = (((Object)(context?.Interactor) != (Object)null) ? ((Object)context.Interactor).name : "null");
			Debug.Log((object)($"[Dialogue Graph] message={message}, count={count}, enabled={enabled}, " + "object=" + text + ", speaker=" + text2 + ", interactor=" + text3));
		}
	}
}
