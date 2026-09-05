using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Quest 노드에 지정된 Attribute 메서드 호출을 담당합니다.</summary>
    public static partial class QuestRunner
    {
        private static bool ExecuteAction(
            IQuestController controller,
            QuestContainer container,
            QuestProgress progress,
            NodeBaseData nodeData,
            MethodBindingData binding,
            string label)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.Key))
            {
                if (nodeData is QuestRewardNodeData)
                {
                    return true;
                }

                Debug.LogError($"[Quest] {label} 키가 비어 있습니다.", container);
                return false;
            }

            var executionContext = new QuestExecutionContext(controller, container, progress, nodeData);
            return QuestMethodInvoker.TryInvokeMethod(binding, executionContext, MethodKind.Action, out _);
        }
    }
}
