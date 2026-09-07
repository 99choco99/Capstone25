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
            MethodParameterDescriptor[] parameters)
            : base(
                key,
                kind,
                method,
                parameters)
        {
            Target = target;
            DisplayName = $"{Key}  [{Target}]  {DeclaringType?.Name}.{MethodName}";
        }

        public QuestMethodTarget Target { get; }
    }
}
