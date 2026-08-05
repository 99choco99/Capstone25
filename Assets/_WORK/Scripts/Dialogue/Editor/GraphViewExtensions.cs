using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;


public static class GraphViewExtensions
{
    public static List<Edge> GetEdges(this GraphView view) => view.edges.ToList();
    public static List<T> GetNodes<T>(this GraphView view) where T : Node => view.nodes.OfType<T>().ToList();
}
