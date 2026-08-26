using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>일치하는 게임 이벤트의 누적 수치가 설정한 목표량에 도달할 때까지 기다립니다.</summary>
    [Serializable]
    public class QuestObjectiveNodeData : NodeBaseData
    {
        public string ObjectiveType;

        [Tooltip("게임 플레이 이벤트와 비교할 프로젝트 정의 대상 ID입니다.")]
        public int TargetId;

        [Tooltip("선택적인 제작용 참조입니다. 현재 런타임 비교에는 TargetId를 사용합니다.")]
        public UnityEngine.Object TargetPrefab;

        [Min(1)]
        public int RequiredAmount = 1;

        [TextArea]
        public string ObjectiveDescription;
    }
}
