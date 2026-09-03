using System;
using System.Reflection;

namespace UniversalGraph
{
	/// <summary>
	/// Dialogue에서 Attribute가 붙은 메소드의 정보의 정의를 담는 클래스(읽기전용)
	/// </summary>
	public sealed class DialogueMethodDescriptor : MethodDescriptor
	{
		public DialogueMethodOwner Owner { get; }

		/// <summary>
		/// Reflection으로 얻은 MethodInfo를 공통 생성자에 전달하는 편의 생성자
		/// </summary>
		internal DialogueMethodDescriptor(
			string key,
			MethodKind kind,
			DialogueMethodOwner owner,
			MethodInfo methodInfo,
			MethodParameterDescriptor[] parameters)
			: this(
				key,
				kind,
				owner,
				methodInfo?.DeclaringType,
				methodInfo?.Name,
				methodInfo?.IsStatic ?? false,
				methodInfo,
				parameters,
				null){ }

		/// <summary>
		/// Reflection과 Generator가 공통으로 사용하는 최종 생성자
		/// </summary>
		internal DialogueMethodDescriptor(
			string key,
			MethodKind kind,
			DialogueMethodOwner owner,
			Type declaringType,
			string methodName,
			bool isStatic,
			MethodInfo method,
			MethodParameterDescriptor[] parameters,
			GeneratedMethodInvoker generatedInvoker): base(
				  key,
				  kind,
				  declaringType,
				  methodName,
				  isStatic,
				  method,
				  parameters,
				  generatedInvoker)
		{
			Owner = owner;
			DisplayName = $"{Key}  [{Owner}]  {DeclaringType?.Name}.{MethodName}";
		}
	}
}
