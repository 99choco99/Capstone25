using System;

namespace UniversalGraph
{
	/// <summary>Attribute 메서드 인수 하나의 타입, 저장 ID와 Runtime 주입 방식을 설명합니다.</summary>
	public sealed class MethodParameterDescriptor
	{
		public int MethodIndex { get; }

		public string ParameterId { get; }

		public string DisplayName { get; }

		public Type ParameterType { get; }

		public MethodParameterSource Source { get; }

		public MethodArgumentKind Kind { get; }

		public string DeclaredTypeId { get; }

		internal MethodParameterDescriptor(int methodIndex, string parameterId, string displayName, Type parameterType, MethodParameterSource source, MethodArgumentKind kind, string declaredTypeId)
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
