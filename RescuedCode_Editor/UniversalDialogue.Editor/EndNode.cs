using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	[GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add End Node")]
	public sealed class EndNode : GraphNode<EndNodeData>
	{
		public override Vector2 DefaultSize => new Vector2(120f, 100f);

		protected override void Draw()
		{
			((GraphElement)this).title = "END";
			Port val = ((Node)this).InstantiatePort((Orientation)0, (Direction)0, (Capacity)1, typeof(float));
			val.portName = "Input";
			((Node)this).inputContainer.Add((VisualElement)(object)val);
			((VisualElement)this).AddToClassList("end-node");
			((Node)this).RefreshPorts();
			((Node)this).RefreshExpandedState();
		}

		public override VisualElement CreateInspector(NodeInspectorContext context)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Expected O, but got Unknown
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Expected O, but got Unknown
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			Label val2 = new Label("End Inspector");
			((VisualElement)val2).AddToClassList("inspector-title");
			val.Add((VisualElement)(object)val2);
			val.Add((VisualElement)new HelpBox("이 노드에 도달하면 대화가 정상 완료됩니다. 출력 포트는 없습니다.", (HelpBoxMessageType)1));
			return val;
		}
	}
}
