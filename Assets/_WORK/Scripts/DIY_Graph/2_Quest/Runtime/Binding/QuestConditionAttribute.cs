using System;
using UnityEngine.Scripting;

namespace UniversalGraph
{
    /// <summary>
    /// bool 반환 메서드를 그래프에서 선택할 수 있는 Quest Condition으로 공개합니다.
    /// 대화·수락 후보 조회 중에도 호출될 수 있으므로 게임 상태를 바꾸지 않는 순수한 판정이어야 합니다.
    /// </summary>
    [RequireAttributeUsages]
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class QuestConditionAttribute : PreserveAttribute
    {
        public QuestConditionAttribute(string key)
        {
            Key = key;
        }

        public string Key { get; }
        public QuestMethodTarget Target { get; set; } = QuestMethodTarget.Controller;
    }
}
