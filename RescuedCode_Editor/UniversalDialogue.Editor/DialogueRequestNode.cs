using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	[GraphNodeEditor(typeof(QuestContainer), "Quest/Dialogue Request Endpoint")]
	public sealed class DialogueRequestNode : GraphNode<DialogueRequestNodeData>
	{
		public override Vector2 DefaultSize => new Vector2(250f, 150f);

		protected override void Draw()
		{
			RefreshTitle();
			Port val = ((Node)this).InstantiatePort((Orientation)0, (Direction)0, (Capacity)1, typeof(float));
			val.portName = "Input";
			((Node)this).inputContainer.Add((VisualElement)(object)val);
			((VisualElement)this).AddToClassList("end-node");
			((Node)this).RefreshPorts();
			((Node)this).RefreshExpandedState();
		}

		public override VisualElement CreateInspector(NodeInspectorContext context)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Expected O, but got Unknown
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Expected O, but got Unknown
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Expected O, but got Unknown
			//IL_00cb: Expected O, but got Unknown
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Expected O, but got Unknown
			//IL_0104: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Expected O, but got Unknown
			//IL_010e: Expected O, but got Unknown
			//IL_0135: Unknown result type (might be due to invalid IL or missing references)
			//IL_013a: Unknown result type (might be due to invalid IL or missing references)
			//IL_014b: Expected O, but got Unknown
			//IL_014c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0153: Expected O, but got Unknown
			//IL_0156: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			Label val2 = new Label("Dialogue Request Inspector");
			((VisualElement)val2).AddToClassList("inspector-title");
			val.Add((VisualElement)(object)val2);
			val.Add((VisualElement)new HelpBox("이 노드에 도달하면 지정한 DialogueReference를 Coordinator에 반환합니다.", (HelpBoxMessageType)1));
			ObjectField val3 = new ObjectField("Graph Asset");
			val3.objectType = typeof(DialogueContainer);
			((BaseField<Object>)(object)val3).value = (Object)(object)base.TypeData.DialogueReference.GraphAsset;
			ObjectField val4 = val3;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<Object>((INotifyValueChanged<Object>)(object)val4, (EventCallback<ChangeEvent<Object>>)delegate(ChangeEvent<Object> evt)
			{
				context.ApplyEdit("Graph Asset 변경", (Action)delegate
				{
					ref DialogueContainer graphAsset = ref base.TypeData.DialogueReference.GraphAsset;
					Object newValue = evt.newValue;
					graphAsset = (DialogueContainer)(object)((newValue is DialogueContainer) ? newValue : null);
					RefreshTitle();
				});
			});
			val.Add((VisualElement)(object)val4);
			TextField val5 = new TextField("Entry ID");
			((BaseField<string>)val5).value = base.TypeData.DialogueReference.EntryId;
			((TextInputBaseField<string>)val5).isDelayed = true;
			TextField val6 = val5;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)val6, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
			{
				context.ApplyEdit("Entry ID 변경", (Action)delegate
				{
					base.TypeData.DialogueReference.EntryId = evt.newValue;
					RefreshTitle();
				});
			});
			val.Add((VisualElement)(object)val6);
			TextField val7 = new TextField("Topic Name");
			((BaseField<string>)val7).value = base.TypeData.TopicName;
			((TextInputBaseField<string>)val7).isDelayed = true;
			TextField val8 = val7;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)val8, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
			{
				context.ApplyEdit("Topic Name 변경", (Action)delegate
				{
					base.TypeData.TopicName = evt.newValue;
					RefreshTitle();
				});
			});
			val.Add((VisualElement)(object)val8);
			IntegerField val9 = new IntegerField("Priority", 1000);
			((BaseField<int>)val9).value = base.TypeData.Priority;
			((TextInputBaseField<int>)val9).isDelayed = true;
			IntegerField val10 = val9;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<int>((INotifyValueChanged<int>)(object)val10, (EventCallback<ChangeEvent<int>>)delegate(ChangeEvent<int> evt)
			{
				context.ApplyEdit("Priority 변경", (Action)delegate
				{
					base.TypeData.Priority = evt.newValue;
					RefreshTitle();
				});
			});
			val.Add((VisualElement)(object)val10);
			return val;
		}

		private void RefreshTitle()
		{
			string text = (((Object)(object)base.TypeData.DialogueReference.GraphAsset != (Object)null) ? ((Object)base.TypeData.DialogueReference.GraphAsset).name : "None");
			((GraphElement)this).title = "Request : " + text + " (" + base.TypeData.DialogueReference.EntryId + ")";
		}
	}
}
