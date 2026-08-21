using System;
using UnityEditor;
using UnityEngine;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>Creates a dialogue graph asset with one named default entry point.</summary>
    internal static class DialogueGraphMenu
    {
        [MenuItem("Window/Dialogue Graph/Create Dialogue Graph")]
        private static void CreateDialogueGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Dialogue Graph",
                "NewDialogue",
                "asset",
                "Choose a location for the dialogue graph asset.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var container = ScriptableObject.CreateInstance<DialogueContainer>();
            container.Nodes.Add(new StartNodeData
            {
                Guid = Guid.NewGuid().ToString(),
                Position = new Vector2(100f, 100f),
                EntryId = StartNodeData.DefaultEntryId
            });
            AssetDatabase.CreateAsset(container, path);
            AssetDatabase.SaveAssets();
            UniversalGraphWindow.Open(container);
        }
    }
}
