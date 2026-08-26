using System;
using UnityEngine.Scripting;

namespace UniversalGraph
{
    /// <summary>void 메서드를 그래프에서 선택할 수 있는 Quest Action으로 공개합니다.</summary>
    [RequireAttributeUsages]
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class QuestActionAttribute : PreserveAttribute
    {
        public QuestActionAttribute(string key)
        {
            Key = key;
        }

        public string Key { get; }
        public QuestMethodTarget Target { get; set; } = QuestMethodTarget.Controller;
    }
}
