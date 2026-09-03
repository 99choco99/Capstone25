using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>상호작용 목록에 현재 Quest를 수락 후보로 제공하는 조회 전용 종착 노드입니다.</summary>
    [Serializable]
    public sealed class QuestOfferNodeData : NodeBaseData
    {
        [Tooltip("Quest 선택 뒤 재생할 선택적인 Dialogue 그래프입니다. 비워 두면 UI에서 바로 수락할 수 있습니다.")]
		public DialogueEntryPoint DialogueEntryPoint;

        [Tooltip("여러 Quest 후보를 정렬하거나 자동 선택할 때 사용할 값입니다.")]
        public int Priority;

        [Tooltip("끄면 UI에 선택할 수 없는 Quest와 차단 이유를 제공할 수 있습니다.")]
        public bool IsAvailable = true;

        [Tooltip("수락할 수 없는 이유입니다. Is Available이 꺼진 경우에만 사용합니다.")]
        public string BlockReason;
    }
}
