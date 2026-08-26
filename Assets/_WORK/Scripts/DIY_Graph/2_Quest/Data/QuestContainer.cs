using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>
    /// Quest 정의 에셋입니다. 목표와 흐름은 그래프 노드에 저장하고, 나머지 정보는
    /// 화면 표시, 선행 조건, 담당 NPC와 보상을 설명합니다.
    /// </summary>
    public class QuestContainer : GraphContainer
    {
        [Header("Quest Identity")]
        public int id;

        public string questName = "New Quest";

        [TextArea(3, 5)]
        public string description;

        public int requiredLevel;

        [Header("Quest Rules")]
        public List<int> prerequisiteQuestIds = new List<int>();

        public int startNPCId;

        public int turnInNPCId;

        public QuestReward reward;

        /// <summary>읽기 쉬운 API 이름으로 공개한 Quest 고정 ID입니다.</summary>
        public int questId => id;
    }
}
