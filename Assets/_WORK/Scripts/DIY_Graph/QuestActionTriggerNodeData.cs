using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Emits a project-defined quest action identifier when the node is reached.</summary>
    [Serializable]
    public class QuestActionTriggerNodeData : NodeBaseData
    {
        [Tooltip("Project-defined action key, for example PlayCutscene_01.")]
        public string ActionId;
    }
}
