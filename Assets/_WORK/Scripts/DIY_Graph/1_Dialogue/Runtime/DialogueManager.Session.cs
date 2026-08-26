using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Dialogue 세션 종료, 실행 인덱스와 구독자 예외 격리를 담당합니다.</summary>
    public sealed partial class DialogueManager
    {
        private void FinishConversation(DialogueEndReason reason)
        {
            if (!IsConversationActive)
            {
                return;
            }

            Action callback = reason == DialogueEndReason.Completed ? completionCallback : null;
            ClearBlockingState();
            ClearSessionData();
            LastEndReason = reason;

            isFinishingConversation = true;
            try
            {
                DispatchSafely(OnConversationEnd, nameof(OnConversationEnd));
                DispatchSafely(OnConversationFinished, reason, nameof(OnConversationFinished));
            }
            finally
            {
                isFinishingConversation = false;
            }

            DispatchSafely(callback, "대화 완료 콜백");
        }

        private void FaultConversation(string message)
        {
            Debug.LogError(message, currentContainer);
            FinishConversation(DialogueEndReason.Faulted);
        }

        private bool TryBuildRuntimeIndexes(DialogueContainer container, out string error)
        {
            nodesByGuid.Clear();
            linksByOutput.Clear();
            entriesById.Clear();

            if (container.Nodes == null || container.Nodes.Count == 0)
            {
                error = $"대화 그래프 '{container.name}'에 노드가 없습니다.";
                return false;
            }

            if (container.NodeLinks == null)
            {
                error = $"대화 그래프 '{container.name}'의 연결선 목록이 null입니다.";
                return false;
            }

            foreach (NodeBaseData node in container.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Guid) || !nodesByGuid.TryAdd(node.Guid, node))
                {
                    error = $"대화 그래프 '{container.name}'에 null 데이터, 빈 GUID 또는 중복 GUID가 있습니다.";
                    return false;
                }

                if (node is DialogueStartNodeData entry && !entriesById.TryAdd(entry.EntryId, entry))
                {
                    error = $"대화 그래프 '{container.name}'에 중복된 진입점 ID '{entry.EntryId}'가 있습니다.";
                    return false;
                }

                if (node is not DialogueChoiceNodeData choiceNode)
                {
                    continue;
                }

                if (choiceNode.Choices == null)
                {
                    error = $"대화 그래프 '{container.name}'의 Choice 노드 '{choiceNode.Guid}'의 선택지 목록이 null입니다.";
                    return false;
                }

                if (choiceNode.Choices.Count == 0)
                {
                    error = $"대화 그래프 '{container.name}'의 Choice 노드 '{choiceNode.Guid}'에 선택지가 없습니다.";
                    return false;
                }

                var portIds = new HashSet<string> { DialogueChoiceNodeData.DefaultPortName };
                foreach (DialogueChoiceData choice in choiceNode.Choices)
                {
                    if (choice == null || string.IsNullOrWhiteSpace(choice.PortName))
                    {
                        error = $"대화 그래프 '{container.name}'의 Choice 노드 '{choiceNode.Guid}'에 " +
                                "null 선택지 또는 빈 선택지 포트 ID가 있습니다.";
                        return false;
                    }

                    if (!portIds.Add(choice.PortName))
                    {
                        error = $"대화 그래프 '{container.name}'의 Choice 노드 '{choiceNode.Guid}'가 " +
                                $"중복되었거나 예약된 선택지 포트 '{choice.PortName}'을 사용합니다.";
                        return false;
                    }
                }
            }

            var uniqueLinks = new HashSet<string>();
            foreach (NodeLinkData link in container.NodeLinks)
            {
                if (link == null
                    || string.IsNullOrWhiteSpace(link.StartNodeGuid)
                    || string.IsNullOrWhiteSpace(link.StartPortName)
                    || string.IsNullOrWhiteSpace(link.TargetNodeGuid))
                {
                    error = $"대화 그래프 '{container.name}'에 불완전한 연결선이 있습니다.";
                    return false;
                }

                string linkKey = $"{link.StartNodeGuid}\u001F{link.StartPortName}\u001F" +
                                 $"{link.TargetNodeGuid}\u001F{link.TargetPortName}";
                if (!uniqueLinks.Add(linkKey))
                {
                    error = $"대화 그래프 '{container.name}'에 중복된 연결선이 있습니다: " +
                            $"{link.StartNodeGuid}.{link.StartPortName} -> {link.TargetNodeGuid}.{link.TargetPortName}.";
                    return false;
                }

                if (!nodesByGuid.ContainsKey(link.StartNodeGuid) || !nodesByGuid.ContainsKey(link.TargetNodeGuid))
                {
                    error = $"대화 그래프 '{container.name}'에 존재하지 않는 노드를 가리키는 연결선이 있습니다.";
                    return false;
                }

                var key = (link.StartNodeGuid, link.StartPortName);
                if (!linksByOutput.TryGetValue(key, out List<NodeLinkData> links))
                {
                    links = new List<NodeLinkData>();
                    linksByOutput.Add(key, links);
                }
                links.Add(link);
            }

            error = null;
            return true;
        }

        private bool IsCurrentSession(int sessionId, NodeBaseData expectedNode = null)
        {
            return IsConversationActive
                   && activeSessionId == sessionId
                   && (expectedNode == null || ReferenceEquals(currentNode, expectedNode));
        }

        private bool DispatchSessionEvent(
            Action handlers,
            string eventName,
            int expectedSessionId,
            NodeBaseData expectedNode = null)
        {
            if (handlers == null)
            {
                return IsCurrentSession(expectedSessionId, expectedNode);
            }

            foreach (Delegate subscriber in handlers.GetInvocationList())
            {
                try
                {
                    ((Action)subscriber)();
                }
                catch (Exception exception)
                {
                    HandleSessionSubscriberException(eventName, exception, expectedSessionId, expectedNode);
                    return false;
                }

                if (!IsCurrentSession(expectedSessionId, expectedNode))
                {
                    return false;
                }
            }

            return true;
        }

        private bool DispatchSessionEvent<T>(
            Action<T> handlers,
            T value,
            string eventName,
            int expectedSessionId,
            NodeBaseData expectedNode = null)
        {
            if (handlers == null)
            {
                return IsCurrentSession(expectedSessionId, expectedNode);
            }

            foreach (Delegate subscriber in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<T>)subscriber)(value);
                }
                catch (Exception exception)
                {
                    HandleSessionSubscriberException(eventName, exception, expectedSessionId, expectedNode);
                    return false;
                }

                if (!IsCurrentSession(expectedSessionId, expectedNode))
                {
                    return false;
                }
            }

            return true;
        }

        private void HandleSessionSubscriberException(
            string eventName,
            Exception exception,
            int expectedSessionId,
            NodeBaseData expectedNode)
        {
            Debug.LogError($"[Dialogue] {eventName} 구독자 실행 중 예외가 발생했습니다.");
            Debug.LogException(exception);
            if (IsCurrentSession(expectedSessionId, expectedNode))
            {
                FinishConversation(DialogueEndReason.Faulted);
            }
        }

        private static void DispatchSafely(Action handlers, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Delegate subscriber in handlers.GetInvocationList())
            {
                try
                {
                    ((Action)subscriber)();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[Dialogue] {eventName} 구독자 실행 중 예외가 발생했습니다.");
                    Debug.LogException(exception);
                }
            }
        }

        private static void DispatchSafely<T>(Action<T> handlers, T value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Delegate subscriber in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<T>)subscriber)(value);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[Dialogue] {eventName} 구독자 실행 중 예외가 발생했습니다.");
                    Debug.LogException(exception);
                }
            }
        }

        private static bool HasActionKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key)
                   && key != "None";
        }

        private void ClearBlockingState()
        {
            if (isSignalSubscribed)
            {
                DialogueSignal.Published -= HandleSignalPublished;
                isSignalSubscribed = false;
            }

            blockKind = BlockKind.None;
            waitRemainingSeconds = 0f;
            waitUsesUnscaledTime = false;
            waitingSignalKey = null;
            currentVisibleChoices.Clear();
        }

        private void ClearSessionData()
        {
            currentContainer = null;
            currentContext = null;
            currentNode = null;
            currentEntryId = null;
            completionCallback = null;
            activeSessionId = 0;
            nodesByGuid.Clear();
            linksByOutput.Clear();
            entriesById.Clear();
        }

        private void ResetSessionStateForPlayMode()
        {
            ClearBlockingState();
            ClearSessionData();
            LastEndReason = null;
            isFinishingConversation = false;
            sessionSequence++;
        }
    }
}
