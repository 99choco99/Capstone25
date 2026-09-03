using System.Collections.Generic;

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
            foreach (NodeBaseData nodeData in container.Nodes)
            {
                if (nodeData == null)
                {
                    error = $"'{container.name}'에 null 노드가 있습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(nodeData.Guid))
                {
                    error = $"'{container.name}'의 {nodeData.GetType().Name} 노드에 GUID가 없습니다.";
                    return false;
                }

                if (!created.nodes.TryAdd(nodeData.Guid, nodeData))
                {
                    error = $"'{container.name}'에 중복된 노드 GUID '{nodeData.Guid}'가 있습니다.";
                    return false;
                }

                if (nodeData is QuestActionNodeData action && action.Action == null)
                {
                    error = $"'{container.name}'의 Action 노드 '{nodeData.Guid}'에 호출 정보가 없습니다.";
                    return false;
                }

                if (nodeData is QuestConditionNodeData condition && condition.Condition == null)
                {
                    error = $"'{container.name}'의 Condition 노드 '{nodeData.Guid}'에 호출 정보가 없습니다.";
                    return false;
                }

                if (nodeData is QuestRewardNodeData reward && reward.RewardAction == null)
                {
                    error = $"'{container.name}'의 Reward 노드 '{nodeData.Guid}'에 호출 정보가 없습니다.";
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
}
