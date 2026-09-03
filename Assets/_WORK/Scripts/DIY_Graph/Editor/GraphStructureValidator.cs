using System;
using System.Collections.Generic;
using UnityEditor;

namespace UniversalGraph.Editor
{
    /// <summary>모든 그래프 에셋이 공유하는 직렬화 구조와 연결 무결성을 검사합니다.</summary>
    internal static class GraphStructureValidator
    {
        /// <summary>공통 구조 문제를 전달받은 결과 목록에 추가합니다.</summary>
        internal static void Validate(
            GraphContainer container,
            ICollection<GraphValidationIssue> issues)
        {
            if (container.SchemaVersion != GraphAssetMigrator.CurrentVersion)
            {
                issues.Add(new GraphValidationIssue(
                    GraphValidationSeverity.Error,
                    "GRAPH_SCHEMA_VERSION",
                    container.SchemaVersion > GraphAssetMigrator.CurrentVersion
                        ? $"그래프 스키마 {container.SchemaVersion}이 지원 버전 " +
                          $"{GraphAssetMigrator.CurrentVersion}보다 높습니다. 이 에셋을 편집하기 전에 패키지를 업데이트하세요."
                        : $"그래프 스키마 {container.SchemaVersion}을 " +
                          $"{GraphAssetMigrator.CurrentVersion} 버전으로 마이그레이션해야 합니다."));
            }

            if (SerializationUtility.HasManagedReferencesWithMissingTypes(container))
            {
                AddError("MISSING_NODE_TYPE", "에셋에 C# 타입이 사라진 노드 데이터가 있습니다.");
            }

            if (container.Nodes == null)
            {
                AddError("NULL_NODE_LIST", "그래프 노드 목록이 null입니다.");
                return;
            }

            if (container.NodeLinks == null)
            {
                AddError("NULL_LINK_LIST", "그래프 연결선 목록이 null입니다.");
                return;
            }

            var guids = new HashSet<string>();
            foreach (NodeBaseData node in container.Nodes)
            {
                if (node == null)
                {
                    AddError("NULL_NODE", "그래프에 null 노드 항목이 있습니다.");
                }
                else if (string.IsNullOrWhiteSpace(node.Guid))
                {
                    AddError("EMPTY_NODE_GUID", $"{node.GetType().Name}에 고정 GUID가 없습니다.");
                }
                else if (!guids.Add(node.Guid))
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        "DUPLICATE_NODE_GUID",
                        $"노드 GUID '{node.Guid}'가 중복되었습니다.",
                        node.Guid));
                }
            }

            var edgeKeys = new HashSet<string>();
            foreach (NodeLinkData link in container.NodeLinks)
            {
                if (link == null)
                {
                    AddError("NULL_LINK", "그래프에 null 연결선 항목이 있습니다.");
                    continue;
                }

                string sourceGuid = link.StartNodeGuid;
                string targetGuid = link.TargetNodeGuid;
                if (string.IsNullOrWhiteSpace(sourceGuid)
                    || string.IsNullOrWhiteSpace(targetGuid)
                    || string.IsNullOrWhiteSpace(link.StartPortName))
                {
                    AddError("INCOMPLETE_LINK", "연결선에 출발 노드, 대상 노드 또는 출력 포트 ID가 없습니다.");
                    continue;
                }

                if (!guids.Contains(sourceGuid) || !guids.Contains(targetGuid))
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        "MISSING_LINK_NODE",
                        $"연결선이 존재하지 않는 노드를 참조합니다: {sourceGuid} -> {targetGuid}.",
                        guids.Contains(sourceGuid) ? sourceGuid : null));
                }

                if (string.IsNullOrWhiteSpace(link.TargetPortName))
                {
                    bool requiresTargetPort = container.SchemaVersion >= GraphAssetMigrator.CurrentVersion;
                    issues.Add(new GraphValidationIssue(
                        requiresTargetPort ? GraphValidationSeverity.Error : GraphValidationSeverity.Warning,
                        requiresTargetPort ? "MISSING_TARGET_PORT" : "LEGACY_TARGET_PORT",
                        requiresTargetPort
                            ? "연결선에 대상 입력 포트 ID가 없어 그래프를 불러올 수 없습니다."
                            : "레거시 연결선에 대상 포트 ID가 없습니다. 그래프를 마이그레이션하세요.",
                        sourceGuid));
                }

                string edgeKey = $"{sourceGuid}\u001F{link.StartPortName}\u001F{targetGuid}\u001F{link.TargetPortName}";
                if (!edgeKeys.Add(edgeKey))
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        "DUPLICATE_LINK",
                        $"연결선 {sourceGuid}.{link.StartPortName} -> {targetGuid}.{link.TargetPortName}이 중복되었습니다.",
                        sourceGuid));
                }
            }

            void AddError(string code, string message)
            {
                issues.Add(new GraphValidationIssue(GraphValidationSeverity.Error, code, message));
            }
        }
    }
}
