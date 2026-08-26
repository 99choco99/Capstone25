using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>
    /// Quest 그래프의 노드와 연결을 한 번 정리하여 실행 중 빠르게 찾기 위한 조회 데이터입니다.
    /// </summary>
    internal sealed class QuestGraphIndex
    {
        private readonly Dictionary<string, NodeBaseData> nodes = new();
        private readonly Dictionary<string, List<NodeLinkData>> outgoingLinks = new();
        private readonly Dictionary<(string SourceGuid, string PortName), List<NodeLinkData>> outgoingByPort = new();
        private readonly Dictionary<string, int> distinctIncomingSourceCounts = new();

        public IReadOnlyDictionary<string, NodeBaseData> Nodes => nodes;
        public IReadOnlyDictionary<string, List<NodeLinkData>> OutgoingLinks => outgoingLinks;
        public IReadOnlyDictionary<(string SourceGuid, string PortName), List<NodeLinkData>> OutgoingByPort => outgoingByPort;
        public IReadOnlyDictionary<string, int> DistinctIncomingSourceCounts => distinctIncomingSourceCounts;

        /// <summary>그래프 구조를 검사하면서 모든 런타임 조회 인덱스를 한 번에 만듭니다.</summary>
        public static bool TryCreate(
            QuestContainer container,
            out QuestGraphIndex index,
            out string error)
        {
            index = null;
            if (container == null)
            {
                error = "Quest 그래프가 null입니다.";
                return false;
            }

            if (container.Nodes == null)
            {
                error = $"'{container.name}'의 노드 목록이 null입니다.";
                return false;
            }

            if (container.NodeLinks == null)
            {
                error = $"'{container.name}'의 연결선 목록이 null입니다.";
                return false;
            }

            var created = new QuestGraphIndex();
            foreach (NodeBaseData node in container.Nodes)
            {
                if (node == null)
                {
                    error = $"'{container.name}'에 null 노드가 있습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(node.Guid))
                {
                    error = $"'{container.name}'의 {node.GetType().Name} 노드에 GUID가 없습니다.";
                    return false;
                }

                if (!created.nodes.TryAdd(node.Guid, node))
                {
                    error = $"'{container.name}'에 중복된 노드 GUID '{node.Guid}'가 있습니다.";
                    return false;
                }
            }

            var edgeKeys = new HashSet<(
                string SourceGuid,
                string SourcePort,
                string TargetGuid,
                string TargetPort)>();
            var incomingSources = new Dictionary<string, HashSet<string>>();

            foreach (NodeLinkData link in container.NodeLinks)
            {
                if (link == null)
                {
                    error = $"'{container.name}'에 null 연결선이 있습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(link.StartNodeGuid)
                    || string.IsNullOrWhiteSpace(link.TargetNodeGuid))
                {
                    error = $"'{container.name}'에 출발 또는 도착 노드 GUID가 없는 연결선이 있습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(link.StartPortName)
                    || string.IsNullOrWhiteSpace(link.TargetPortName))
                {
                    error = $"'{container.name}'에 출발 또는 도착 포트 ID가 없는 연결선이 있습니다.";
                    return false;
                }

                if (!created.nodes.ContainsKey(link.StartNodeGuid)
                    || !created.nodes.ContainsKey(link.TargetNodeGuid))
                {
                    error = $"'{container.name}'의 연결선이 존재하지 않는 노드를 참조합니다: " +
                            $"{link.StartNodeGuid} -> {link.TargetNodeGuid}.";
                    return false;
                }

                var edgeKey = (
                    link.StartNodeGuid,
                    link.StartPortName,
                    link.TargetNodeGuid,
                    link.TargetPortName);
                if (!edgeKeys.Add(edgeKey))
                {
                    error = $"'{container.name}'에 중복된 연결선이 있습니다: " +
                            $"{link.StartNodeGuid}.{link.StartPortName} -> " +
                            $"{link.TargetNodeGuid}.{link.TargetPortName}.";
                    return false;
                }

                AddLink(created.outgoingLinks, link.StartNodeGuid, link);
                AddLink(created.outgoingByPort, (link.StartNodeGuid, link.StartPortName), link);

                if (!incomingSources.TryGetValue(link.TargetNodeGuid, out HashSet<string> sources))
                {
                    sources = new HashSet<string>();
                    incomingSources.Add(link.TargetNodeGuid, sources);
                }

                sources.Add(link.StartNodeGuid);
            }

            foreach (KeyValuePair<string, HashSet<string>> pair in incomingSources)
            {
                created.distinctIncomingSourceCounts.Add(pair.Key, pair.Value.Count);
            }

            index = created;
            error = null;
            return true;
        }

        private static void AddLink<TKey>(
            IDictionary<TKey, List<NodeLinkData>> linksByKey,
            TKey key,
            NodeLinkData link)
        {
            if (!linksByKey.TryGetValue(key, out List<NodeLinkData> links))
            {
                links = new List<NodeLinkData>();
                linksByKey.Add(key, links);
            }

            links.Add(link);
        }
    }

    /// <summary>Quest 진행 시작점을 선택합니다.</summary>
    public static partial class QuestRunner
    {
        private static NodeBaseData ResolveStartNode(
            QuestContainer container,
            QuestGraphIndex index)
        {
            QuestStartNodeData[] starts = index.Nodes.Values.OfType<QuestStartNodeData>().ToArray();
            if (starts.Length == 1)
            {
                return starts[0];
            }

            if (starts.Length > 1)
            {
                Debug.LogError($"[Quest] '{container.name}'에 Quest Start 노드가 {starts.Length}개 있습니다.", container);
                return null;
            }

            // 전용 Quest Start 노드가 생기기 전에 만든 그래프를 위한 호환 처리입니다.
            QuestEventEntryNodeData[] legacyEntries = index.Nodes.Values.OfType<QuestEventEntryNodeData>().ToArray();
            if (legacyEntries.Length == 1)
            {
                Debug.LogWarning(
                    $"[Quest] '{container.name}'은 상호작용 진입점을 레거시 진행 시작점으로 사용합니다. " +
                    "다음에 그래프를 편집할 때 Quest Start 노드를 추가하세요.",
                    container);
                return legacyEntries[0];
            }

            return index.Nodes.Values.OfType<QuestObjectiveNodeData>().FirstOrDefault();
        }
    }
}
