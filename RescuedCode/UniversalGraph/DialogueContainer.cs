using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UniversalGraph
{
	[MovedFrom(true, "UniversalGraph", "Assembly-CSharp", "DialogueContainer")]
	public class DialogueContainer : GraphContainer
	{
		public bool TryResolveEntry(string entryId, out StartNodeData entryNode, out string error)
		{
			entryNode = null;
			if (Nodes == null || Nodes.Count == 0)
			{
				error = "'" + ((Object)this).name + "' 洹몃옒?꾩뿉 ?몃뱶媛\u0080 ?놁뒿?덈떎.";
				return false;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (NodeBaseData node in Nodes)
			{
				if (node == null)
				{
					error = "'" + ((Object)this).name + "' 洹몃옒?꾩뿉 鍮꾩뼱 ?덈뒗 ?몃뱶 ?곗씠?곌? ?덉뒿?덈떎.";
					return false;
				}
				if (string.IsNullOrWhiteSpace(node.Guid))
				{
					error = "'" + ((Object)this).name + "' 洹몃옒?꾩뿉 GUID媛\u0080 ?녿뒗 ?몃뱶媛\u0080 ?덉뒿?덈떎.";
					return false;
				}
				if (!hashSet.Add(node.Guid))
				{
					error = "'" + ((Object)this).name + "' 洹몃옒?꾩뿉 以묐났 ?몃뱶 GUID '" + node.Guid + "'媛\u0080 ?덉뒿?덈떎.";
					return false;
				}
				if (!(node is DialogueNodeData { Choices: not null } dialogueNodeData))
				{
					continue;
				}
				HashSet<string> hashSet2 = new HashSet<string>(StringComparer.Ordinal);
				foreach (DialogueChoiceData choice in dialogueNodeData.Choices)
				{
					if (choice == null)
					{
						error = "'" + ((Object)this).name + "' 洹몃옒?꾩쓽 Dialogue ?몃뱶 '" + node.Guid + "'??鍮꾩뼱 ?덈뒗 Choice媛\u0080 ?덉뒿?덈떎.";
						return false;
					}
					if (string.IsNullOrWhiteSpace(choice.PortName))
					{
						error = "'" + ((Object)this).name + "' 洹몃옒?꾩쓽 Dialogue ?몃뱶 '" + node.Guid + "'??Port ID媛\u0080 ?녿뒗 Choice媛\u0080 ?덉뒿?덈떎.";
						return false;
					}
					if (string.Equals(choice.PortName, "Next", StringComparison.Ordinal) || !hashSet2.Add(choice.PortName))
					{
						error = "'" + ((Object)this).name + "' 洹몃옒?꾩쓽 Dialogue ?몃뱶 '" + node.Guid + "'??以묐났?섍굅???덉빟??Choice Port ID '" + choice.PortName + "'媛\u0080 ?덉뒿?덈떎.";
						return false;
					}
				}
			}
			Dictionary<string, StartNodeData> dictionary = new Dictionary<string, StartNodeData>(StringComparer.OrdinalIgnoreCase);
			foreach (StartNodeData item in Nodes.OfType<StartNodeData>())
			{
				string normalizedEntryId = item.GetNormalizedEntryId();
				if (!dictionary.TryAdd(normalizedEntryId, item))
				{
					error = "'" + ((Object)this).name + "' 洹몃옒?꾩뿉 以묐났 Entry ID '" + normalizedEntryId + "'媛\u0080 ?덉뒿?덈떎.";
					return false;
				}
			}
			string text = StartNodeData.NormalizeEntryId(entryId);
			if (!dictionary.TryGetValue(text, out var resolvedEntry))
			{
				error = "'" + ((Object)this).name + "' 洹몃옒?꾩뿉??Entry '" + text + "'瑜?李얠쓣 ???놁뒿?덈떎.";
				return false;
			}
			List<NodeLinkData> list = NodeLinks?.Where((NodeLinkData link) => link != null && string.Equals(link.BaseNodeGuid, resolvedEntry.Guid, StringComparison.Ordinal) && string.Equals(link.PortName, "Next", StringComparison.Ordinal)).ToList() ?? new List<NodeLinkData>();
			if (list.Count != 1)
			{
				error = "Error";
				return false;
			}
			if (!hashSet.Contains(list[0].TargetNodeGuid))
			{
				error = "'" + ((Object)this).name + "' 洹몃옒?꾩쓽 Entry '" + text + "'媛\u0080 議댁옱?섏? ?딅뒗 ?몃뱶瑜?媛\u0080由ы궢?덈떎.";
				return false;
			}
			entryNode = resolvedEntry;
			error = null;
			return true;
		}
	}
}
