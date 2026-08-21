using System;

namespace UniversalGraph
{
	public sealed class DialogueParameterDescriptor
	{
		public int MethodIndex { get; }

		public string ParameterId { get; }

		public string DisplayName { get; }

		public Type ParameterType { get; }

		public DialogueParameterSource Source { get; }

		public DialogueArgumentKind Kind { get; }

		public string DeclaredTypeId { get; }

		internal DialogueParameterDescriptor(int methodIndex, string parameterId, string displayName, Type parameterType, DialogueParameterSource source, DialogueArgumentKind kind, string declaredTypeId)
		{
			MethodIndex = methodIndex;
			ParameterId = parameterId;
			DisplayName = displayName;
			ParameterType = parameterType;
			Source = source;
			Kind = kind;
			DeclaredTypeId = declaredTypeId;
		}
	}
}
