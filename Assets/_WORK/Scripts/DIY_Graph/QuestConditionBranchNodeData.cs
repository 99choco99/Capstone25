using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Quest-flow branch data. Runtime condition evaluation is not implemented yet.</summary>
    [Serializable]
    public class QuestConditionBranchNodeData : NodeBaseData
    {
        [Tooltip("Condition category, such as Level, Gold, Item, or a project-defined key.")]
        public string ConditionType;

        public int TargetId;

        public int RequiredValue;
    }
}
