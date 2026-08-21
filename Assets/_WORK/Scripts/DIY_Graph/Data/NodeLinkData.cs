using System;

namespace UniversalGraph
{
	/// <summary>Serializable directed connection between two graph node ports.</summary>
	[Serializable]
	public class NodeLinkData
	{
		/// <summary>GUID of the node that owns the output port.</summary>
		public string BaseNodeGuid;

		/// <summary>Stable identifier of the source output port.</summary>
		public string PortName;

		/// <summary>GUID of the node that owns the input port.</summary>
		public string TargetNodeGuid;

		/// <summary>
		/// Stable identifier of the target input port. Empty values are legacy links and are supported only
		/// when the target node has exactly one input port.
		/// </summary>
		public string TargetPortName;
	}
}
