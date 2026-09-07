using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UniversalGraph.Editor
{
    /// <summary>
    /// 검증할 때 사용할 노드와 링크의 정보들을 조회하기 위해 미리 정리해둔 클래스
    /// </summary>
    public sealed class GraphValidationIndex
    {
        public GraphContainer Container { get; }
        public IReadOnlyList<NodeBaseData> Nodes { get; }
        public IReadOnlyList<NodeLinkData> Links { get; }

        private readonly Dictionary<string, NodeBaseData> nodesByGuid = new();
        private readonly Dictionary<string, List<NodeLinkData>> startLinksByGuid = new();
        private readonly Dictionary<string, List<NodeLinkData>> targetLinksByGuid = new();


        /// <summary>구조 검사를 통과한 그래프의 노드와 링크로 dictionary 제작</summary>
        public GraphValidationIndex(GraphContainer container)
        {
            Container = container != null ? container : throw new ArgumentNullException(nameof(container), "검증할 GraphContainer가 필요합니다.");
            Nodes = container.Nodes;
            Links = container.NodeLinks;

            foreach (NodeBaseData node in Nodes)
            {
                nodesByGuid.Add(node.Guid, node);
            }

            foreach (NodeLinkData link in Links)
            {
                if (!startLinksByGuid.TryGetValue(link.StartNodeGuid, out List<NodeLinkData> Startlinks))
                {
                    Startlinks = new List<NodeLinkData>();
                    startLinksByGuid.Add(link.StartNodeGuid, Startlinks);
                }
                Startlinks.Add(link);

                if (!targetLinksByGuid.TryGetValue(link.TargetNodeGuid, out List<NodeLinkData> targetLinks))
                {
                    targetLinks = new List<NodeLinkData>();
                    targetLinksByGuid.Add(link.TargetNodeGuid, targetLinks);
                }
                targetLinks.Add(link);
            }
        }


        /// <summary>guid로 노드 데이터 가져오기</summary>
        public bool GetNodeData(string guid, out NodeBaseData node)
        {
            node = null;
            return !string.IsNullOrWhiteSpace(guid) && nodesByGuid.TryGetValue(guid, out node);
        }

        /// <summary>출발 포트랑 연결된 링크 정보를 다 반환</summary>
        public IReadOnlyList<NodeLinkData> GetLinkInStartPort(string nodeGuid, string portName = null)
        {
            if (!startLinksByGuid.TryGetValue(nodeGuid ?? string.Empty, out List<NodeLinkData> links))
            {
                return Array.Empty<NodeLinkData>();
            }

            return string.IsNullOrWhiteSpace(portName) ? links : links.Where(link => link.StartPortName == portName).ToArray();
        }

        /// <summary>노드의 진입 포트와 연결된 링크 정보들 다 반환</summary>
        public IReadOnlyList<NodeLinkData> GetLinkInTargetPorts(string nodeGuid)
        {
            return targetLinksByGuid.TryGetValue(nodeGuid ?? string.Empty, out List<NodeLinkData> links) ? links : Array.Empty<NodeLinkData>();
        }

        /// <summary>주어진 시작 노드들에서 도달 가능한 모든 유효 노드를 찾기 BFS 사용</summary>
        public HashSet<string> GetReachableNode(IEnumerable<string> rootGuids)
        {
            HashSet<string> reachedNode = new ();

            Queue<string> q = new();
            if (rootGuids != null)
            {
                foreach (string guid in rootGuids)
                {
                    if (GetNodeData(guid, out _))
                    {
                        q.Enqueue(guid);
                    }
                }
            }

            while (q.Count > 0)
            {
                string guid = q.Dequeue();
                if (!reachedNode.Add(guid))
                {
                    continue;
                }

                foreach (NodeLinkData link in GetLinkInStartPort(guid))
                {
                    q.Enqueue(link.TargetNodeGuid);
                }
            }

            return reachedNode;
        }


    }
}
