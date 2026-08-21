using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	[GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Wait Signal Node")]
	public sealed class WaitSignalNode : GraphNode<WaitSignalNodeData>
	{
		public override Vector2 DefaultSize => new Vector2(200f, 120f);

		protected override void Draw()
		{
			RefreshPreview();
			Port val = ((Node)this).InstantiatePort((Orientation)0, (Direction)0, (Capacity)1, typeof(float));
			val.portName = "Input";
			((Node)this).inputContainer.Add((VisualElement)(object)val);
			Port val2 = ((Node)this).InstantiatePort((Orientation)0, (Direction)1, (Capacity)0, typeof(float));
			val2.portName = "Next";
			((Node)this).outputContainer.Add((VisualElement)(object)val2);
			((VisualElement)this).AddToClassList("wait-signal-node");
			((Node)this).RefreshPorts();
			((Node)this).RefreshExpandedState();
		}

		public override VisualElement CreateInspector(NodeInspectorContext context)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Expected O, but got Unknown
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Expected O, but got Unknown
			//IL_0064: Expected O, but got Unknown
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Expected O, but got Unknown
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			Label val2 = new Label("Wait Signal Inspector");
			((VisualElement)val2).AddToClassList("inspector-title");
			val.Add((VisualElement)(object)val2);
			TextField val3 = new TextField("Signal Key");
			((BaseField<string>)val3).value = base.TypeData.GetNormalizedSignalKey();
			((TextInputBaseField<string>)val3).isDelayed = true;
			TextField signalKeyField = val3;
			val.Add((VisualElement)(object)signalKeyField);
			HelpBox missingKeyWarning = new HelpBox("기다릴 Signal Key를 입력해야 합니다. 빈 Key는 실행 시 오류로 처리됩니다.", (HelpBoxMessageType)2);
			val.Add((VisualElement)(object)missingKeyWarning);
			INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)signalKeyField, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
			{
				string normalizedKey = WaitSignalNodeData.NormalizeSignalKey(evt.newValue);
				context.ApplyEdit("Signal Key 변경", (Action)delegate
				{
					base.TypeData.SignalKey = normalizedKey;
					((BaseField<string>)(object)signalKeyField).SetValueWithoutNotify(normalizedKey);
					RefreshPreview();
					RefreshValidation();
				});
			});
			val.Add((VisualElement)new HelpBox("Signal Key는 앞뒤 공백을 제거한 뒤 대소문자를 구분해 비교합니다.", (HelpBoxMessageType)1));
			RefreshValidation();
			return val;
			void RefreshValidation()
			{
				//IL_0027: Unknown result type (might be due to invalid IL or missing references)
				((VisualElement)missingKeyWarning).style.display = StyleEnum<DisplayStyle>.op_Implicit((DisplayStyle)(!string.IsNullOrWhiteSpace(base.TypeData.SignalKey)));
			}
		}

		private void RefreshPreview()
		{
			WaitSignalNodeData typeData = base.TypeData;
			string text = ((typeData != null) ? typeData.GetNormalizedSignalKey() : null);
			((GraphElement)this).title = (string.IsNullOrWhiteSpace(text) ? "WAIT SIGNAL : No Key" : ("WAIT SIGNAL : " + text));
		}
	}
}
