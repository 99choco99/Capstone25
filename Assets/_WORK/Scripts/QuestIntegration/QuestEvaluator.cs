using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniversalGraph;
namespace UniversalGraph
{
    public static class QuestEvaluator
    {
        public static List<DialogueRequest> Evaluate(IEnumerable<QuestContainer> questGraphs, Player player, NPC npc)
        {
            List<DialogueRequest> results = new();

            if (questGraphs == null || player == null || npc == null)
            {
                return results;
            }

            foreach (var graph in questGraphs)
            {
                if (graph == null) continue;

                DialogueRequest request = EvaluateGraph(graph, player, npc);
                if (request != null)
                {
                    results.Add(request);
                }
            }

            return results;
        }

        private static DialogueRequest EvaluateGraph(QuestContainer graph, Player player, NPC npc)
        {
            if (graph.Nodes == null) return null;

            var entryNodes = graph.Nodes.OfType<QuestEventEntryNodeData>();
            QuestEventEntryNodeData validEntry = null;

            foreach (var entry in entryNodes)
            {
                string tId = entry.TargetId;
                if (string.IsNullOrWhiteSpace(tId) || 
                    tId.Equals("Any", StringComparison.OrdinalIgnoreCase) ||
                    tId == npc.id.ToString() || 
                    tId.Equals(npc.NPC_Name, StringComparison.OrdinalIgnoreCase))
                {
                    validEntry = entry;
                    break;
                }
            }

            if (validEntry == null) return null;
            return Traverse(graph, validEntry, player);
        }

        private static DialogueRequest Traverse(QuestContainer graph, NodeBaseData startNode, Player player)
        {
            NodeBaseData currentNode = startNode;
            int maxDepth = 100;

            while (currentNode != null && maxDepth-- > 0)
            {
                if (currentNode is DialogueRequestNodeData reqNode)
                {
                    return new DialogueRequest(reqNode.DialogueReference, reqNode.TopicName, reqNode.Priority, graph.name);
                }

                if (currentNode is QuestStateConditionNodeData stateNode)
                {
                    bool isMatch = false;
                    if (player.Quest != null && player.Quest.QuestProgress != null)
                    {
                        if (player.Quest.QuestProgress.TryGetValue(stateNode.QuestId, out QuestProgress progress))
                        {
                            isMatch = (progress.state == stateNode.TargetState);
                        }
                    }

                    string portName = isMatch ? "True" : "False";
                    currentNode = GetNextNode(graph, currentNode.Guid, portName);
                    continue;
                }

                if (currentNode is QuestEventEntryNodeData)
                {
                    currentNode = GetNextNode(graph, currentNode.Guid, "Next");
                    continue;
                }

                break;
            }

            if (maxDepth <= 0)
            {
                Debug.LogWarning($"[QuestEvaluator] Loop detected in graph '{graph.name}'.");
            }

            return null;
        }

        private static NodeBaseData GetNextNode(QuestContainer graph, string nodeGuid, string portName)
        {
            if (graph.NodeLinks == null) return null;
            var link = graph.NodeLinks.FirstOrDefault(l => l.BaseNodeGuid == nodeGuid && l.PortName == portName);
            if (link != null)
            {
                return graph.Nodes.FirstOrDefault(n => n.Guid == link.TargetNodeGuid);
            }
            return null;
        }
    }
}


