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
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			Position = position;
			ExistingNodes = existingNodes ?? Array.Empty<NodeBaseData>();
		}
	}
}
