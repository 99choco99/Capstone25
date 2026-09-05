using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniversalGraph.Editor
{
    /// <summary>배포 전 검사와 수동 QA에 사용하는 프로젝트 전체 그래프 검증 명령입니다.</summary>
    internal static class GraphValidationMenu
    {
        [MenuItem("Tools/Universal Graph/Migrate All Graph Assets")]
        private static void MigrateAllGraphs()
        {
            GraphContainer[] graphs = FindAllGraphAssets();
            GraphContainer[] outdated = graphs
                .Where(graph => graph.SchemaVersion < GraphAssetMigrator.CurrentVersion)
                .ToArray();
            if (outdated.Length > 0)
            {
                Undo.RegisterCompleteObjectUndo(outdated, "Migrate Universal Graph Assets");
            }

            int migratedCount = 0;
            int failureCount = 0;
            foreach (GraphContainer graph in graphs)
            {
                if (!GraphAssetMigrator.TryMigrate(
                        graph,
                        out GraphAssetMigrationResult result,
                        out string error))
                {
                    failureCount++;
                    Debug.LogError($"[Flow Graph] '{AssetDatabase.GetAssetPath(graph)}'을 마이그레이션하지 못했습니다: {error}", graph);
                    continue;
                }

                if (!result.Changed)
                {
                    continue;
                }

                migratedCount++;
                EditorUtility.SetDirty(graph);
                Debug.Log(
                    $"[Flow Graph] '{AssetDatabase.GetAssetPath(graph)}'을 스키마 {result.FromVersion}에서 " +
                    $"{result.ToVersion}(으)로 마이그레이션했습니다.",
                    graph);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Universal Graph 마이그레이션",
                $"그래프 에셋 {migratedCount}개를 마이그레이션했으며 {failureCount}개는 실패했습니다.",
                "확인");
        }

        [MenuItem("Tools/Universal Graph/Validate All Graph Assets")]
        private static void ValidateAllGraphs()
        {
            GraphContainer[] graphs = FindAllGraphAssets();
            int errorCount = 0;
            int warningCount = 0;

            try
            {
                for (int index = 0; index < graphs.Length; index++)
                {
                    GraphContainer graph = graphs[index];
                    EditorUtility.DisplayProgressBar(
                        "Universal Graph 검증",
                        graph.name,
                        (float)index / graphs.Length);

                    IReadOnlyList<GraphValidationIssue> issues = GraphValidatorRegistry.Validate(graph);
                    foreach (GraphValidationIssue issue in issues)
                    {
                        string message = $"[Flow Graph] '{AssetDatabase.GetAssetPath(graph)}' {issue}";
                        if (issue.Severity == GraphValidationSeverity.Error)
                        {
                            errorCount++;
                            Debug.LogError(message, graph);
                        }
                        else
                        {
                            warningCount++;
                            Debug.LogWarning(message, graph);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string summary = $"그래프 에셋 {graphs.Length}개 검증 완료: 오류 {errorCount}개, 경고 {warningCount}개.";
            EditorUtility.DisplayDialog("Universal Graph 검증", summary, "확인");
        }

        /// <summary>도메인 어셈블리를 직접 알지 않고도 모든 GraphContainer 실제 타입의 에셋을 찾습니다.</summary>
        private static GraphContainer[] FindAllGraphAssets()
        {
            var assetGuids = new HashSet<string>();
            IEnumerable<Type> containerTypes = TypeCache.GetTypesDerivedFrom<GraphContainer>()
                .Where(type => type.IsClass && !type.IsAbstract);
            foreach (Type containerType in containerTypes)
            {
                foreach (string guid in AssetDatabase.FindAssets($"t:{containerType.Name}"))
                {
                    assetGuids.Add(guid);
                }
            }

            return assetGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GraphContainer>)
                .Where(graph => graph != null)
                .OrderBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
