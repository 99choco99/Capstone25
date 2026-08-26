using System;
using System.ComponentModel;

namespace UniversalGraph
{
    /// <summary>Quest 메서드 하나에 대해 Generator가 만든 Reflection 없는 메타데이터와 직접 호출자입니다.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class QuestGeneratedMethodRegistration
    {
        public QuestGeneratedMethodRegistration(
            MethodKind kind,
            string key,
            QuestMethodTarget target,
            string declaringTypeMetadataName,
            string methodMetadataName,
            bool isStatic,
            GeneratedParameterRegistration[] parameters,
            GeneratedMethodInvoker directInvoker)
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

        public MethodKind Kind { get; }
        public string Key { get; }
        public QuestMethodTarget Target { get; }
        public string DeclaringTypeMetadataName { get; }
        public string MethodMetadataName { get; }
        public bool IsStatic { get; }
        public GeneratedParameterRegistration[] Parameters { get; }
        public GeneratedMethodInvoker DirectInvoker { get; }
    }
}
