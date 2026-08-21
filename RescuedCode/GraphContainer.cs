using System.Collections.Generic;
using UnityEngine;
using UniversalGraph;

public abstract class GraphContainer : ScriptableObject
{
	public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();

	[SerializeReference]
	public List<NodeBaseData> Nodes = new List<NodeBaseData>();
}
