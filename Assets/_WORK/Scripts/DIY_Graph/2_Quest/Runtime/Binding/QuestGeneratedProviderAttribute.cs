using System;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace UniversalGraph
{
    /// <summary>Quest Source Generator가 어셈블리에 추가하는 표시 Attribute입니다.</summary>
    [RequireAttributeUsages]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class QuestGeneratedProviderAttribute : Attribute
    {
        public QuestGeneratedProviderAttribute(Type providerType)
        {
            ProviderType = providerType;
        }

        public Type ProviderType { get; }
    }
}
