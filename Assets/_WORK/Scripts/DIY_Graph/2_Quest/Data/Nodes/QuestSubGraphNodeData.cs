using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>다른 Quest를 시작하고 해당 Quest가 완료될 때까지 현재 흐름을 멈춥니다.</summary>
    [Serializable]
    public class QuestSubGraphNodeData : NodeBaseData
    {
        [Tooltip("참조한 Quest가 완료되면 상위 Quest 흐름을 다시 진행합니다.")]
        public int SubQuestId;
    }
}
