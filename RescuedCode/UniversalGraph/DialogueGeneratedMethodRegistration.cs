using System;
using System.ComponentModel;

namespace UniversalGraph
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed class DialogueGeneratedMethodRegistration
	{
		public DialogueMethodKind Kind { get; }

		public string Key { get; }

		public DialogueTarget Target { get; }

		public string DeclaringTypeMetadataName { get; }

		public string MethodMetadataName { get; }

		public bool IsStatic { get; }

		public DialogueGeneratedParameterRegistration[] Parameters { get; }

		public DialogueGeneratedMethodInvoker DirectInvoker { get; }

		public DialogueGeneratedMethodRegistration(DialogueMethodKind kind, string key, DialogueTarget target, string declaringTypeMetadataName, string methodMetadataName, bool isStatic, DialogueGeneratedParameterRegistration[] parameters, DialogueGeneratedMethodInvoker directInvoker)
		{
			Kind = kind;
			Key = key;
			Target = target;
			DeclaringTypeMetadataName = declaringTypeMetadataName;
			MethodMetadataName = methodMetadataName;
			IsStatic = isStatic;
			Parameters = parameters ?? Array.Empty<DialogueGeneratedParameterRegistration>();
			DirectInvoker = directInvoker;
		}
	}
}
