using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>노드에 도달하면 프로젝트에서 정의한 Quest Action을 실행합니다.</summary>
    [Serializable]
    public class QuestActionNodeData : NodeBaseData
    {
        /// <summary>Attribute가 붙은 Quest Action에 전달할 타입 기반 인수입니다.</summary>
        [Tooltip("프로젝트에서 정의한 Action 키입니다. 예: PlayCutscene_01")]
        public MethodBindingData Action = new();
    }
}
