using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>게임의 Controller가 제공하는 Condition 결과에 따라 Quest 흐름을 분기합니다.</summary>
    [Serializable]
    public class QuestConditionNodeData : NodeBaseData
    {
        /// <summary>Attribute가 붙은 Quest Condition에 전달할 타입 기반 인수입니다.</summary>
        [Tooltip("고정된 Condition 키입니다. 예: player.level, inventory.item-count 또는 프로젝트 정의 키")]
        public MethodBindingData Condition = new();
    }
}
