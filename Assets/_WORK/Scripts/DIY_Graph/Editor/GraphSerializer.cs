using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace UniversalGraph.Editor
{
    /// <summary>
    /// Translates between the visual GraphView canvas and a serializable <see cref="GraphContainer"/>.
    /// It performs structural validation but deliberately does not contain dialogue or quest domain rules.
    /// </summary>
    public static class GraphSerializer
    {
        /// <summary>
        /// Stores the current node positions, data, and explicit source/target port links in an asset.
        /// Data is assigned only after the complete canvas has been validated.
        /// </summary>
        public static void SaveGraphToMemory(UniversalGraphView view, GraphContainer container)
        {
            if (view == null || container == null)
            {
                return;
            }

            var nodes = view.nodes.OfType<GraphNode>().ToList();
            var links = new List<NodeLinkData>();
            var savedNodes = new List<NodeBaseData>();
            var guids = new HashSet<string>(StringComparer.Ordinal);

            foreach (GraphNode node in nodes)
            {
                if (node?.Data == null)
                {
                    throw new InvalidOperationException("A graph node has no backing node data.");
                }

                if (string.IsNullOrWhiteSpace(node.Data.Guid))
                {
                    throw new InvalidOperationException($"{node.Data.GetType().Name} has no node GUID.");
                }

                if (!guids.Add(node.Data.Guid))
                {
                    throw new InvalidOperationException($"Duplicate node GUID '{node.Data.Guid}' was found in the canvas.");
                }
            }

            foreach (Edge edge in view.edges.ToList())
            {
                if (!(edge?.output?.node is GraphNode sourceNode) || !(edge.input?.node is GraphNode targetNode))
                {
                    throw new InvalidOperationException("An edge is not connected to two graph nodes.");
                }

                if (string.IsNullOrWhiteSpace(edge.output.portName) || string.IsNullOrWhiteSpace(edge.input.portName))
                {
                    throw new InvalidOperationException("Every connected graph port must have a stable port name.");
                }

                links.Add(new NodeLinkData
                {
                    BaseNodeGuid = sourceNode.Data.Guid,
                    PortName = edge.output.portName,
                    TargetNodeGuid = targetNode.Data.Guid,
                    TargetPortName = edge.input.portName
                });
            }

            foreach (GraphNode node in nodes)
            {
                node.Data.Position = node.GetPosition().position;
                savedNodes.Add(node.Data);
            }

            container.NodeLinks = links;
            container.Nodes = savedNodes;
            EditorUtility.SetDirty(container);
        }

        /// <summary>
        /// Compatibility alias retained for existing editor extensions. New code should use
        /// <see cref="SaveGraphToMemory"/> because the serializer is graph-domain agnostic.
        /// </summary>
        public static void SaveDialogueGraphToMemory(UniversalGraphView view, GraphContainer container)
        {
            SaveGraphToMemory(view, container);
        }

        /// <summary>
        /// Rebuilds a canvas from serialized data. Validation and temporary construction finish before
        /// the existing view is cleared, so a malformed asset does not leave the window half-empty.
        /// </summary>
        public static void LoadGraph(UniversalGraphView view, GraphContainer container)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            ValidateContainerData(container);
            List<GraphNode> nodes = LoadNodes(container);
            List<Edge> edges = LoadEdges(nodes, container);

            ClearGraph(view);
            foreach (GraphNode node in nodes)
            {
                view.AddElement(node);
            }

            foreach (Edge edge in edges)
            {
                view.AddElement(edge);
            }
        }

        /// <summary>Removes all current nodes and edges after replacement data has been prepared.</summary>
        private static void ClearGraph(UniversalGraphView view)
        {
            foreach (Edge edge in view.edges.ToList())
            {
                view.RemoveElement(edge);
            }

            foreach (GraphNode node in view.nodes.OfType<GraphNode>().ToList())
            {
                view.RemoveElement(node);
            }
        }

        /// <summary>Creates and binds each visual node through the registered node-editor factory.</summary>
        private static List<GraphNode> LoadNodes(GraphContainer container)
        {
            var nodes = new List<GraphNode>();
            foreach (NodeBaseData data in container.Nodes)
            {
                GraphNode node;
                try
                {
                    node = GraphNodeEditorRegistry.CreateNode(container, data);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Could not create an editor node for '{data.GetType().FullName}'.", exception);
                }

                if (node == null)
                {
                    throw new InvalidOperationException($"No editor node is registered for '{data.GetType().FullName}'.");
                }

                var position = node.GetPosition();
                position.position = data.Position;
                node.SetPosition(position);
                nodes.Add(node);
            }

            return nodes;
        }

        /// <summary>Resolves serialized port names into connected GraphView edges.</summary>
        private static List<Edge> LoadEdges(List<GraphNode> nodes, GraphContainer container)
        {
            var nodesByGuid = nodes.ToDictionary(node => node.Data.Guid, StringComparer.Ordinal);
            var edges = new List<Edge>();
            var usedSingleOutputs = new HashSet<string>(StringComparer.Ordinal);
            var usedSingleInputs = new HashSet<string>(StringComparer.Ordinal);
            var uniqueEdges = new HashSet<string>(StringComparer.Ordinal);

            foreach (NodeLinkData link in container.NodeLinks)
            {
                GraphNode sourceNode = nodesByGuid[link.BaseNodeGuid];
                GraphNode targetNode = nodesByGuid[link.TargetNodeGuid];
                Port output = FindSinglePort(sourceNode.outputContainer.Children().OfType<Port>(), link.PortName, "output", link);
                Port input = FindInputPort(targetNode, link);

                string outputKey = $"{link.BaseNodeGuid}\u001F{link.PortName}";
                string inputKey = $"{link.TargetNodeGuid}\u001F{input.portName}";
                string edgeKey = $"{outputKey}\u001F{inputKey}";
                if (!uniqueEdges.Add(edgeKey))
                {
                    throw new InvalidOperationException($"Duplicate edge was found: {link.BaseNodeGuid}.{link.PortName} -> {link.TargetNodeGuid}.{input.portName}.");
                }

                if (output.capacity == Port.Capacity.Single && !usedSingleOutputs.Add(outputKey))
                {
                    throw new InvalidOperationException($"Output '{link.BaseNodeGuid}.{link.PortName}' allows only one connection.");
                }

                if (input.capacity == Port.Capacity.Single && !usedSingleInputs.Add(inputKey))
                {
                    throw new InvalidOperationException($"Input '{link.TargetNodeGuid}.{input.portName}' allows only one connection.");
                }

                var edge = new Edge
                {
                    output = output,
                    input = input
                };
                output.Connect(edge);
                input.Connect(edge);
                edges.Add(edge);
            }

            return edges;
        }

        private static Port FindInputPort(GraphNode targetNode, NodeLinkData link)
        {
            IEnumerable<Port> inputs = targetNode.inputContainer.Children().OfType<Port>();
            if (!string.IsNullOrWhiteSpace(link.TargetPortName))
            {
                return FindSinglePort(inputs, link.TargetPortName, "input", link);
            }

            Port[] legacyInputs = inputs.ToArray();
            if (legacyInputs.Length == 1)
            {
                return legacyInputs[0];
            }

            throw new InvalidOperationException(
                $"Legacy link to '{link.TargetNodeGuid}' has no target port name, but the node has {legacyInputs.Length} input ports. Reconnect this edge once in the graph editor.");
        }

        private static Port FindSinglePort(IEnumerable<Port> candidates, string portName, string direction, NodeLinkData link)
        {
            Port[] ports = candidates.Where(port => string.Equals(port.portName, portName, StringComparison.Ordinal)).ToArray();
            if (ports.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one {direction} port named '{portName}' for link '{link.BaseNodeGuid}' -> '{link.TargetNodeGuid}', but found {ports.Length}.");
            }

            return ports[0];
        }

        /// <summary>Validates data that can be checked without constructing editor views.</summary>
        private static void ValidateContainerData(GraphContainer container)
        {
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(container))
            {
                throw new InvalidOperationException($"'{container.name}' contains SerializeReference data whose concrete type is missing.");
            }

            if (container.Nodes == null)
            {
                throw new InvalidOperationException("Graph node list is null.");
            }

            if (container.NodeLinks == null)
            {
                throw new InvalidOperationException("Graph link list is null.");
            }

            var nodeGuids = new HashSet<string>(StringComparer.Ordinal);
            foreach (NodeBaseData node in container.Nodes)
            {
                if (node == null)
                {
                    throw new InvalidOperationException("Graph node list contains a null entry.");
                }

                if (string.IsNullOrWhiteSpace(node.Guid))
                {
                    throw new InvalidOperationException($"{node.GetType().Name} has no GUID.");
                }

                if (!nodeGuids.Add(node.Guid))
                {
                    throw new InvalidOperationException($"Duplicate node GUID '{node.Guid}' was found in the asset.");
                }
            }

            foreach (NodeLinkData link in container.NodeLinks)
            {
                if (link == null)
                {
                    throw new InvalidOperationException("Graph link list contains a null entry.");
                }

                if (string.IsNullOrWhiteSpace(link.BaseNodeGuid)
                    || string.IsNullOrWhiteSpace(link.TargetNodeGuid)
                    || string.IsNullOrWhiteSpace(link.PortName))
                {
                    throw new InvalidOperationException("A link is missing a source GUID, target GUID, or source port name.");
                }

                if (!nodeGuids.Contains(link.BaseNodeGuid) || !nodeGuids.Contains(link.TargetNodeGuid))
                {
                    throw new InvalidOperationException($"Link references a missing node: {link.BaseNodeGuid} -> {link.TargetNodeGuid}.");
                }
            }
        }
    }
}
