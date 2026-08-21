using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Waits for the configured number of incoming branches before continuing.</summary>
    [Serializable]
    public class QuestAndGateNodeData : NodeBaseData
    {
        [Tooltip("The number of incoming branches required before this gate continues.")]
        public int RequiredInputCount = 2;
    }
}
