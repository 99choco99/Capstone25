using System;
using System.Collections.Generic;
using UnityEditor;

namespace UniversalGraph.Editor
{
    /// <summary>모든 그래프 에셋이 공유하는 기본 데이터 구조를 검사하는 클래스</summary>
    internal static class GraphStructureValidator
    {
        /// <summary>구조 문제를 문제 목록에 추가</summary>
        internal static void Validate(GraphContainer container, ICollection<GraphValidationIssue> issues)
        {
            //버전 확인
            if (container.SchemaVersion != GraphAssetMigrator.CurrentVersion)
            {
                AddError("GRAPH_SCHEMA_VERSION", container.SchemaVersion > GraphAssetMigrator.CurrentVersion ? 
                    $"지원하지 않는 그래프 버전: {container.SchemaVersion} (지원: {GraphAssetMigrator.CurrentVersion})" : 
                    $"그래프 버전 변환 필요: {container.SchemaVersion} → {GraphAssetMigrator.CurrentVersion}");
            }

            //null 검사
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

            //그래프 노드 검사
            HashSet<string> guids = new() ;
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
                    AddError("DUPLICATE_NODE_GUID", $"노드 GUID '{node.Guid}'가 중복되었습니다.", node.Guid);
                }
            }

            //노드 사이의 링크들 검사
            HashSet<string> edgeKeys = new();
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
                    AddError("MISSING_LINK_NODE", $"연결선이 존재하지 않는 노드를 참조합니다: {sourceGuid} -> {targetGuid}.", guids.Contains(sourceGuid) ? sourceGuid : null);
                }

                if (string.IsNullOrWhiteSpace(link.TargetPortName))
                {
                    bool requiresTargetPort = container.SchemaVersion >= GraphAssetMigrator.CurrentVersion;
                    issues.Add(new GraphValidationIssue(
                        requiresTargetPort ? GraphValidationSeverity.Error : GraphValidationSeverity.Warning,
                        requiresTargetPort ? "MISSING_TARGET_PORT" : "LEGACY_TARGET_PORT",
                        requiresTargetPort ? "연결선에 대상 입력 포트 ID가 없습니다." : "구형 연결선의 입력 포트 ID 변환이 필요합니다.",
                        sourceGuid));
                }
                //중복검사
                string edgeKey = $"{sourceGuid}\u001F{link.StartPortName}\u001F{targetGuid}\u001F{link.TargetPortName}";
                if (!edgeKeys.Add(edgeKey))
                {
                    AddError("DUPLICATE_LINK", $"연결선 {sourceGuid}.{link.StartPortName} -> {targetGuid}.{link.TargetPortName}이 중복되었습니다.", sourceGuid);
                }
            }

            void AddError(string issueKind, string message, string nodeGuid = null)
            {
                issues.Add(new GraphValidationIssue(GraphValidationSeverity.Error, issueKind, message, nodeGuid));
            }
        }
    }
}
