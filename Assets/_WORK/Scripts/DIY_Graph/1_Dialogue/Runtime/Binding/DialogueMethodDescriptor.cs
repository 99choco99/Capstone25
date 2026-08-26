using System;
using System.Reflection;

namespace UniversalGraph
{
	public sealed class DialogueMethodDescriptor : MethodDescriptor
	{
		public DialogueTarget Target { get; }

		internal DialogueMethodDescriptor(string key, MethodKind kind, DialogueTarget target, MethodInfo method, MethodParameterDescriptor[] parameters, MethodParameterDescriptor[] serializedParameters, GeneratedMethodInvoker generatedInvoker = null)
			: this(key, kind, target, method?.DeclaringType, method?.Name, method?.IsStatic ?? false, method, parameters, serializedParameters, generatedInvoker)
		{
		}

		internal DialogueMethodDescriptor(string key, MethodKind kind, DialogueTarget target, Type declaringType, string methodName, bool isStatic, MethodInfo method, MethodParameterDescriptor[] parameters, MethodParameterDescriptor[] serializedParameters, GeneratedMethodInvoker generatedInvoker)
			: base(key, kind, declaringType, methodName, isStatic, method, parameters, serializedParameters, generatedInvoker)
		{
			Target = target;
			DisplayName = $"{Key}  [{Target}]  {DeclaringType?.Name}.{MethodName}";
		}
	}
}
