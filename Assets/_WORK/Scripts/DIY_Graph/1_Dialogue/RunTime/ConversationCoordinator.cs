using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalGraph
{
	public class ConversationCoordinator : MonoBehaviour
	{
		public static ConversationCoordinator Instance { get; private set; }

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				UnityEngine.Object.Destroy(((Component)this).gameObject);
			}
			else
			{
				Instance = this;
			}
		}

		public void HandleInteraction(IEnumerable<DialogueRequest> requests, DialogueContext context, DialogueReference? defaultReference = null, Action onComplete = null)
		{
			if (requests == null)
			{
				ExecuteDefaultOrEnd(defaultReference, context, onComplete);
				return;
			}
			List<DialogueRequest> list = (from r in requests
				where r != null && (object)r.Reference.GraphAsset != (object)null
				orderby r.Priority descending
				select r).ToList();
			if (list.Count == 0)
			{
				ExecuteDefaultOrEnd(defaultReference, context, onComplete);
				return;
			}
			if (list.Count == 1)
			{
				StartDialogue(list[0].Reference, context, onComplete);
				return;
			}
			Debug.Log((object)$"[ConversationCoordinator] 寃뱀튂???\u0080???꾨낫媛\u0080 {list.Count}媛?議댁옱?⑸땲??");
			for (int i = 0; i < list.Count; i++)
			{
				Debug.Log((object)$" - {i + 1}: [{list[i].Priority}] {list[i].TopicName} (from {list[i].SourceQuestId})");
			}
			Debug.Log((object)"[ConversationCoordinator] 媛\u0080???곗꽑?쒖쐞媛\u0080 ?믪? ?\u0080?붾? ?꾩떆濡??먮룞 ?ㅽ뻾?⑸땲??");
			StartDialogue(list[0].Reference, context, onComplete);
		}

		private void ExecuteDefaultOrEnd(DialogueReference? defaultReference, DialogueContext context, Action onComplete)
		{
			if (defaultReference.HasValue && (object)defaultReference.Value.GraphAsset != (object)null)
			{
				Debug.Log((object)"[ConversationCoordinator] ?좏슚???섏뒪???\u0080?붽? ?놁뼱 湲곕낯(Default) ?\u0080?붾? ?ㅽ뻾?⑸땲??");
				StartDialogue(defaultReference.Value, context, onComplete);
			}
			else
			{
				Debug.Log((object)"[ConversationCoordinator] ?ㅽ뻾??湲곕낯 ?\u0080?붽? ?놁뒿?덈떎.");
				onComplete?.Invoke();
			}
		}

		private void StartDialogue(DialogueReference reference, DialogueContext context, Action onComplete)
		{
			Debug.Log((object)("[ConversationCoordinator] ?\u0080???쒖옉 ?붿껌: " + ((UnityEngine.Object)reference.GraphAsset).name + " (Entry: " + reference.EntryId + ")"));
			if (DialogueManager.Instance != null)
			{
				DialogueManager.Instance.TryStartConversation(reference.GraphAsset, reference.EntryId, context, onComplete);
			}
			else
			{
				Debug.LogWarning((object)"[ConversationCoordinator] DialogueManager ?몄뒪?댁뒪瑜?李얠쓣 ???놁뒿?덈떎.");
			}
		}
	}
}





