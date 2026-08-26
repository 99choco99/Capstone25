using System;
using System.ComponentModel;

namespace UniversalGraph
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed class DialogueGeneratedMethodRegistration
	{
		public MethodKind Kind { get; }

		public string Key { get; }

		public DialogueTarget Target { get; }

		public string DeclaringTypeMetadataName { get; }

		public string MethodMetadataName { get; }

		public bool IsStatic { get; }

		public GeneratedParameterRegistration[] Parameters { get; }

		public GeneratedMethodInvoker DirectInvoker { get; }

		public DialogueGeneratedMethodRegistration(MethodKind kind, string key, DialogueTarget target, string declaringTypeMetadataName, string methodMetadataName, bool isStatic, GeneratedParameterRegistration[] parameters, GeneratedMethodInvoker directInvoker)
		{
			Kind = kind;
			Key = key;
			Target = target;
			DeclaringTypeMetadataName = declaringTypeMetadataName;
			MethodMetadataName = methodMetadataName;
			IsStatic = isStatic;
			Parameters = parameters ?? Array.Empty<GeneratedParameterRegistration>();
			DirectInvoker = directInvoker;
		}
	}
}
