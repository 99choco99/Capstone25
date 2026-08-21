using UnityEngine.Scripting;

namespace UniversalGraph.Generated
{
	[Preserve]
	public sealed class __DialogueGeneratedProvider_UniversalGraph_Runtime_027d6278 : IDialogueGeneratedMethodProvider
	{
		[Preserve]
		public void Collect(IDialogueGeneratedMethodSink sink)
		{
			sink.Add(new DialogueGeneratedMethodRegistration(DialogueMethodKind.Action, "dialogue.debug-log", DialogueTarget.Global, "UniversalGraph.DialogueBuiltInActions", "Log", isStatic: true, new DialogueGeneratedParameterRegistration[1]
			{
				new DialogueGeneratedParameterRegistration("message", "message", "System.String", "mscorlib")
			}, null));
			sink.Add(new DialogueGeneratedMethodRegistration(DialogueMethodKind.Action, "dialogue.debug-values", DialogueTarget.Global, "UniversalGraph.DialogueBuiltInActions", "LogValues", isStatic: true, new DialogueGeneratedParameterRegistration[5]
			{
				new DialogueGeneratedParameterRegistration("message", "message", "System.String", "mscorlib"),
				new DialogueGeneratedParameterRegistration("count", "count", "System.Int32", "mscorlib"),
				new DialogueGeneratedParameterRegistration("enabled", "enabled", "System.Boolean", "mscorlib"),
				new DialogueGeneratedParameterRegistration("objectValue", "objectValue", "UnityEngine.UnityEngine.Object", "UnityEngine.CoreModule"),
				new DialogueGeneratedParameterRegistration("context", "context", "UniversalGraph.DialogueContext", "UniversalGraph.Runtime")
			}, null));
		}
	}
}
