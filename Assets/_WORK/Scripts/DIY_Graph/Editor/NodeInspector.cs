using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UniversalGraph.Editor
{
	public sealed class NodeInspector : ScrollView
	{
		private readonly NodeInspectorContext context;

		public NodeInspector(Action<string, Action> applyEdit)
		{
			context = new NodeInspectorContext(applyEdit);
			((VisualElement)this).AddToClassList("inspector-panel");
		}

		public void UpdateInspector(Node selectedNode)
		{
			((VisualElement)this).Clear();
			if (selectedNode is GraphNode graphNode)
			{
				VisualElement val = graphNode.CreateInspector(context);
				if (val != null)
				{
					((VisualElement)this).Add(val);
				}
			}
		}
	}
}
