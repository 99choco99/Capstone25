using System;
using System.Collections.Generic;

namespace UniversalGraph
{
    /// <summary>Dialogue 노드 종류별 실행과 선택지 평가를 담당합니다.</summary>
    public sealed partial class DialogueManager
    {
        /// <summary>대기 노드나 종료 노드에 도달할 때까지 즉시 노드를 연속 실행합니다.</summary>
        private void ProcessCurrentNode()
        {
            int immediateSteps = 0;
            while (currentNode != null)
            {
                if (++immediateSteps > MaxImmediateNodeSteps)
                {
                    FaultConversation(
                        $"[Dialogue] 즉시 실행 노드가 최대 허용 횟수 {MaxImmediateNodeSteps}회를 초과했습니다. " +
                        "Condition, Action, 0초 Wait 또는 Default로 진행하는 Choice 노드의 순환 연결이 있는지 확인하세요.");
                    return;
                }

                switch (currentNode)
                {
                    case DialogueConditionNodeData condition:
                        if (!ProcessCondition(condition))
                        {
                            return;
                        }
                        continue;

                    case DialogueActionNodeData action:
                        if (!ProcessAction(action))
                        {
                            return;
                        }
                        continue;

                    case DialogueEndNodeData:
                        FinishConversation(DialogueEndReason.Completed);
                        return;

                    case DialogueWaitNodeData wait:
                        if (ProcessWait(wait))
                        {
                            continue;
                        }
                        return;

                    case DialogueWaitSignalNodeData waitSignal:
                        ProcessSignalWait(waitSignal);
                        return;

                    case DialogueNodeData dialogue:
                        ProcessDialogueLine(dialogue);
                        return;

                    case DialogueChoiceNodeData choiceNode:
                        if (ProcessChoiceNode(choiceNode))
                        {
                            continue;
                        }
                        return;

                    default:
                        FaultConversation($"[Dialogue] 지원하지 않는 노드 타입입니다: '{currentNode.GetType().FullName}'.");
                        return;
                }
            }

            FaultConversation("[Dialogue] 현재 노드가 예기치 않게 null이 되었습니다.");
        }

        private bool ProcessCondition(DialogueConditionNodeData condition)
        {
            int sessionId = activeSessionId;
            NodeBaseData expectedNode = currentNode;
            bool evaluated = DialogueEventRegistry.TryEvaluateCondition(
                condition.Condition,
                currentContext,
                out bool result);

            if (!IsCurrentSession(sessionId, expectedNode))
            {
                return false;
            }

            if (!evaluated)
            {
                FaultConversation($"[Dialogue] Condition '{condition.Condition.Key}'을(를) 평가하지 못했습니다.");
                return false;
            }

            return TryMoveToNextNode(condition.Guid, result ? "True" : "False");
        }

        private bool ProcessAction(DialogueActionNodeData action)
        {
            if (!HasActionKey(action.Event.Key))
            {
                FaultConversation($"[Dialogue] Action 노드 '{action.Guid}'에 이벤트 키가 없습니다.");
                return false;
            }

            int sessionId = activeSessionId;
            NodeBaseData expectedNode = currentNode;
            bool executed = DialogueEventRegistry.ExecuteAction(
                action.Event,
                currentContext);

            if (!IsCurrentSession(sessionId, expectedNode))
            {
                return false;
            }

            if (!executed)
            {
                FaultConversation($"[Dialogue] 노드 '{action.Guid}'에서 Action '{action.Event.Key}' 실행에 실패했습니다.");
                return false;
            }

            return TryMoveToNextNode(action.Guid, "Next");
        }

        /// <returns>대기 시간이 0이라 즉시 진행했으면 true, 실제 대기를 시작했으면 false입니다.</returns>
        private bool ProcessWait(DialogueWaitNodeData wait)
        {
            float duration = wait.DurationSeconds;
            if (duration < 0f || float.IsNaN(duration) || float.IsInfinity(duration))
            {
                FaultConversation($"[Dialogue] Wait 노드 '{wait.Guid}'의 대기 시간 '{duration}'이(가) 올바르지 않습니다.");
                return false;
            }

            if (duration == 0f)
            {
                return TryMoveToNextNode(wait.Guid, "Next");
            }

            blockKind = BlockKind.Time;
            waitRemainingSeconds = duration;
            waitUsesUnscaledTime = wait.UseUnscaledTime;
            DialogueRuntimeDriver.Ensure();
            return false;
        }

        private void ProcessSignalWait(DialogueWaitSignalNodeData waitSignal)
        {
            string signalKey = waitSignal.SignalKey;
            if (string.IsNullOrEmpty(signalKey))
            {
                FaultConversation($"[Dialogue] Wait Signal 노드 '{waitSignal.Guid}'에 Signal 키가 없습니다.");
                return;
            }

            BeginSignalWait(signalKey);
        }

        private void ProcessDialogueLine(DialogueNodeData dialogue)
        {
            int sessionId = activeSessionId;
            NodeBaseData expectedNode = currentNode;

            if (HasActionKey(dialogue.Event.Key)
                && !DialogueEventRegistry.ExecuteAction(
                    dialogue.Event,
                    currentContext))
            {
                if (IsCurrentSession(sessionId, expectedNode))
                {
                    FaultConversation(
                        $"[Dialogue] 노드 '{dialogue.Guid}'에서 Action '{dialogue.Event.Key}' 실행에 실패했습니다.");
                }
                return;
            }

            blockKind = BlockKind.Line;

            if (!IsCurrentSession(sessionId, expectedNode)
                || !DispatchSessionEvent(OnShowLine, dialogue, nameof(OnShowLine), sessionId, expectedNode))
            {
                return;
            }
        }

        /// <summary>표시 가능한 선택지를 준비하고 선택 입력을 기다립니다.</summary>
        /// <returns>표시할 선택지가 없어 Default로 즉시 진행했으면 true입니다.</returns>
        private bool ProcessChoiceNode(DialogueChoiceNodeData choiceNode)
        {
            int sessionId = activeSessionId;
            NodeBaseData expectedNode = currentNode;

            if (!TryBuildVisibleChoices(choiceNode, sessionId, expectedNode))
            {
                return false;
            }

            if (currentVisibleChoices.Count == 0)
            {
                return TryMoveToNextNode(
                    choiceNode.Guid,
                    DialogueChoiceNodeData.DefaultPortName,
                    allowImplicitCompletion: true);
            }

            blockKind = BlockKind.Choice;
            DispatchSessionEvent(
                OnShowChoices,
                new List<DialogueChoiceData>(currentVisibleChoices),
                nameof(OnShowChoices),
                sessionId,
                expectedNode);
            return false;
        }

        /// <summary>선택지별 조건을 평가하고 현재 진입에서 사용할 수 있는 선택지만 보관합니다.</summary>
        private bool TryBuildVisibleChoices(
            DialogueChoiceNodeData choiceNode,
            int sessionId,
            NodeBaseData expectedNode)
        {
            currentVisibleChoices.Clear();
            foreach (DialogueChoiceData choice in choiceNode.Choices)
            {
                if (string.IsNullOrWhiteSpace(choice.VisibilityCondition.Key))
                {
                    currentVisibleChoices.Add(choice);
                    continue;
                }

                bool evaluated = DialogueEventRegistry.TryEvaluateCondition(
                    choice.VisibilityCondition,
                    currentContext,
                    out bool visible);
                if (!IsCurrentSession(sessionId, expectedNode))
                {
                    return false;
                }

                if (!evaluated)
                {
                    FaultConversation(
                        $"[Dialogue] 노드 '{choiceNode.Guid}'에서 선택지 Condition " +
                        $"'{choice.VisibilityCondition.Key}'을(를) 평가하지 못했습니다.");
                    return false;
                }

                if (visible)
                {
                    currentVisibleChoices.Add(choice);
                }
            }

            return true;
        }
    }
}
