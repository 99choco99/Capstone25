using System;

namespace UniversalGraph.Editor
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class GraphNodeEditorAttribute : Attribute
	{
		public Type ContainerType { get; }

		public string MenuPath { get; }

		public GraphNodeEditorAttribute(string menuPath)
			: this(typeof(GraphContainer), menuPath)
		{
		}

		public GraphNodeEditorAttribute(Type containerType, string menuPath)
		{
			ContainerType = containerType;
			MenuPath = menuPath;
		}
	}
}
