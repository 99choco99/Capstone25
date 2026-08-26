using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalGraph.Editor
{
	/// <summary>
	/// 새 노드 데이터의 생성을 위한 데이터박스
	/// </summary>
	public readonly struct GraphNodeDataCreationContext
	{
		/// <summary>
		/// 생성할 노드 위치
		/// </summary>
		public Vector2 Position { get; }

		/// <summary>
		/// 이미 존재하는 노드들
		/// </summary>
		public IReadOnlyList<NodeBaseData> ExistingNodes { get; }

		public GraphNodeDataCreationContext(Vector2 position, IReadOnlyList<NodeBaseData> existingNodes)
		{
			Position = position;
			ExistingNodes = existingNodes ?? Array.Empty<NodeBaseData>();
		}
	}
}
