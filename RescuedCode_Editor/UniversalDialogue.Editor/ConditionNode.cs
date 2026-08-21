using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	[GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Condition Node")]
	public class ConditionNode : GraphNode<ConditionNodeData>
	{
		public override Vector2 DefaultSize => new Vector2(200f, 150f);

		protected override void Draw()
		{
			RefreshPreview();
			Port val = ((Node)this).InstantiatePort((Orientation)0, (Direction)0, (Capacity)1, typeof(float));
			val.portName = "Input";
			((Node)this).inputContainer.Add((VisualElement)(object)val);
			Port val2 = ((Node)this).InstantiatePort((Orientation)0, (Direction)1, (Capacity)0, typeof(float));
			val2.portName = "True";
			((Node)this).outputContainer.Add((VisualElement)(object)val2);
			Port val3 = ((Node)this).InstantiatePort((Orientation)0, (Direction)1, (Capacity)0, typeof(float));
			val3.portName = "False";
			((Node)this).outputContainer.Add((VisualElement)(object)val3);
			((VisualElement)this).AddToClassList("condition-node");
			((Node)this).RefreshPorts();
			((Node)this).RefreshExpandedState();
		}

		public void RefreshPreview()
		{
			string text = (string.IsNullOrWhiteSpace(base.TypeData?.ConditionEventKey) ? "No Condition" : base.TypeData.ConditionEventKey);
			((GraphElement)this).title = "CONDITION : " + text;
		}

		public override VisualElement CreateInspector(NodeInspectorContext context)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Expected O, but got Unknown
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			Label val2 = new Label("Condition Inspector");
			((VisualElement)val2).AddToClassList("inspector-title");
			val.Add((VisualElement)(object)val2);
			val.Add(DialogueMethodBindingEditor.Create(context, "Condition", (DialogueMethodKind)1, new DialogueMethodBindingAccessor(() => base.TypeData.ConditionEventKey, delegate(string key)
			{
				base.TypeData.ConditionEventKey = key;
				RefreshPreview();
			}, () => base.TypeData.ConditionEventParam, delegate(string parameter)
			{
				base.TypeData.ConditionEventParam = parameter;
			}, () => base.TypeData.ConditionEventArguments, delegate(List<DialogueArgumentData> arguments)
			{
				base.TypeData.ConditionEventArguments = arguments;
			})));
			return val;
		}
	}
}
