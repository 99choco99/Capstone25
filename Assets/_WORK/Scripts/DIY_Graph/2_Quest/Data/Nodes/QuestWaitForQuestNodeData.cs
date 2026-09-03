using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>다른 Quest가 지정한 상태가 될 때까지 현재 흐름을 멈춥니다.</summary>
    [Serializable]
    public class QuestWaitForQuestNodeData : NodeBaseData
    {
        [Tooltip("현재 흐름이 기다릴 Quest ID입니다.")]
        public int TargetQuestId;

        [Tooltip("대상 Quest가 이 상태가 되면 현재 흐름을 다시 진행합니다.")]
        public QuestState RequiredState = QuestState.TurnedIn;
    }
}
