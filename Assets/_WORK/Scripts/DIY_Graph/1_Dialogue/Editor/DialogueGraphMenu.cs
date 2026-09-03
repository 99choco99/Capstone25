using System;
using UnityEditor;
using UnityEngine;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>이름이 있는 기본 시작점 하나를 포함한 Dialogue 그래프 에셋을 만듭니다.</summary>
    internal static class DialogueGraphMenu
    {
        [MenuItem("Assets/Create/Universal/Dialogue Graph")]
        private static void CreateDialogueGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Draw Dialogue Graph",
                "NewDialogue",
                "asset",
                "Choose a location for the dialogue graph asset.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var container = ScriptableObject.CreateInstance<DialogueContainer>();
            GraphAssetMigrator.EnsureCurrent(container);
            container.Nodes.Add(new DialogueEntryNodeData
            {
                Guid = Guid.NewGuid().ToString(),
                Position = new Vector2(100f, 100f),
                EntryId = DialogueEntryNodeData.DefaultEntryId
            });
            AssetDatabase.CreateAsset(container, path);
            AssetDatabase.SaveAssets();
            UniversalGraphWindow.OpenWindow(container);
        }
    }
}
