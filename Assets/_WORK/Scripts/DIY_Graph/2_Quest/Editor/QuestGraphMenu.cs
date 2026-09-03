using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>고유 ID와 필수 진행 시작점을 가진 Quest 그래프를 만듭니다.</summary>
    internal static class QuestGraphMenu
    {
        [MenuItem("Assets/Create/Universal/Quest Graph")]
        private static void CreateQuestGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Quest Graph",
                "NewQuest",
                "asset",
                "Choose a location for the quest graph asset.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            int nextQuestId = QuestAssetIndex.Quests
                .Where(quest => quest != null && quest.QuestId > 0)
                .Select(quest => quest.QuestId)
                .DefaultIfEmpty(0)
                .Max() + 1;
            var container = ScriptableObject.CreateInstance<QuestContainer>();
            GraphAssetMigrator.EnsureCurrent(container);
            container.QuestId = nextQuestId;
            container.questName = System.IO.Path.GetFileNameWithoutExtension(path);
            container.Nodes.Add(new QuestStartNodeData
            {
                Guid = Guid.NewGuid().ToString(),
                Position = new Vector2(100f, 100f)
            });

            AssetDatabase.CreateAsset(container, path);
            AssetDatabase.SaveAssets();
            UniversalGraphWindow.OpenWindow(container);
        }
    }
}
