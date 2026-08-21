using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>
    /// Quest definition asset. It currently contains both graph data and legacy list-based metadata;
    /// those two execution models should be separated before this is released as a reusable package.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuestGraph", menuName = "Universal/Quest Graph")]
    public class QuestContainer : GraphContainer
    {
        [Header("Quest Identity")]
        public int id;

        public string questName = "New Quest";

        [TextArea(3, 5)]
        public string description;

        public int requiredLevel;

        [Header("Legacy Metadata (migration only)")]
        public List<int> prerequisiteQuestIds = new List<int>();

        public int startNPCId;

        public int turnInNPCId;

        public List<QuestObjective> objectives = new List<QuestObjective>();

        public QuestReward reward;

        /// <summary>Compatibility alias used by the current quest runner and integration layer.</summary>
        public int questId => id;
    }
}
