using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	[GraphNodeEditor(typeof(QuestContainer), "Quest/Condition (Quest State)")]
	public sealed class QuestStateConditionNode : GraphNode<QuestStateConditionNodeData>
	{
		public override Vector2 DefaultSize => new Vector2(200f, 120f);

		protected override void Draw()
		{
			RefreshTitle();
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

		public override VisualElement CreateInspector(NodeInspectorContext context)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Expected O, but got Unknown
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Expected O, but got Unknown
			//IL_0064: Expected O, but got Unknown
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			Label val2 = new Label("Quest State Inspector");
			((VisualElement)val2).AddToClassList("inspector-title");
			val.Add((VisualElement)(object)val2);
			IntegerField val3 = new IntegerField("Quest ID", 1000);
			((BaseField<int>)val3).value = base.TypeData.QuestId;
			((TextInputBaseField<int>)val3).isDelayed = true;
			IntegerField val4 = val3;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<int>((INotifyValueChanged<int>)(object)val4, (EventCallback<ChangeEvent<int>>)delegate(ChangeEvent<int> evt)
			{
				context.ApplyEdit("Quest ID 변경", (Action)delegate
				{
					base.TypeData.QuestId = evt.newValue;
					RefreshTitle();
				});
			});
			val.Add((VisualElement)(object)val4);
			EnumField val5 = new EnumField("Target State", (Enum)base.TypeData.TargetState);
			INotifyValueChangedExtensions.RegisterValueChangedCallback<Enum>((INotifyValueChanged<Enum>)(object)val5, (EventCallback<ChangeEvent<Enum>>)delegate(ChangeEvent<Enum> evt)
			{
				context.ApplyEdit("Target State 변경", (Action)delegate
				{
					base.TypeData.TargetState = (QuestState)(object)evt.newValue;
					RefreshTitle();
				});
			});
			val.Add((VisualElement)(object)val5);
			return val;
		}

		private void RefreshTitle()
		{
			((GraphElement)this).title = $"Q[{base.TypeData.QuestId}] == {base.TypeData.TargetState}";
		}
	}
}
