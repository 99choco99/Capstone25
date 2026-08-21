using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Event-count objective used by the current legacy quest runner.</summary>
    [Serializable]
    public class QuestObjectiveNodeData : NodeBaseData
    {
        public string ObjectiveType;

        public int TargetId;

        [Tooltip("Optional authoring reference. Runtime matching currently uses TargetId.")]
        public UnityEngine.Object TargetPrefab;

        public int RequiredAmount;

        public string ObjectiveDescription;
    }
}
