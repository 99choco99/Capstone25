using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>
    /// 상호작용에 들어온 대화 요청 중 하나를 선택해 시작하는 선택적 씬 연결 컴포넌트입니다.
    /// 별도 대화 UI를 사용하는 게임은 이 컴포넌트 없이 <see cref="DialogueManager"/>를 직접 호출할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConversationCoordinator : MonoBehaviour
    {
        public static ConversationCoordinator Instance { get; private set; }

        /// <summary>
        /// 여러 요청이 겹쳤을 때 사용할 선택 규칙입니다. 기본값은 우선순위가 가장 높은 요청을 고릅니다.
        /// 게임에서 주제 선택 UI 또는 프로젝트 전용 규칙으로 교체할 수 있습니다.
        /// </summary>
        public Func<IReadOnlyList<DialogueRequest>, DialogueRequest> RequestSelector { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 잘못된 요청을 제외하고 우선순위순으로 정렬해 하나를 선택하며, 없으면 기본 대화를 사용합니다.
        /// </summary>
        public void HandleInteraction(
            IEnumerable<DialogueRequest> requests,
            DialogueContext context,
            DialogueReference? defaultReference = null,
            Action onComplete = null)
        {
            List<DialogueRequest> candidates = requests?
                .Where(request => request?.Reference.GraphAsset != null)
                .OrderByDescending(request => request.Priority)
                .ThenBy(request => request.TopicName, StringComparer.Ordinal)
                .ToList() ?? new List<DialogueRequest>();

            if (candidates.Count == 0)
            {
                ExecuteDefaultOrComplete(defaultReference, context, onComplete);
                return;
            }

            DialogueRequest selected = RequestSelector?.Invoke(candidates) ?? candidates[0];
            if (selected == null || !candidates.Contains(selected))
            {
                Debug.LogWarning("[Dialogue] 요청 선택기가 올바르지 않은 요청을 반환하여 우선순위가 가장 높은 요청을 사용합니다.");
                selected = candidates[0];
            }

            StartDialogue(selected.Reference, context, onComplete);
        }

        private static void ExecuteDefaultOrComplete(
            DialogueReference? defaultReference,
            DialogueContext context,
            Action onComplete)
        {
            if (defaultReference.HasValue && defaultReference.Value.GraphAsset != null)
            {
                StartDialogue(defaultReference.Value, context, onComplete);
                return;
            }

            onComplete?.Invoke();
        }

        private static void StartDialogue(
            DialogueReference reference,
            DialogueContext context,
            Action onComplete)
        {
            DialogueManager.Instance.TryStartConversation(
                reference.GraphAsset,
                reference.EntryId,
                context,
                onComplete);
        }
    }
}
