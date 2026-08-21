using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;

namespace UniversalGraph.Editor
{
	public static class GraphViewExtensions
	{
		public static List<Edge> GetEdges(this GraphView view)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			return view.edges.ToList();
		}

		public static List<T> GetNodes<T>(this GraphView view) where T : Node
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			return ((IEnumerable)(object)view.nodes).OfType<T>().ToList();
		}
	}
}
