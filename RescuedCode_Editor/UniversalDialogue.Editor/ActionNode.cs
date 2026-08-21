using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	[GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Action Node")]
	public sealed class ActionNode : GraphNode<ActionNodeData>
	{
		public override Vector2 DefaultSize => new Vector2(200f, 150f);

		protected override void Draw()
		{
			RefreshPreview();
			Port val = ((Node)this).InstantiatePort((Orientation)0, (Direction)0, (Capacity)1, typeof(float));
			val.portName = "Input";
			((Node)this).inputContainer.Add((VisualElement)(object)val);
			Port val2 = ((Node)this).InstantiatePort((Orientation)0, (Direction)1, (Capacity)0, typeof(float));
			val2.portName = "Next";
			((Node)this).outputContainer.Add((VisualElement)(object)val2);
			((VisualElement)this).AddToClassList("action-node");
			((Node)this).RefreshPorts();
			((Node)this).RefreshExpandedState();
		}

		public override VisualElement CreateInspector(NodeInspectorContext context)
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Expected O, but got Unknown
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected O, but got Unknown
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Expected O, but got Unknown
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			Label val2 = new Label("Action Inspector");
			((VisualElement)val2).AddToClassList("inspector-title");
			val.Add((VisualElement)(object)val2);
			val.Add((VisualElement)new HelpBox("대사를 표시하지 않고 Action을 한 번 실행한 뒤 Next로 진행합니다.", (HelpBoxMessageType)1));
			HelpBox missingActionWarning = new HelpBox("실행할 Action을 선택해야 합니다. 빈 Action은 실행 시 오류로 처리됩니다.", (HelpBoxMessageType)2);
			val.Add(DialogueMethodBindingEditor.Create(context, "Action", (DialogueMethodKind)0, new DialogueMethodBindingAccessor(() => base.TypeData.EventKey, delegate(string key)
			{
				base.TypeData.EventKey = key;
				RefreshPreview();
				RefreshValidation();
			}, () => base.TypeData.EventParam, delegate(string parameter)
			{
				base.TypeData.EventParam = parameter;
			}, () => base.TypeData.EventArguments, delegate(List<DialogueArgumentData> arguments)
			{
				base.TypeData.EventArguments = arguments;
			})));
			val.Add((VisualElement)(object)missingActionWarning);
			RefreshValidation();
			return val;
			void RefreshValidation()
			{
				//IL_0027: Unknown result type (might be due to invalid IL or missing references)
				((VisualElement)missingActionWarning).style.display = StyleEnum<DisplayStyle>.op_Implicit((DisplayStyle)(!string.IsNullOrWhiteSpace(base.TypeData.EventKey)));
			}
		}

		private void RefreshPreview()
		{
			string text = (string.IsNullOrWhiteSpace(base.TypeData?.EventKey) ? "No Action" : base.TypeData.EventKey);
			((GraphElement)this).title = "ACTION : " + text;
		}
	}
}
