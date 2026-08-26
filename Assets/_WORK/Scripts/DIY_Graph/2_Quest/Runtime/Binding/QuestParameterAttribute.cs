using System;
using UnityEngine.Scripting;

namespace UniversalGraph
{
    /// <summary>그래프에서 편집할 Quest 메서드 인수에 고정 직렬화 ID를 부여합니다.</summary>
    [RequireAttributeUsages]
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class QuestParameterAttribute : Attribute
    {
        public QuestParameterAttribute(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }
}
