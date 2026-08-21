using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
	[GraphNodeEditor(typeof(QuestContainer), "Quest/On Interact Entry")]
	public sealed class QuestEventEntryNode : GraphNode<QuestEventEntryNodeData>
	{
		public override Vector2 DefaultSize => new Vector2(150f, 100f);

		protected override void Draw()
		{
			RefreshTitle();
			Port val = ((Node)this).InstantiatePort((Orientation)0, (Direction)1, (Port.Capacity)0, typeof(float));
			val.portName = "Next";
			((Node)this).outputContainer.Add((VisualElement)(object)val);
			((VisualElement)this).AddToClassList("quest-entry-node");
			((Node)this).RefreshPorts();
			((Node)this).RefreshExpandedState();
		}

		public override VisualElement CreateInspector(NodeInspectorContext context)
		{
			VisualElement val = new VisualElement();
			Label val2 = new Label("Interact Entry Inspector");
			((VisualElement)val2).AddToClassList("inspector-title");
			val.Add((VisualElement)(object)val2);
			val.Add((VisualElement)new HelpBox("특정 대상과 상호작용 시 이 위치에서 퀘스트 조건 검사를 시작합니다.", (HelpBoxMessageType)1));
			TextField val3 = new TextField("Target ID");
			((BaseField<string>)val3).value = base.TypeData.TargetId;
			((TextInputBaseField<string>)val3).isDelayed = true;
			TextField val4 = val3;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)val4, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
			{
				context.ApplyEdit("Target ID 변경", (Action)delegate
				{
					base.TypeData.TargetId = evt.newValue;
					RefreshTitle();
				});
			});
			val.Add((VisualElement)(object)val4);
			return val;
		}

		private void RefreshTitle()
		{
			string text = (string.IsNullOrWhiteSpace(base.TypeData.TargetId) ? "Any" : base.TypeData.TargetId);
			((GraphElement)this).title = "Interact : " + text;
		}
	}
}


