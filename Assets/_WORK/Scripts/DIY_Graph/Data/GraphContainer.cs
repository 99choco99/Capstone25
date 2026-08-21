using System.Collections.Generic;
using UnityEngine;
using UniversalGraph;

/// <summary>Base ScriptableObject asset that stores graph nodes and their directed links.</summary>
public abstract class GraphContainer : ScriptableObject
{
	/// <summary>Serialized directed connections between node ports.</summary>
	public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();

	/// <summary>Concrete node data stored polymorphically by Unity SerializeReference.</summary>
	[SerializeReference]
	public List<NodeBaseData> Nodes = new List<NodeBaseData>();
}
