using System.Collections.Generic;
using System.Linq;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>대화를 실행하기 전에 Dialogue 그래프 작성 문제를 찾아 보고합니다.</summary>
    public sealed class DialogueGraphValidator : GraphValidator<DialogueContainer>
    {
        /// <summary>시작점, 분기, 바인딩, 도달 가능 여부와 즉시 실행 순환을 검사합니다.</summary>
        protected override void Validate(
            DialogueContainer container,
            GraphValidationContext context,
            ICollection<GraphValidationIssue> issues)
        {
            DialogueStartNodeData[] entries = context.Nodes.OfType<DialogueStartNodeData>().ToArray();
            if (entries.Length == 0)
            {
                AddError(
                    "DIALOGUE_NO_ENTRY",
                    "Dialogue Entry 노드를 하나 이상 추가하세요.");
            }

            foreach (IGrouping<string, DialogueStartNodeData> duplicates in entries
                         .GroupBy(entry => entry.EntryId)
                         .Where(group => group.Count() > 1))
            {
                foreach (DialogueStartNodeData entry in duplicates)
                {
                    AddError(
                        "DIALOGUE_DUPLICATE_ENTRY",
                        $"Entry ID '{duplicates.Key}'를 둘 이상의 Entry 노드가 사용하고 있습니다.",
                        entry.Guid);
                }
            }

            foreach (NodeBaseData node in context.Nodes.Where(node => node != null))
            {
                switch (node)
                {
                    case DialogueStartNodeData entry:
                        RequireExactlyOneOutput(entry.Guid, "Next", "Dialogue Entry는 첫 번째 노드 하나에 정확히 연결되어야 합니다.");
                        break;

                    case DialogueNodeData line:
                        ValidateLine(line);
                        break;

                    case DialogueChoiceNodeData choiceNode:
                        ValidateChoiceNode(choiceNode);
                        break;

                    case DialogueConditionNodeData condition:
                        ValidateRequiredBinding(
                            condition.Guid,
                            MethodKind.Condition,
                            condition.Condition);
                        RequireExactlyOneOutput(condition.Guid, "True", "Condition의 True 출력은 노드 하나에 정확히 연결되어야 합니다.");
                        RequireExactlyOneOutput(condition.Guid, "False", "Condition의 False 출력은 노드 하나에 정확히 연결되어야 합니다.");
                        break;

                    case DialogueActionNodeData action:
                        ValidateRequiredBinding(
                            action.Guid,
                            MethodKind.Action,
                            action.Event);
                        RequireExactlyOneOutput(action.Guid, "Next", "Action 노드는 다음 노드 하나에 정확히 연결되어야 합니다.");
                        break;

                    case DialogueWaitNodeData wait:
                        if (wait.DurationSeconds < 0f
                            || float.IsNaN(wait.DurationSeconds)
                            || float.IsInfinity(wait.DurationSeconds))
                        {
                            AddError(
                                "DIALOGUE_WAIT_DURATION",
                                "Wait 시간은 0 이상의 유한한 값이어야 합니다.",
                                wait.Guid);
                        }
                        RequireExactlyOneOutput(wait.Guid, "Next", "Wait 노드는 다음 노드 하나에 정확히 연결되어야 합니다.");
                        break;

                    case DialogueWaitSignalNodeData signal:
                        if (string.IsNullOrEmpty(signal.SignalKey))
                        {
                            AddError("DIALOGUE_SIGNAL_KEY", "Wait Signal에는 비어 있지 않은 Signal 키가 필요합니다.", signal.Guid);
                        }
                        RequireExactlyOneOutput(signal.Guid, "Next", "Wait Signal 노드는 다음 노드 하나에 정확히 연결되어야 합니다.");
                        break;

                    case DialogueEndNodeData end:
                        if (context.GetOutgoing(end.Guid).Count > 0)
                        {
                            AddError("DIALOGUE_END_OUTPUT", "End 노드에는 나가는 연결선이 있을 수 없습니다.", end.Guid);
                        }
                        break;

                    default:
                        AddError(
                            "DIALOGUE_UNSUPPORTED_NODE",
                            $"DialogueManager가 노드 타입 '{node.GetType().Name}'을 지원하지 않습니다.",
                            node.Guid);
                        break;
                }
            }

            HashSet<string> reachable = context.GetReachableNodeGuids(entries.Select(entry => entry.Guid));
            foreach (NodeBaseData node in context.Nodes.Where(node => node != null && !reachable.Contains(node.Guid)))
            {
                AddWarning("DIALOGUE_UNREACHABLE", "어떤 Dialogue Entry에서도 이 노드에 도달할 수 없습니다.", node.Guid);
            }

            HashSet<string> immediateCycleNodes = GraphValidatorRegistry.FindCycleNodes(
                context,
                node => node is DialogueActionNodeData
                        || node is DialogueConditionNodeData
                        || node is DialogueWaitNodeData wait && wait.DurationSeconds <= 0f);
            foreach (string nodeGuid in immediateCycleNodes)
            {
                AddError(
                    "DIALOGUE_IMMEDIATE_CYCLE",
                    "이 노드는 실행을 멈출 대사, Signal 또는 양수 Wait가 없는 순환에 포함되어 있습니다.",
                    nodeGuid);
            }

            void ValidateLine(DialogueNodeData line)
            {
                if (string.IsNullOrWhiteSpace(line.DialogueText))
                {
                    AddWarning("DIALOGUE_EMPTY_TEXT", "대화문이 비어 있습니다.", line.Guid);
                }

                ValidateOptionalBinding(
                    line.Guid,
                    MethodKind.Action,
                    line.Event,
                    "대화 진입 Action");

                ValidateOptionalSingleOutput(
                    line.Guid,
                    "Next",
                    "연결되지 않은 대화 노드는 대화를 종료합니다. 연결선을 추가하거나 명시적인 End 노드를 사용하세요.");
            }

            void ValidateChoiceNode(DialogueChoiceNodeData choiceNode)
            {
                if (choiceNode.Choices == null)
                {
                    AddError("DIALOGUE_NULL_CHOICES", "Choice 노드의 선택지 목록이 null입니다.", choiceNode.Guid);
                    return;
                }

                if (choiceNode.Choices.Count == 0)
                {
                    AddError("DIALOGUE_EMPTY_CHOICES", "Choice 노드에는 선택지를 하나 이상 추가해야 합니다.", choiceNode.Guid);
                    return;
                }

                var portIds = new HashSet<string> { DialogueChoiceNodeData.DefaultPortName };
                foreach (DialogueChoiceData choice in choiceNode.Choices)
                {
                    if (choice == null || string.IsNullOrWhiteSpace(choice.PortName))
                    {
                        AddError("DIALOGUE_INVALID_CHOICE", "선택지가 null이거나 고정 포트 ID가 없습니다.", choiceNode.Guid);
                        continue;
                    }

                    if (!portIds.Add(choice.PortName))
                    {
                        AddError("DIALOGUE_DUPLICATE_CHOICE", $"선택지 포트 '{choice.PortName}'이 중복되었거나 예약된 포트입니다.", choiceNode.Guid);
                    }

                    if (string.IsNullOrWhiteSpace(choice.ChoiceText))
                    {
                        AddWarning("DIALOGUE_EMPTY_CHOICE", "선택지에 표시할 문장이 없습니다.", choiceNode.Guid);
                    }

                    ValidateOptionalBinding(
                        choiceNode.Guid,
                        MethodKind.Condition,
                        choice.VisibilityCondition,
                        "선택지 표시 Condition");

                    ValidateOptionalBinding(
                        choiceNode.Guid,
                        MethodKind.Action,
                        choice.ChoiceEvent,
                        "선택지 Action");

                    ValidateOptionalSingleOutput(
                        choiceNode.Guid,
                        choice.PortName,
                        "연결되지 않은 선택지는 대화를 종료합니다. 연결선을 추가하거나 명시적인 End 노드를 사용하세요.");
                }

                if (choiceNode.Choices.All(choice =>
                        choice != null && !string.IsNullOrWhiteSpace(choice.VisibilityCondition.Key)))
                {
                    ValidateOptionalSingleOutput(
                        choiceNode.Guid,
                        DialogueChoiceNodeData.DefaultPortName,
                        "모든 선택지에 Condition이 있습니다. 모든 선택지가 숨겨지는 경우를 처리하려면 Default를 연결하고, " +
                        "대화를 종료하려면 비워 두세요.");
                }
                else
                {
                    int defaultCount = context.GetOutgoing(choiceNode.Guid, DialogueChoiceNodeData.DefaultPortName).Count;
                    if (defaultCount > 1)
                    {
                        AddError(
                            "DIALOGUE_OUTPUT_COUNT",
                            $"출력 포트 '{DialogueChoiceNodeData.DefaultPortName}'에는 연결선이 {defaultCount}개 있지만 하나만 허용됩니다.",
                            choiceNode.Guid);
                    }
                    else if (defaultCount == 1)
                    {
                        AddWarning(
                            "DIALOGUE_UNUSED_DEFAULT",
                            "조건 없는 선택지가 있으므로 Default는 사용되지 않습니다.",
                            choiceNode.Guid);
                    }
                }

            }

            void ValidateRequiredBinding(
                string nodeGuid,
                MethodKind kind,
                MethodCallData methodCall)
            {
                if (string.IsNullOrWhiteSpace(methodCall.Key))
                {
                    AddError(
                        kind == MethodKind.Action ? "DIALOGUE_ACTION_REQUIRED" : "DIALOGUE_CONDITION_REQUIRED",
                        $"등록된 Dialogue {kind}을 선택하세요.",
                        nodeGuid);
                    return;
                }

                ValidateOptionalBinding(nodeGuid, kind, methodCall, kind.ToString().ToLowerInvariant());
            }

            void ValidateOptionalBinding(
                string nodeGuid,
                MethodKind kind,
                MethodCallData methodCall,
                string label)
            {
                if (string.IsNullOrWhiteSpace(methodCall.Key))
                {
                    return;
                }

                if (!DialogueMethodCatalog.TryGetMethod(kind, methodCall.Key, out DialogueMethodDescriptor descriptor))
                {
                    AddError("DIALOGUE_MISSING_METHOD", $"{label} 키 '{methodCall.Key}'가 등록되어 있지 않습니다.", nodeGuid);
                    return;
                }

                if (!MethodArgumentCodec.TryValidateArguments(methodCall.Arguments, descriptor, out string error))
                {
                    AddError("DIALOGUE_ARGUMENTS", $"{label} 인수가 올바르지 않습니다: {error}", nodeGuid);
                }
            }

            void RequireExactlyOneOutput(string nodeGuid, string portName, string message)
            {
                int count = context.GetOutgoing(nodeGuid, portName).Count;
                if (count != 1)
                {
                    AddError("DIALOGUE_OUTPUT_COUNT", $"{message} {portName}에서 연결선 {count}개를 발견했습니다.", nodeGuid);
                }
            }

            void ValidateOptionalSingleOutput(string nodeGuid, string portName, string missingMessage)
            {
                int count = context.GetOutgoing(nodeGuid, portName).Count;
                if (count == 0)
                {
                    AddWarning("DIALOGUE_IMPLICIT_END", missingMessage, nodeGuid);
                }
                else if (count > 1)
                {
                    AddError("DIALOGUE_OUTPUT_COUNT", $"출력 포트 '{portName}'에는 연결선이 {count}개 있지만 하나만 허용됩니다.", nodeGuid);
                }
            }

            void AddError(
                string code,
                string message,
                string nodeGuid = null)
            {
                issues.Add(new GraphValidationIssue(
                    GraphValidationSeverity.Error,
                    code,
                    message,
                    nodeGuid));
            }

            void AddWarning(string code, string message, string nodeGuid = null)
            {
                issues.Add(new GraphValidationIssue(GraphValidationSeverity.Warning, code, message, nodeGuid));
            }
        }
    }
}
