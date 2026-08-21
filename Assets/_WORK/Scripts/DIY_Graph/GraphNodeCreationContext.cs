using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph.Editor
{
	public readonly struct GraphNodeCreationContext
	{
		public Vector2 Position { get; }

		public IReadOnlyList<NodeBaseData> ExistingNodes { get; }

		public GraphNodeCreationContext(Vector2 position, IReadOnlyList<NodeBaseData> existingNodes)
		{
			Position = position;
			ExistingNodes = existingNodes ?? Array.Empty<NodeBaseData>();
		}
	}
}
