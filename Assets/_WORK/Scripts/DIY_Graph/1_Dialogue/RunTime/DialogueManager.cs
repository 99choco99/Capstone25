using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>
    /// 특정 UI 구현에 의존하지 않고 한 번에 하나의 대화 세션을 실행합니다. 즉시 노드는 연속 실행하고,
    /// 대화문·선택지·시간·신호 노드는 외부 입력이나 조건을 기다립니다.
    /// </summary>
    public sealed partial class DialogueManager
    {
        private enum BlockKind
        {
            None,
            Line,
            Choice,
            Time,
            Signal
        }

        private const int MaxImmediateNodeSteps = 256;

        private static DialogueManager instance;

        private readonly Dictionary<string, NodeBaseData> nodesByGuid = new();
        private readonly Dictionary<(string NodeGuid, string PortName), List<NodeLinkData>> linksByOutput = new();
        private readonly Dictionary<string, DialogueStartNodeData> entriesById = new();
        private readonly List<DialogueChoiceData> currentVisibleChoices = new();

        private DialogueContainer currentContainer;
        private DialogueContext currentContext;
        private NodeBaseData currentNode;
        private string currentEntryId;
        private int sessionSequence;
        private int activeSessionId;
        private BlockKind blockKind;
        private float waitRemainingSeconds;
        private bool waitUsesUnscaledTime;
        private string waitingSignalKey;
        private bool isSignalSubscribed;
        private bool isFinishingConversation;
        private Action completionCallback;

        private DialogueManager()
        {
        }

        public static DialogueManager Instance => instance ??= new DialogueManager();

        public bool IsWaitingForChoice => blockKind == BlockKind.Choice;
        public bool IsConversationActive => currentContainer != null;
        public string CurrentEntryId => currentEntryId;
        public DialogueEndReason? LastEndReason { get; private set; }

        public event Action OnConversationStart;
        public event Action OnConversationEnd;
        public event Action<DialogueEndReason> OnConversationFinished;
        public event Action<DialogueNodeData> OnShowLine;
        public event Action<List<DialogueChoiceData>> OnShowChoices;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance?.ResetSessionStateForPlayMode();
            instance = null;
        }

        /// <summary>프로젝트가 시작 시점을 명시적으로 관리하고 싶을 때 서비스를 미리 생성합니다.</summary>
        public static void Init()
        {
            _ = Instance;
        }

        /// <summary>Speaker와 Interactor 문맥으로 기본 Entry의 대화를 시작합니다.</summary>
        public void StartConversation(
            DialogueContainer container,
            GameObject speaker,
            GameObject interactor,
            Action onComplete = null)
        {
            TryStartConversation(
                container,
                DialogueStartNodeData.DefaultEntryId,
                new DialogueContext(speaker, interactor),
                onComplete);
        }

        /// <summary>
        /// 이름으로 시작점을 찾아 대화를 시작합니다. 다른 대화가 실행 중이거나 그래프 검증에 실패하면 false를 반환합니다.
        /// </summary>
        public bool TryStartConversation(
            DialogueContainer container,
            string entryId,
            DialogueContext context,
            Action onComplete = null)
        {
            if (IsConversationActive || isFinishingConversation)
            {
                Debug.LogWarning(
                    isFinishingConversation
                        ? "[Dialogue] 완료 콜백을 실행하는 동안에는 새 대화를 시작할 수 없습니다."
                        : "[Dialogue] 이미 대화가 진행 중입니다.");
                return false;
            }

            if (container == null)
            {
                Debug.LogWarning("[Dialogue] null인 대화 그래프는 시작할 수 없습니다.");
                return false;
            }

            if (context == null)
            {
                Debug.LogError($"[Dialogue] '{container.name}'을 실행하려면 DialogueContext가 필요합니다.", container);
                return false;
            }

            if (!GraphAssetMigrator.TryMigrate(container, out _, out string error))
            {
                Debug.LogError($"[Dialogue] 대화를 시작하지 못했습니다. {error}", container);
                return false;
            }

            if (!TryBuildRuntimeIndexes(container, out error))
            {
                Debug.LogError($"[Dialogue] 대화를 시작하지 못했습니다. {error}", container);
                return false;
            }

            string requestedEntryId = string.IsNullOrWhiteSpace(entryId)
                ? DialogueStartNodeData.DefaultEntryId
                : entryId.Trim();
            if (!entriesById.TryGetValue(requestedEntryId, out DialogueStartNodeData entryNode))
            {
                Debug.LogError(
                    $"[Dialogue] 대화를 시작하지 못했습니다. 대화 그래프 '{container.name}'에 " +
                    $"진입점 '{requestedEntryId}'가 없습니다.",
                    container);
                return false;
            }

            if (!TryResolveNextNode(entryNode.Guid, "Next", out NodeBaseData firstNode, out _, out error))
            {
                Debug.LogError($"[Dialogue] 대화를 시작하지 못했습니다. {error}", container);
                return false;
            }

            currentContainer = container;
            currentContext = context;
            currentEntryId = entryNode.EntryId;
            completionCallback = onComplete;
            LastEndReason = null;
            ClearBlockingState();
            activeSessionId = ++sessionSequence;

            int expectedSessionId = activeSessionId;
            if (!DispatchSessionEvent(OnConversationStart, nameof(OnConversationStart), expectedSessionId))
            {
                return true;
            }

            currentNode = firstNode;
            ProcessCurrentNode();
            return true;
        }

        /// <summary>현재 대화를 정상 완료하고 완료 콜백을 호출합니다.</summary>
        public void EndConversation()
        {
            FinishConversation(DialogueEndReason.Completed);
        }

        /// <summary>완료 콜백을 호출하지 않고 현재 대화를 취소합니다.</summary>
        public void CancelConversation()
        {
            FinishConversation(DialogueEndReason.Cancelled);
        }

        /// <summary>선택지가 없는 현재 대화문을 다음 흐름으로 진행합니다.</summary>
        public void ContinueNextLine()
        {
            if (!IsConversationActive || blockKind != BlockKind.Line || currentNode is not DialogueNodeData)
            {
                return;
            }

            AdvanceFromBlockingNode(activeSessionId, currentNode, "Next", allowImplicitCompletion: true);
        }

        /// <summary>현재 표시된 선택지 하나를 소비하고 해당 선택지의 고정 포트를 따라 진행합니다.</summary>
        public void OnSelectionChoice(DialogueChoiceData choice)
        {
            if (!IsConversationActive
                || blockKind != BlockKind.Choice
                || choice == null
                || currentNode is not DialogueChoiceNodeData)
            {
                return;
            }

            DialogueChoiceData resolvedChoice = currentVisibleChoices.Find(candidate =>
                candidate.PortName == choice.PortName);
            if (resolvedChoice == null)
            {
                Debug.LogWarning("[Dialogue] 요청한 선택지는 현재 노드에 속하지 않습니다.");
                return;
            }

            int sessionId = activeSessionId;
            NodeBaseData expectedNode = currentNode;
            ClearBlockingState();

            if (HasActionKey(resolvedChoice.ChoiceEvent.Key)
                && !DialogueEventRegistry.ExecuteAction(
                    resolvedChoice.ChoiceEvent,
                    currentContext))
            {
                if (IsCurrentSession(sessionId, expectedNode))
                {
                    FaultConversation(
                        $"[Dialogue] 노드 '{expectedNode.Guid}'에서 선택지 Action " +
                        $"'{resolvedChoice.ChoiceEvent.Key}' 실행에 실패했습니다.");
                }
                return;
            }

            AdvanceFromBlockingNode(
                sessionId,
                expectedNode,
                resolvedChoice.PortName,
                allowImplicitCompletion: true);
        }

        /// <summary>시간을 기다리는 Wait 노드를 갱신합니다. <see cref="DialogueRuntimeDriver"/>가 호출합니다.</summary>
        public void Tick(float scaledDeltaTime, float unscaledDeltaTime)
        {
            if (!IsConversationActive || blockKind != BlockKind.Time || currentNode == null)
            {
                return;
            }

            float deltaTime = waitUsesUnscaledTime ? unscaledDeltaTime : scaledDeltaTime;
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            waitRemainingSeconds -= deltaTime;
            if (waitRemainingSeconds <= 0f)
            {
                AdvanceFromBlockingNode(activeSessionId, currentNode, "Next", allowImplicitCompletion: false);
            }
        }

        private void NextNode(string currentGuid, string portName, bool allowImplicitCompletion = false)
        {
            if (IsConversationActive && TryMoveToNextNode(currentGuid, portName, allowImplicitCompletion))
            {
                ProcessCurrentNode();
            }
        }

        private bool TryMoveToNextNode(string currentGuid, string portName, bool allowImplicitCompletion = false)
        {
            if (!TryResolveNextNode(
                    currentGuid,
                    portName,
                    out NodeBaseData nextNode,
                    out int linkCount,
                    out string error))
            {
                if (allowImplicitCompletion && linkCount == 0)
                {
                    FinishConversation(DialogueEndReason.Completed);
                }
                else
                {
                    FaultConversation($"[Dialogue] {error}");
                }
                return false;
            }

            currentNode = nextNode;
            return true;
        }

        /// <summary>출력 포트 하나에 연결된 다음 노드를 런타임 인덱스에서 찾습니다.</summary>
        private bool TryResolveNextNode(
            string currentGuid,
            string portName,
            out NodeBaseData nextNode,
            out int linkCount,
            out string error)
        {
            nextNode = null;
            linksByOutput.TryGetValue((currentGuid, portName), out List<NodeLinkData> links);
            linkCount = links?.Count ?? 0;
            if (linkCount == 0)
            {
                error = $"노드 '{currentGuid}'의 출력 포트 '{portName}'에 연결선이 없습니다.";
                return false;
            }

            if (linkCount > 1)
            {
                error = $"노드 '{currentGuid}'의 단일 흐름 출력 포트 '{portName}'에 " +
                        $"연결선이 {linkCount}개 있습니다.";
                return false;
            }

            string targetGuid = links[0].TargetNodeGuid;
            if (!nodesByGuid.TryGetValue(targetGuid, out nextNode))
            {
                error = $"연결선이 가리키는 대상 노드 '{targetGuid}'가 존재하지 않습니다.";
                return false;
            }

            error = null;
            return true;
        }

        private void AdvanceFromBlockingNode(
            int expectedSessionId,
            NodeBaseData expectedNode,
            string portName,
            bool allowImplicitCompletion)
        {
            if (!IsCurrentSession(expectedSessionId, expectedNode))
            {
                return;
            }

            string guid = expectedNode.Guid;
            ClearBlockingState();
            NextNode(guid, portName, allowImplicitCompletion);
        }

        private void BeginSignalWait(string signalKey)
        {
            ClearBlockingState();
            blockKind = BlockKind.Signal;
            waitingSignalKey = signalKey;
            DialogueSignal.Published += HandleSignalPublished;
            isSignalSubscribed = true;
        }

        private void HandleSignalPublished(string signalKey)
        {
            if (IsConversationActive
                && blockKind == BlockKind.Signal
                && currentNode != null
                && waitingSignalKey == signalKey)
            {
                AdvanceFromBlockingNode(activeSessionId, currentNode, "Next", allowImplicitCompletion: false);
            }
        }

    }
}
