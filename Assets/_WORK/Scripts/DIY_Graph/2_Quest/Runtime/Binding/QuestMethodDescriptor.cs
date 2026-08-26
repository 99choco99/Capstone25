using System;
using System.Reflection;

namespace UniversalGraph
{
    /// <summary>Quest 작성 화면과 런타임 호출에서 사용하는 검증된 메서드 정보입니다.</summary>
    public sealed class QuestMethodDescriptor : MethodDescriptor
    {
        internal QuestMethodDescriptor(
            string key,
            MethodKind kind,
            QuestMethodTarget target,
            MethodInfo method,
            MethodParameterDescriptor[] parameters,
            MethodParameterDescriptor[] serializedParameters)
            : this(
                key,
                kind,
                target,
                method?.DeclaringType,
                method?.Name,
                method?.IsStatic ?? false,
                method,
                parameters,
                serializedParameters,
                null)
        {
        }

        internal QuestMethodDescriptor(
            string key,
            MethodKind kind,
            QuestMethodTarget target,
            Type declaringType,
            string methodName,
            bool isStatic,
            MethodInfo method,
            MethodParameterDescriptor[] parameters,
            MethodParameterDescriptor[] serializedParameters,
            GeneratedMethodInvoker generatedInvoker)
            : base(
                key,
                kind,
                declaringType,
                methodName,
                isStatic,
                method,
                parameters,
                serializedParameters,
                generatedInvoker)
        {
            Target = target;
            DisplayName = $"{Key}  [{Target}]  {DeclaringType?.Name}.{MethodName}";
        }

        public QuestMethodTarget Target { get; }
    }
}
