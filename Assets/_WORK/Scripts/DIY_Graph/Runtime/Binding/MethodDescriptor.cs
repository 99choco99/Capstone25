using System;
using System.Collections.Generic;
using System.Reflection;

namespace UniversalGraph
{
    /// <summary>Dialogue와 Quest Attribute 메서드가 공유하는 호출 정보를 보관합니다.</summary>
    public abstract class MethodDescriptor
    {
        protected MethodDescriptor(
            string key,
            MethodKind kind,
            Type declaringType,
            string methodName,
            bool isStatic,
            MethodInfo method,
            MethodParameterDescriptor[] parameters,
            MethodParameterDescriptor[] serializedParameters,
            GeneratedMethodInvoker generatedInvoker)
        {
            Key = key;
            Kind = kind;
            DeclaringType = declaringType;
            MethodName = methodName;
            IsStatic = isStatic;
            Method = method;
            Parameters = parameters ?? Array.Empty<MethodParameterDescriptor>();
            SerializedParameters = serializedParameters ?? Array.Empty<MethodParameterDescriptor>();
            GeneratedInvoker = generatedInvoker;
            DisplayName = $"{Key}  {DeclaringType?.Name}.{MethodName}";
        }

        public string Key { get; }
        public MethodKind Kind { get; }
        public Type DeclaringType { get; }
        public string MethodName { get; }
        public bool IsStatic { get; }
        public MethodInfo Method { get; }
        public IReadOnlyList<MethodParameterDescriptor> Parameters { get; }
        public IReadOnlyList<MethodParameterDescriptor> SerializedParameters { get; }
        public string QualifiedMethodName => $"{DeclaringType?.FullName}.{MethodName}";
        public string DisplayName { get; protected set; }

        internal GeneratedMethodInvoker GeneratedInvoker { get; }
    }
}
