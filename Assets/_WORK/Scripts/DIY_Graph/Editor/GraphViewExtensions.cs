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
			return view.edges.ToList();
		}

		public static List<T> GetNodes<T>(this GraphView view) where T : Node
		{
			return ((IEnumerable)(object)view.nodes).OfType<T>().ToList();
		}
	}
}


