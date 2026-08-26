using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Quest 노드와 Attribute 또는 구형 게임 연결 API 사이의 호출을 담당합니다.</summary>
    public static partial class QuestRunner
    {
        private static bool TryEvaluateCustomCondition(
            IQuestController controller,
            QuestContainer container,
            QuestProgress progress,
            QuestConditionBranchNodeData condition,
            out bool result,
            out bool handlerFound)
        {
            var context = new QuestExecutionContext(controller, container, progress, condition);
            if (QuestEventRegistry.TryEvaluateCondition(
                    condition.Condition,
                    controller,
                    context,
                    out result,
                    out bool registered))
            {
                handlerFound = true;
                return true;
            }

            // 등록된 처리기가 실행에 실패했다면 구형 Resolver로 조용히 넘어가면 안 됩니다.
            if (registered)
            {
                handlerFound = true;
                return false;
            }

            handlerFound = controller is IQuestConditionResolver;
            return controller is IQuestConditionResolver resolver
                   && resolver.TryEvaluateCondition(condition, out result);
        }

        private static bool ExecuteAction(
            IQuestController controller,
            QuestContainer container,
            QuestProgress progress,
            QuestActionTriggerNodeData action)
        {
            var context = new QuestExecutionContext(controller, container, progress, action);
            if (QuestEventRegistry.TryExecuteAction(
                    action.Action,
                    controller,
                    context,
                    out bool registered))
            {
                return true;
            }

            // Attribute 등록부가 모르는 키에만 구형 연결 방식을 사용합니다.
            if (registered)
            {
                Debug.LogError($"[Quest] 등록된 Action '{action.Action.Key}' 실행에 실패했습니다.", container);
                return false;
            }

            if (controller is IQuestActionReceiver receiver && receiver.TryExecuteAction(action))
            {
                return true;
            }

            QuestEventManager.TriggerAction(action.Action.Key);
            return true;
        }

        /// <summary>게임 Controller가 완료 처리하기 직전에 선택적인 이식 가능 보상 Action을 실행합니다.</summary>
        private static bool ExecuteRewardAction(
            IQuestController controller,
            QuestContainer container,
            QuestProgress progress,
            QuestRewardNodeData reward)
        {
            if (string.IsNullOrWhiteSpace(reward.RewardAction.Key))
            {
                return true;
            }

            var context = new QuestExecutionContext(controller, container, progress, reward);
            if (QuestEventRegistry.TryExecuteAction(
                    reward.RewardAction,
                    controller,
                    context,
                    out bool registered))
            {
                return true;
            }

            if (registered)
            {
                Debug.LogError(
                    $"[Quest] 등록된 Reward Action '{reward.RewardAction.Key}' 실행에 실패했습니다.",
                    container);
                return false;
            }

            var legacyAction = new QuestActionTriggerNodeData
            {
                Guid = reward.Guid,
                Action = reward.RewardAction
            };
            if (controller is IQuestActionReceiver receiver && receiver.TryExecuteAction(legacyAction))
            {
                return true;
            }

            QuestEventManager.TriggerAction(reward.RewardAction.Key);
            return true;
        }
    }
}
