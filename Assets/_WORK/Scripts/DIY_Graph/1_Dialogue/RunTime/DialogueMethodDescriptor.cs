using System;
using System.Collections.Generic;
using System.Reflection;

namespace UniversalGraph
{
	public sealed class DialogueMethodDescriptor
	{
		public string Key { get; }

		public DialogueMethodKind Kind { get; }

		public DialogueTarget Target { get; }

		public Type DeclaringType { get; }

		public string MethodName { get; }

		public bool IsStatic { get; }

		public MethodInfo Method { get; }

		public IReadOnlyList<DialogueParameterDescriptor> Parameters { get; }

		public IReadOnlyList<DialogueParameterDescriptor> SerializedParameters { get; }

		internal DialogueGeneratedMethodInvoker GeneratedInvoker { get; set; }

		public string QualifiedMethodName => DeclaringType?.FullName + "." + MethodName;

		public string DisplayName => $"{Key}  [{Target}]  {DeclaringType?.Name}.{MethodName}";

		internal DialogueMethodDescriptor(string key, DialogueMethodKind kind, DialogueTarget target, MethodInfo method, DialogueParameterDescriptor[] parameters, DialogueParameterDescriptor[] serializedParameters, DialogueGeneratedMethodInvoker generatedInvoker = null)
			: this(key, kind, target, method?.DeclaringType, method?.Name, method?.IsStatic ?? false, method, parameters, serializedParameters, generatedInvoker)
		{
		}

		internal DialogueMethodDescriptor(string key, DialogueMethodKind kind, DialogueTarget target, Type declaringType, string methodName, bool isStatic, MethodInfo method, DialogueParameterDescriptor[] parameters, DialogueParameterDescriptor[] serializedParameters, DialogueGeneratedMethodInvoker generatedInvoker)
		{
			Key = key;
			Kind = kind;
			Target = target;
			DeclaringType = declaringType;
			MethodName = methodName;
			IsStatic = isStatic;
			Method = method;
			Parameters = parameters;
			SerializedParameters = serializedParameters;
			GeneratedInvoker = generatedInvoker;
		}
	}
}
