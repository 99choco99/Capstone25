using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	[GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Entry Node")]
	public class StartNode : GraphNode<StartNodeData>
	{
		public override Vector2 DefaultSize => new Vector2(100f, 100f);

		protected override void InitializeNewData(StartNodeData data, GraphNodeCreationContext context)
		{
			HashSet<string> hashSet = (from entry in ((GraphNodeCreationContext)(ref context)).ExistingNodes.OfType<StartNodeData>()
				select entry.GetNormalizedEntryId()).ToHashSet(StringComparer.OrdinalIgnoreCase);
			if (!hashSet.Contains("Default"))
			{
				data.EntryId = "Default";
				return;
			}
			int num = 2;
			string text;
			do
			{
				text = $"Entry_{num++}";
			}
			while (hashSet.Contains(text));
			data.EntryId = text;
		}

		protected override void Draw()
		{
			RefreshTitle();
			Port val = ((Node)this).InstantiatePort((Orientation)0, (Direction)1, (Capacity)0, typeof(float));
			val.portName = "Next";
			((Node)this).outputContainer.Add((VisualElement)(object)val);
			((VisualElement)this).AddToClassList("start-node");
			((Node)this).RefreshPorts();
			((Node)this).RefreshExpandedState();
		}

		public override VisualElement CreateInspector(NodeInspectorContext context)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Expected O, but got Unknown
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Expected O, but got Unknown
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Expected O, but got Unknown
			//IL_0083: Expected O, but got Unknown
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			Label val2 = new Label("Entry Inspector");
			((VisualElement)val2).AddToClassList("inspector-title");
			val.Add((VisualElement)(object)val2);
			Label val3 = new Label("외부 시스템은 Graph와 Entry ID를 지정해 이 위치부터 대화를 시작합니다.");
			((VisualElement)val3).AddToClassList("entry-description");
			val.Add((VisualElement)(object)val3);
			TextField val4 = new TextField("Entry ID");
			((BaseField<string>)val4).value = base.TypeData.GetNormalizedEntryId();
			((TextInputBaseField<string>)val4).isDelayed = true;
			TextField entryIdField = val4;
			val.Add((VisualElement)(object)entryIdField);
			HelpBox duplicateWarning = new HelpBox("같은 Graph 안에서 Entry ID는 중복될 수 없습니다.", (HelpBoxMessageType)3);
			val.Add((VisualElement)(object)duplicateWarning);
			INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)entryIdField, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
			{
				string normalizedEntryId = StartNodeData.NormalizeEntryId(evt.newValue);
				context.ApplyEdit("Entry ID 변경", (Action)delegate
				{
					base.TypeData.EntryId = normalizedEntryId;
					((BaseField<string>)(object)entryIdField).SetValueWithoutNotify(normalizedEntryId);
					RefreshTitle();
					RefreshValidation();
				});
			});
			RefreshValidation();
			return val;
			void RefreshValidation()
			{
				//IL_005a: Unknown result type (might be due to invalid IL or missing references)
				string currentEntryId = StartNodeData.NormalizeEntryId(((BaseField<string>)(object)entryIdField).value);
				UniversalGraphView firstAncestorOfType = ((VisualElement)this).GetFirstAncestorOfType<UniversalGraphView>();
				int num = ((firstAncestorOfType != null) ? GraphViewExtensions.GetNodes<StartNode>((GraphView)(object)firstAncestorOfType).Count((StartNode node) => ((GraphNode<StartNodeData>)node).TypeData != null && string.Equals(((GraphNode<StartNodeData>)node).TypeData.GetNormalizedEntryId(), currentEntryId, StringComparison.OrdinalIgnoreCase)) : 0);
				((VisualElement)duplicateWarning).style.display = StyleEnum<DisplayStyle>.op_Implicit((DisplayStyle)(num <= 1));
			}
		}

		public void RefreshTitle()
		{
			((GraphElement)this).title = "ENTRY : " + base.TypeData.GetNormalizedEntryId();
		}
	}
}
