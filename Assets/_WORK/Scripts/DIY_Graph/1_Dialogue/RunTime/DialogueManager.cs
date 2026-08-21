using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalGraph
{
	public class DialogueManager
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

		private Action npcPendingTask;

		public static DialogueManager Instance => instance ?? (instance = new DialogueManager());

		public bool IsWaitingForChoice => blockKind == BlockKind.Choice;

		public bool IsConversationActive => currentContainer != null;

		public string CurrentEntryId => currentEntryId;

		public DialogueEndReason? LastEndReason { get; private set; }

		public event Action OnConversationStart;

		public event Action OnConversationEnd;

		public event Action<DialogueEndReason> OnConversationFinished;

		public event Action<DialogueNodeData> OnShowLine;

		public event Action<List<DialogueChoiceData>> OnShowChoices;

		private DialogueManager()
		{
		}

		[RuntimeInitializeOnLoadMethod]
		private static void ResetStaticState()
		{
			instance?.ResetSessionStateForPlayMode();
		}

		public static void Init()
		{
			_ = Instance;
		}

		public void StartConversation(DialogueContainer container, GameObject speaker, GameObject interactor, Action onComplete = null)
		{
			TryStartConversation(container, "Default", new DialogueContext(speaker, interactor), onComplete);
		}

		public bool TryStartConversation(DialogueContainer container, string entryId, DialogueContext context, Action onComplete = null)
		{
			if (IsConversationActive || isFinishingConversation)
			{
				Debug.LogWarning((object)(isFinishingConversation ? "[Dialogue] ?댁쟾 ?\u0080?붿쓽 醫낅즺 ?뚮┝??泥섎━?섎뒗 以묒뿉?????\u0080?붾? ?쒖옉?????놁뒿?덈떎." : "[Dialogue] ?대? ?\u0080?붽? 吏꾪뻾 以묒엯?덈떎."));
				return false;
			}
			if ((object)container == (object)null)
			{
				Debug.LogWarning((object)"[Dialogue] ?ъ깮??Dialogue Graph媛\u0080 ?놁뒿?덈떎.");
				return false;
			}
			if (context == null)
			{
				Debug.LogError((object)("[Dialogue] '" + ((UnityEngine.Object)container).name + "'???\u0080?붾? ?쒖옉?섎젮硫?DialogueContext媛\u0080 ?꾩슂?⑸땲??"));
				return false;
			}
			if (!container.TryResolveEntry(entryId, out var entryNode, out var error))
			{
				Debug.LogError((object)("[Dialogue] ?\u0080?붾? ?쒖옉?????놁뒿?덈떎. " + error), (UnityEngine.Object)container);
				return false;
			}
			currentContainer = container;
			currentContext = context;
			currentEntryId = entryNode.GetNormalizedEntryId();
			LastEndReason = null;
			npcPendingTask = onComplete;
			ClearBlockingState();
			activeSessionId = ++sessionSequence;
			int expectedSessionId = activeSessionId;
			if (!DispatchSessionEvent(this.OnConversationStart, "OnConversationStart", expectedSessionId))
			{
				return true;
			}
			NextNode(entryNode.Guid, "Next");
			return true;
		}

		public void EndConversation()
		{
			FinishConversation(DialogueEndReason.Completed);
		}

		public void CancelConversation()
		{
			FinishConversation(DialogueEndReason.Cancelled);
		}

		private void FinishConversation(DialogueEndReason reason)
		{
			if (IsConversationActive)
			{
				Action handlers = ((reason == DialogueEndReason.Completed) ? npcPendingTask : null);
				ClearBlockingState();
				currentContainer = null;
				currentNode = null;
				currentContext = null;
				currentEntryId = null;
				npcPendingTask = null;
				activeSessionId = 0;
				LastEndReason = reason;
				isFinishingConversation = true;
				try
				{
					DispatchSafely(this.OnConversationEnd, "OnConversationEnd");
					DispatchSafely(this.OnConversationFinished, reason, "OnConversationFinished");
				}
				finally
				{
					isFinishingConversation = false;
				}
				DispatchSafely(handlers, "Conversation completion callback");
			}
		}

		private void FaultConversation(string message)
		{
			Debug.LogError((object)message);
			FinishConversation(DialogueEndReason.Faulted);
		}

		private void ProcessCurrentNode()
		{
			int num = 0;
			while (currentNode != null)
			{
				num++;
				if (num > 256)
				{
					FaultConversation($"[Dialogue] 利됱떆 泥섎━ ?몃뱶媛\u0080 {256}?뚮? 珥덇낵?덉뒿?덈떎. " + "Condition/Action/0珥?Wait ?쒗솚 ?곌껐???뺤씤?섏꽭??");
					return;
				}
				if (currentNode is ConditionNodeData conditionNodeData)
				{
					int sessionId = activeSessionId;
					NodeBaseData expectedNode = currentNode;
					bool result;
					bool flag = DialogueEventRegistry.TryEvaluateCondition(conditionNodeData.ConditionEventKey, conditionNodeData.ConditionEventArguments, conditionNodeData.ConditionEventParam, currentContext, out result);
					if (!IsCurrentSession(sessionId, expectedNode))
					{
						return;
					}
					if (!flag)
					{
						FaultConversation("[Dialogue] Condition '" + conditionNodeData.ConditionEventKey + "'???됯??섏? 紐삵뻽?듬땲??");
						return;
					}
					string portName = (result ? "True" : "False");
					if (!TryMoveToNextNode(currentNode.Guid, portName))
					{
						return;
					}
					continue;
				}
				if (currentNode is ActionNodeData actionNodeData)
				{
					int sessionId2 = activeSessionId;
					NodeBaseData expectedNode2 = currentNode;
					if (!HasActionKey(actionNodeData.EventKey))
					{
						FaultConversation("[Dialogue] Action ?몃뱶 '" + actionNodeData.Guid + "'??Event Key媛\u0080 ?놁뒿?덈떎.");
						return;
					}
					bool flag2 = DialogueEventRegistry.ExecuteAction(actionNodeData.EventKey, actionNodeData.EventArguments, actionNodeData.EventParam, currentContext);
					if (!IsCurrentSession(sessionId2, expectedNode2))
					{
						return;
					}
					if (!flag2)
					{
						FaultConversation("[Dialogue] Action ?몃뱶 '" + actionNodeData.Guid + "'??'" + actionNodeData.EventKey + "'???ㅽ뻾?섏? 紐삵뻽?듬땲??");
						return;
					}
					if (!TryMoveToNextNode(actionNodeData.Guid, "Next"))
					{
						return;
					}
					continue;
				}
				if (currentNode is EndNodeData)
				{
					FinishConversation(DialogueEndReason.Completed);
					return;
				}
				if (currentNode is WaitNodeData { DurationSeconds: var durationSeconds } waitNodeData)
				{
					if (durationSeconds < 0f || float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds))
					{
						FaultConversation("[Dialogue] Wait ?몃뱶 '" + waitNodeData.Guid + "'???\u0080湲??쒓컙 " + $"'{durationSeconds}'???щ컮瑜댁? ?딆뒿?덈떎.");
						return;
					}
					if (durationSeconds == 0f)
					{
						if (!TryMoveToNextNode(waitNodeData.Guid, "Next"))
						{
							return;
						}
						continue;
					}
					blockKind = BlockKind.Time;
					waitRemainingSeconds = durationSeconds;
					waitUsesUnscaledTime = waitNodeData.UseUnscaledTime;
					DialogueRuntimeDriver.Ensure();
					return;
				}
				if (currentNode is WaitSignalNodeData waitSignalNodeData)
				{
					string normalizedSignalKey = waitSignalNodeData.GetNormalizedSignalKey();
					if (string.IsNullOrWhiteSpace(normalizedSignalKey))
					{
						FaultConversation("[Dialogue] Wait Signal ?몃뱶 '" + waitSignalNodeData.Guid + "'??Signal Key媛\u0080 ?놁뒿?덈떎.");
					}
					else
					{
						BeginSignalWait(normalizedSignalKey);
					}
				}
				else if (currentNode is DialogueNodeData dialogueNodeData)
				{
					int num2 = activeSessionId;
					NodeBaseData expectedNode3 = currentNode;
					blockKind = ((dialogueNodeData.Choices == null || dialogueNodeData.Choices.Count <= 0) ? BlockKind.Line : BlockKind.Choice);
					if (HasActionKey(dialogueNodeData.EventKey))
					{
						bool flag3 = DialogueEventRegistry.ExecuteAction(dialogueNodeData.EventKey, dialogueNodeData.EventArguments, dialogueNodeData.EventParam, currentContext);
						if (!IsCurrentSession(num2, expectedNode3))
						{
							return;
						}
						if (!flag3)
						{
							FaultConversation("[Dialogue] ?몃뱶 '" + dialogueNodeData.Guid + "'??Action '" + dialogueNodeData.EventKey + "'???ㅽ뻾?섏? 紐삵뻽?듬땲??");
							return;
						}
					}
					if (DispatchSessionEvent(this.OnShowLine, dialogueNodeData, "OnShowLine", num2, expectedNode3) && blockKind == BlockKind.Choice && !DispatchSessionEvent(this.OnShowChoices, dialogueNodeData.Choices, "OnShowChoices", num2, expectedNode3))
					{
					}
				}
				else
				{
					FaultConversation("[Dialogue] ?????녿뒗 ?몃뱶 ?\u0080?낆엯?덈떎: " + currentNode.GetType().FullName);
				}
				return;
			}
			FaultConversation("[Dialogue] ?꾩옱 ?몃뱶媛\u0080 ?덇린移??딄쾶 null???섏뿀?듬땲??");
		}

		private bool IsCurrentSession(int sessionId, NodeBaseData expectedNode = null)
		{
			if (!IsConversationActive || activeSessionId != sessionId)
			{
				return false;
			}
			return expectedNode == null || currentNode == expectedNode;
		}

		private bool DispatchSessionEvent(Action handlers, string eventName, int expectedSessionId, NodeBaseData expectedNode = null)
		{
			if (handlers == null)
			{
				return IsCurrentSession(expectedSessionId, expectedNode);
			}
			Delegate[] invocationList = handlers.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				Action action = (Action)invocationList[i];
				try
				{
					action();
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

		private bool DispatchSessionEvent<T>(Action<T> handlers, T value, string eventName, int expectedSessionId, NodeBaseData expectedNode = null)
		{
			if (handlers == null)
			{
				return IsCurrentSession(expectedSessionId, expectedNode);
			}
			Delegate[] invocationList = handlers.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				Action<T> action = (Action<T>)invocationList[i];
				try
				{
					action(value);
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

		private void HandleSessionSubscriberException(string eventName, Exception exception, int expectedSessionId, NodeBaseData expectedNode)
		{
			Debug.LogError((object)("[Dialogue] " + eventName + " 援щ룆???ㅽ뻾 以??덉쇅媛\u0080 諛쒖깮?덉뒿?덈떎."));
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
			Delegate[] invocationList = handlers.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				Action action = (Action)invocationList[i];
				try
				{
					action();
				}
				catch (Exception ex)
				{
					Debug.LogError((object)("[Dialogue] " + eventName + " 援щ룆???ㅽ뻾 以??덉쇅媛\u0080 諛쒖깮?덉뒿?덈떎."));
					Debug.LogException(ex);
				}
			}
		}

		private static void DispatchSafely<T>(Action<T> handlers, T value, string eventName)
		{
			if (handlers == null)
			{
				return;
			}
			Delegate[] invocationList = handlers.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				Action<T> action = (Action<T>)invocationList[i];
				try
				{
					action(value);
				}
				catch (Exception ex)
				{
					Debug.LogError((object)("[Dialogue] " + eventName + " 援щ룆???ㅽ뻾 以??덉쇅媛\u0080 諛쒖깮?덉뒿?덈떎."));
					Debug.LogException(ex);
				}
			}
		}

		private void ResetSessionStateForPlayMode()
		{
			ClearBlockingState();
			currentContainer = null;
			currentContext = null;
			currentNode = null;
			currentEntryId = null;
			npcPendingTask = null;
			activeSessionId = 0;
			LastEndReason = null;
			isFinishingConversation = false;
			sessionSequence++;
		}

		private static bool HasActionKey(string key)
		{
			return !string.IsNullOrWhiteSpace(key) && !string.Equals(key, "None", StringComparison.OrdinalIgnoreCase);
		}

		private bool TryMoveToNextNode(string currentGuid, string portName, bool allowImplicitCompletion = false)
		{
			List<NodeLinkData> list = currentContainer.NodeLinks?.Where((NodeLinkData edge) => edge != null && edge.BaseNodeGuid == currentGuid && edge.PortName == portName).ToList() ?? new List<NodeLinkData>();
			if (list.Count == 0)
			{
				if (allowImplicitCompletion)
				{
					FinishConversation(DialogueEndReason.Completed);
				}
				else
				{
					FaultConversation("[Dialogue] ?몃뱶 '" + currentGuid + "'??'" + portName + "' 異쒕젰??Next 留곹겕媛\u0080 ?놁뒿?덈떎.");
				}
				return false;
			}
			if (list.Count > 1)
			{
				FaultConversation("[Dialogue] ?몃뱶 '" + currentGuid + "'??'" + portName + "' 異쒕젰??" + $"留곹겕媛\u0080 {list.Count}媛??덉뒿?덈떎.");
				return false;
			}
			string targetGuid = list[0].TargetNodeGuid;
			if (string.IsNullOrWhiteSpace(targetGuid))
			{
				FaultConversation("[Dialogue] ?몃뱶 '" + currentGuid + "'??'" + portName + "' 留곹겕???\u0080??GUID媛\u0080 ?놁뒿?덈떎.");
				return false;
			}
			NodeBaseData nodeBaseData = currentContainer.Nodes?.FirstOrDefault((NodeBaseData node) => node != null && node.Guid == targetGuid);
			if (nodeBaseData == null)
			{
				FaultConversation("[Dialogue] 留곹겕 ?\u0080???몃뱶 '" + targetGuid + "'瑜?李얠쓣 ???놁뒿?덈떎.");
				return false;
			}
			currentNode = nodeBaseData;
			return true;
		}

		private void NextNode(string currentGuid, string portName, bool allowImplicitCompletion = false)
		{
			if (IsConversationActive && TryMoveToNextNode(currentGuid, portName, allowImplicitCompletion))
			{
				ProcessCurrentNode();
			}
		}

		private void AdvanceFromBlockingNode(int expectedSessionId, NodeBaseData expectedNode, string portName, bool allowImplicitCompletion)
		{
			if (IsCurrentSession(expectedSessionId, expectedNode))
			{
				string guid = expectedNode.Guid;
				ClearBlockingState();
				NextNode(guid, portName, allowImplicitCompletion);
			}
		}

		public void Tick(float scaledDeltaTime, float unscaledDeltaTime)
		{
			if (!IsConversationActive || blockKind != BlockKind.Time || currentNode == null)
			{
				return;
			}
			float num = (waitUsesUnscaledTime ? unscaledDeltaTime : scaledDeltaTime);
			if (!(num <= 0f) && !float.IsNaN(num) && !float.IsInfinity(num))
			{
				waitRemainingSeconds -= num;
				if (!(waitRemainingSeconds > 0f))
				{
					int expectedSessionId = activeSessionId;
					NodeBaseData expectedNode = currentNode;
					AdvanceFromBlockingNode(expectedSessionId, expectedNode, "Next", allowImplicitCompletion: false);
				}
			}
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
			if (IsConversationActive && blockKind == BlockKind.Signal && currentNode != null && string.Equals(waitingSignalKey, signalKey, StringComparison.Ordinal))
			{
				int expectedSessionId = activeSessionId;
				NodeBaseData expectedNode = currentNode;
				AdvanceFromBlockingNode(expectedSessionId, expectedNode, "Next", allowImplicitCompletion: false);
			}
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
		}

		public void ContinueNextLine()
		{
			if (IsConversationActive && blockKind == BlockKind.Line && currentNode is DialogueNodeData)
			{
				int expectedSessionId = activeSessionId;
				NodeBaseData expectedNode = currentNode;
				AdvanceFromBlockingNode(expectedSessionId, expectedNode, "Next", allowImplicitCompletion: true);
			}
		}

		public void OnSelectionChoice(DialogueChoiceData choice)
		{
			if (!IsConversationActive || blockKind != BlockKind.Choice || choice == null || currentNode == null || !(currentNode is DialogueNodeData dialogueNodeData))
			{
				return;
			}
			DialogueChoiceData dialogueChoiceData = dialogueNodeData.Choices.FirstOrDefault((DialogueChoiceData currentChoice) => currentChoice != null && currentChoice.PortName == choice.PortName);
			if (dialogueChoiceData == null)
			{
				Debug.LogWarning((object)"[Dialogue] ?꾩옱 ?몃뱶???녿뒗 ?좏깮吏\u0080媛\u0080 ?붿껌?섏뿀?듬땲??");
				return;
			}
			int num = activeSessionId;
			NodeBaseData expectedNode = currentNode;
			string guid = currentNode.Guid;
			ClearBlockingState();
			if (HasActionKey(dialogueChoiceData.ChoiceEventKey))
			{
				bool flag = DialogueEventRegistry.ExecuteAction(dialogueChoiceData.ChoiceEventKey, dialogueChoiceData.ChoiceEventArguments, dialogueChoiceData.ChoiceEventParam, currentContext);
				if (!IsCurrentSession(num, expectedNode))
				{
					return;
				}
				if (!flag)
				{
					FaultConversation("[Dialogue] ?몃뱶 '" + guid + "'??Choice Action '" + dialogueChoiceData.ChoiceEventKey + "'???ㅽ뻾?섏? 紐삵뻽?듬땲??");
					return;
				}
			}
			AdvanceFromBlockingNode(num, expectedNode, dialogueChoiceData.PortName, allowImplicitCompletion: true);
		}
	}
}





