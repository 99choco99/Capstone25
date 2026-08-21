using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UniversalGraph.Editor
{
	public static class GraphSerializer
	{
		public static void SaveGraphToMemory(UniversalGraphView view, GraphContainer container)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected O, but got Unknown
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Expected O, but got Unknown
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Expected O, but got Unknown
			//IL_0260: Unknown result type (might be due to invalid IL or missing references)
			//IL_0265: Unknown result type (might be due to invalid IL or missing references)
			//IL_026b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0270: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b7: Expected O, but got Unknown
			if (view == null || (Object)container == (Object)null)
			{
				return;
			}
			List<NodeLinkData> list = new List<NodeLinkData>();
			List<NodeBaseData> list2 = new List<NodeBaseData>();
			List<Edge> list3 = GraphViewExtensions.GetEdges((GraphView)view).ToList();
			List<GraphNode> list4 = GraphViewExtensions.GetNodes<GraphNode>((GraphView)view).ToList();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (GraphNode item in list4)
			{
				if (item?.Data == null)
				{
					throw new InvalidOperationException("?\u0080?ν븷 GraphNode??NodeData媛\u0080 ?놁뒿?덈떎.");
				}
				if (string.IsNullOrWhiteSpace(item.Data.Guid))
				{
					throw new InvalidOperationException("?\u0080?ν븷 NodeData??GUID媛\u0080 ?놁뒿?덈떎.");
				}
				if (!hashSet.Add(item.Data.Guid))
				{
					throw new InvalidOperationException("以묐났 Node GUID '" + item.Data.Guid + "'媛\u0080 ?덉뒿?덈떎.");
				}
			}
			foreach (Edge item2 in list3)
			{
				object obj;
				if (item2 == null)
				{
					obj = null;
				}
				else
				{
					Port output = item2.output;
					obj = ((output != null) ? output.node : null);
				}
				if (obj is GraphNode graphNode)
				{
					Port input = item2.input;
					if (((input != null) ? input.node : null) is GraphNode graphNode2)
					{
						if (string.IsNullOrWhiteSpace(item2.output.portName))
						{
							throw new InvalidOperationException("'" + graphNode.Data.Guid + "' ?몃뱶??異쒕젰 Port ID媛\u0080 鍮꾩뼱 ?덉뒿?덈떎.");
						}
						list.Add(new NodeLinkData
						{
							BaseNodeGuid = graphNode.Data.Guid,
							PortName = item2.output.portName,
							TargetNodeGuid = graphNode2.Data.Guid
						});
						continue;
					}
				}
				throw new InvalidOperationException("?쒖옉 ?먮뒗 ?꾩갑 ?몃뱶媛\u0080 ?녿뒗 Edge媛\u0080 ?덉뒿?덈떎.");
			}
			foreach (GraphNode item3 in list4)
			{
				NodeBaseData data = item3.Data;
				Rect position = ((GraphElement)item3).GetPosition();
				data.Position = ((Rect)(ref position)).position;
				list2.Add(item3.Data);
			}
			container.NodeLinks = list;
			container.Nodes = list2;
			EditorUtility.SetDirty((Object)container);
		}

		public static void SaveDialogueGraphToMemory(UniversalGraphView view, GraphContainer container)
		{
			SaveGraphToMemory(view, container);
		}

		public static void LoadGraph(UniversalGraphView view, GraphContainer container)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected O, but got Unknown
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Expected O, but got Unknown
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Expected O, but got Unknown
			if (view == null)
			{
				throw new ArgumentNullException("view");
			}
			if ((Object)container == (Object)null)
			{
				throw new ArgumentNullException("container");
			}
			ValidateContainerData(container);
			List<GraphNode> list = LoadNodes(container);
			List<Edge> list2 = LoadEdges(list, container);
			ClearGraph(view);
			foreach (GraphNode item in list)
			{
				((GraphView)view).AddElement((GraphElement)item);
			}
			foreach (Edge item2 in list2)
			{
				((GraphView)view).AddElement((GraphElement)item2);
			}
		}

		private static void ClearGraph(UniversalGraphView view)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Expected O, but got Unknown
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Expected O, but got Unknown
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Expected O, but got Unknown
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Expected O, but got Unknown
			foreach (Edge item in GraphViewExtensions.GetEdges((GraphView)view).ToList())
			{
				((GraphView)view).RemoveElement((GraphElement)item);
			}
			foreach (GraphNode item2 in GraphViewExtensions.GetNodes<GraphNode>((GraphView)view).ToList())
			{
				((GraphView)view).RemoveElement((GraphElement)item2);
			}
		}

		private static List<GraphNode> LoadNodes(GraphContainer container)
		{
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			List<GraphNode> list = new List<GraphNode>();
			foreach (NodeBaseData node in container.Nodes)
			{
				GraphNode graphNode;
				try
				{
					graphNode = GraphNodeEditorRegistry.CreateNode(container, node);
				}
				catch (Exception innerException)
				{
					throw new InvalidOperationException("'" + node.GetType().FullName + "' ?몃뱶 View瑜??앹꽦?섏? 紐삵뻽?듬땲??", innerException);
				}
				if (graphNode == null)
				{
					throw new InvalidOperationException("'" + node.GetType().FullName + "'???깅줉??GraphNode Editor媛\u0080 ?놁뒿?덈떎.");
				}
				Rect position = ((GraphElement)graphNode).GetPosition();
				((Rect)(ref position)).position = node.Position;
				((GraphElement)graphNode).SetPosition(position);
				list.Add(graphNode);
			}
			return list;
		}

		private static List<Edge> LoadEdges(List<GraphNode> nodes, GraphContainer container)
		{
			//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0257: Unknown result type (might be due to invalid IL or missing references)
			//IL_0294: Unknown result type (might be due to invalid IL or missing references)
			//IL_0299: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ad: Expected O, but got Unknown
			List<Edge> list = new List<Edge>();
			Dictionary<string, GraphNode> dictionary = nodes.ToDictionary((GraphNode n) => n.Data.Guid, StringComparer.Ordinal);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> hashSet2 = new HashSet<string>(StringComparer.Ordinal);
			foreach (NodeLinkData nodeLink in container.NodeLinks)
			{
				GraphNode graphNode = dictionary[nodeLink.BaseNodeGuid];
				GraphNode graphNode2 = dictionary[nodeLink.TargetNodeGuid];
				Port[] array = (from port in ((Node)graphNode).outputContainer.Children().OfType<Port>()
					where port.portName == nodeLink.PortName
					select port).ToArray();
				Port[] array2 = (from port in ((Node)graphNode2).inputContainer.Children().OfType<Port>()
					where (int)port.direction == 0
					select port).ToArray();
				if (array.Length != 1)
				{
					throw new InvalidOperationException("'" + nodeLink.BaseNodeGuid + "' ?몃뱶?먯꽌 異쒕젰 Port '" + nodeLink.PortName + "'???뺥솗???섎굹 李얠븘???섏?留?" + $"{array.Length}媛쒖엯?덈떎.");
				}
				if (array2.Length != 1)
				{
					throw new InvalidOperationException("'" + nodeLink.TargetNodeGuid + "' ?몃뱶???낅젰 Port???뺥솗???섎굹?ъ빞 ?섏?留?" + $"{array2.Length}媛쒖엯?덈떎.");
				}
				Port val = array[0];
				Port val2 = array2[0];
				string item = nodeLink.BaseNodeGuid + "\u001f" + nodeLink.PortName;
				if ((int)val.capacity == 0 && !hashSet.Add(item))
				{
					throw new InvalidOperationException("'" + nodeLink.BaseNodeGuid + "' ?몃뱶??'" + nodeLink.PortName + "' Single 異쒕젰??Edge媛\u0080 ???댁긽 ?\u0080?λ릺???덉뒿?덈떎.");
				}
				string targetNodeGuid = nodeLink.TargetNodeGuid;
				if ((int)val2.capacity == 0 && !hashSet2.Add(targetNodeGuid))
				{
					throw new InvalidOperationException("'" + nodeLink.TargetNodeGuid + "' ?몃뱶??Single ?낅젰??Edge媛\u0080 ???댁긽 ?\u0080?λ릺???덉뒿?덈떎.");
				}
				Edge val3 = new Edge
				{
					output = val,
					input = val2
				};
				val2.Connect(val3);
				val.Connect(val3);
				list.Add(val3);
			}
			return list;
		}

		private static void ValidateContainerData(GraphContainer container)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Expected O, but got Unknown
			if (SerializationUtility.HasManagedReferencesWithMissingTypes((Object)container))
			{
				throw new InvalidOperationException("'" + ((Object)container).name + "' 洹몃옒?꾩뿉 遺덈윭?????녿뒗 SerializeReference ?\u0080?낆씠 ?덉뒿?덈떎. ?먮낯 ?곗씠?곕? 吏\u0080?곗? 留먭퀬 ?꾨씫??NodeData ?\u0080???먮뒗 留덉씠洹몃젅?댁뀡??蹂듦뎄?섏꽭??");
			}
			if (container.Nodes == null)
			{
				throw new InvalidOperationException("Nodes 紐⑸줉??null?낅땲??");
			}
			if (container.NodeLinks == null)
			{
				throw new InvalidOperationException("NodeLinks 紐⑸줉??null?낅땲??");
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (NodeBaseData node in container.Nodes)
			{
				if (node == null)
				{
					throw new InvalidOperationException("Nodes 紐⑸줉??null ?곗씠?곌? ?덉뒿?덈떎.");
				}
				if (string.IsNullOrWhiteSpace(node.Guid))
				{
					throw new InvalidOperationException("GUID媛\u0080 ?녿뒗 NodeData媛\u0080 ?덉뒿?덈떎.");
				}
				if (!hashSet.Add(node.Guid))
				{
					throw new InvalidOperationException("以묐났 Node GUID '" + node.Guid + "'媛\u0080 ?덉뒿?덈떎.");
				}
			}
			foreach (NodeLinkData nodeLink in container.NodeLinks)
			{
				if (nodeLink == null)
				{
					throw new InvalidOperationException("NodeLinks 紐⑸줉??null ?곗씠?곌? ?덉뒿?덈떎.");
				}
				if (string.IsNullOrWhiteSpace(nodeLink.BaseNodeGuid) || string.IsNullOrWhiteSpace(nodeLink.TargetNodeGuid) || string.IsNullOrWhiteSpace(nodeLink.PortName))
				{
					throw new InvalidOperationException("GUID ?먮뒗 Port ID媛\u0080 鍮꾩뼱 ?덈뒗 Edge媛\u0080 ?덉뒿?덈떎.");
				}
				if (!hashSet.Contains(nodeLink.BaseNodeGuid) || !hashSet.Contains(nodeLink.TargetNodeGuid))
				{
					throw new InvalidOperationException("議댁옱?섏? ?딅뒗 ?몃뱶瑜?媛\u0080由ы궎??Edge媛\u0080 ?덉뒿?덈떎: " + nodeLink.BaseNodeGuid + " -> " + nodeLink.TargetNodeGuid);
				}
			}
		}
	}
}
