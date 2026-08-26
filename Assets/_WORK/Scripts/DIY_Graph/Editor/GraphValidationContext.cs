using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalGraph.Editor
{
    /// <summary>
    /// 도메인 검증기에 제공하는 읽기 전용 그래프 인덱스입니다. 잘못된 항목은 원본 목록에는 남기되
    /// 인덱스에서는 제외하여, 한 번의 검사로 여러 문제를 함께 보고할 수 있게 합니다.
    /// </summary>
    public sealed class GraphValidationContext
    {
        private readonly Dictionary<string, NodeBaseData> nodesByGuid = new();
        private readonly Dictionary<string, List<NodeLinkData>> outgoingByGuid = new();
        private readonly Dictionary<string, List<NodeLinkData>> incomingByGuid = new();

        public GraphValidationContext(GraphContainer container)
        {
            Container = container ?? throw new ArgumentNullException(nameof(container), "검증할 GraphContainer가 필요합니다.");
            Nodes = container.Nodes ?? new List<NodeBaseData>();
            Links = container.NodeLinks ?? new List<NodeLinkData>();

            foreach (NodeBaseData node in Nodes)
            {
                if (node != null && !string.IsNullOrWhiteSpace(node.Guid))
                {
                    nodesByGuid.TryAdd(node.Guid, node);
                }
            }

            foreach (NodeLinkData link in Links)
            {
                if (link == null)
                {
                    continue;
                }

                AddLink(outgoingByGuid, link.StartNodeGuid, link);
                AddLink(incomingByGuid, link.TargetNodeGuid, link);
            }
        }

        public GraphContainer Container { get; }
        public IReadOnlyList<NodeBaseData> Nodes { get; }
        public IReadOnlyList<NodeLinkData> Links { get; }

        /// <summary>고정 GUID로 유효한 노드 데이터를 찾습니다.</summary>
        public bool TryGetNode(string guid, out NodeBaseData node)
        {
            node = null;
            return !string.IsNullOrWhiteSpace(guid) && nodesByGuid.TryGetValue(guid, out node);
        }

        /// <summary>출발 연결을 반환하며, 필요하면 특정 출력 포트로 제한합니다.</summary>
        public IReadOnlyList<NodeLinkData> GetOutgoing(string nodeGuid, string portName = null)
        {
            if (!outgoingByGuid.TryGetValue(nodeGuid ?? string.Empty, out List<NodeLinkData> links))
            {
                return Array.Empty<NodeLinkData>();
            }

            return string.IsNullOrWhiteSpace(portName)
                ? links
                : links.Where(link => link.StartPortName == portName).ToArray();
        }

        /// <summary>주어진 노드로 들어오는 모든 연결을 반환합니다.</summary>
        public IReadOnlyList<NodeLinkData> GetIncoming(string nodeGuid)
        {
            return incomingByGuid.TryGetValue(nodeGuid ?? string.Empty, out List<NodeLinkData> links)
                ? links
                : Array.Empty<NodeLinkData>();
        }

        /// <summary>주어진 시작 노드들에서 도달 가능한 모든 유효 노드를 찾습니다.</summary>
        public HashSet<string> GetReachableNodeGuids(IEnumerable<string> rootGuids)
        {
            var reachable = new HashSet<string>();
            var pending = new Queue<string>(rootGuids?.Where(guid => !string.IsNullOrWhiteSpace(guid))
                                            ?? Enumerable.Empty<string>());
            while (pending.Count > 0)
            {
                string guid = pending.Dequeue();
                if (!reachable.Add(guid))
                {
                    continue;
                }

                foreach (NodeLinkData link in GetOutgoing(guid))
                {
                    if (TryGetNode(link.TargetNodeGuid, out _))
                    {
                        pending.Enqueue(link.TargetNodeGuid);
                    }
                }
            }

            return reachable;
        }

        private static void AddLink(
            IDictionary<string, List<NodeLinkData>> index,
            string guid,
            NodeLinkData link)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            if (!index.TryGetValue(guid, out List<NodeLinkData> links))
            {
                links = new List<NodeLinkData>();
                index.Add(guid, links);
            }

            links.Add(link);
        }
    }
}
