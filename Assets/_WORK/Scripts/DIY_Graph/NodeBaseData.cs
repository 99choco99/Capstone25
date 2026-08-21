using System;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>Base serialized state shared by every graph node.</summary>
	[Serializable]
	public abstract class NodeBaseData
	{
		/// <summary>Stable identifier used by graph links and runtime progress.</summary>
		public string Guid;

		/// <summary>Canvas position used by the editor only.</summary>
		public Vector2 Position;
	}
}
