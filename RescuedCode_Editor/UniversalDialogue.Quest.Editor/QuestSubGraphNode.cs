using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalDialogue.Editor;

namespace UniversalDialogue.Quest.Editor
{
	[GraphNodeEditor(typeof(QuestContainer), "Quest/Flow/Sub-Quest Graph")]
	public sealed class QuestSubGraphNode : GraphNode<QuestSubGraphNodeData>
	{
		public override Vector2 DefaultSize => new Vector2(200f, 100f);

		protected override void Draw()
		{
			((GraphElement)this).title = $"서브 퀘스트: {base.TypeData.SubQuestId}";
			((Node)this).inputContainer.Add((VisualElement)(object)((Node)this).InstantiatePort((Orientation)0, (Direction)0, (Capacity)1, typeof(float)));
			((Node)this).outputContainer.Add((VisualElement)(object)((Node)this).InstantiatePort((Orientation)0, (Direction)1, (Capacity)0, typeof(float)));
			((Node)this).RefreshPorts();
		}
	}
}
