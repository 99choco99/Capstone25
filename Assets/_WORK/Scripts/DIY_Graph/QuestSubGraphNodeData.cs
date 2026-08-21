using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Starts another quest and blocks this flow until that quest completes.</summary>
    [Serializable]
    public class QuestSubGraphNodeData : NodeBaseData
    {
        [Tooltip("The parent quest flow resumes after the referenced quest completes.")]
        public int SubQuestId;
    }
}
