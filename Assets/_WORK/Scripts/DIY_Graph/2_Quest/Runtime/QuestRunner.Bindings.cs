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
            MethodCallData methodCall,
            string label)
        {
            if (methodCall == null || string.IsNullOrWhiteSpace(methodCall.Key))
            {
                if (nodeData is QuestRewardNodeData)
                {
                    return true;
                }

                Debug.LogError($"[Quest] {label} 키가 비어 있습니다.", container);
                return false;
            }

            var executionContext = new QuestExecutionContext(controller, container, progress, nodeData);
            if (QuestMethodInvoker.TryExecuteAction(
                    methodCall,
                    controller,
                    executionContext,
                    out bool registered))
            {
                return true;
            }

            if (registered)
            {
                Debug.LogError($"[Quest] 등록된 {label} '{methodCall.Key}' 실행에 실패했습니다.", container);
                return false;
            }

            Debug.LogError($"[Quest] {label} '{methodCall.Key}'이 등록되지 않았습니다.", container);
            return false;
        }
    }
}
