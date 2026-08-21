using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	[GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Wait Node")]
	public sealed class WaitNode : GraphNode<WaitNodeData>
	{
		public override Vector2 DefaultSize => new Vector2(180f, 120f);

		protected override void ValidateDataForView(WaitNodeData data)
		{
			if (float.IsNaN(data.DurationSeconds) || float.IsInfinity(data.DurationSeconds) || data.DurationSeconds < 0f)
			{
				throw new InvalidOperationException("Wait 시간은 0 이상의 유한한 초 단위 값이어야 합니다.");
			}
		}

		protected override void Draw()
		{
			RefreshPreview();
			Port val = ((Node)this).InstantiatePort((Orientation)0, (Direction)0, (Capacity)1, typeof(float));
			val.portName = "Input";
			((Node)this).inputContainer.Add((VisualElement)(object)val);
			Port val2 = ((Node)this).InstantiatePort((Orientation)0, (Direction)1, (Capacity)0, typeof(float));
			val2.portName = "Next";
			((Node)this).outputContainer.Add((VisualElement)(object)val2);
			((VisualElement)this).AddToClassList("wait-node");
			((Node)this).RefreshPorts();
			((Node)this).RefreshExpandedState();
		}

		public override VisualElement CreateInspector(NodeInspectorContext context)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Expected O, but got Unknown
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Expected O, but got Unknown
			//IL_0069: Expected O, but got Unknown
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Expected O, but got Unknown
			//IL_00ab: Expected O, but got Unknown
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			Label val2 = new Label("Wait Inspector");
			((VisualElement)val2).AddToClassList("inspector-title");
			val.Add((VisualElement)(object)val2);
			FloatField val3 = new FloatField("Duration (Seconds)", 1000);
			((BaseField<float>)val3).value = base.TypeData.DurationSeconds;
			((TextInputBaseField<float>)val3).isDelayed = true;
			FloatField durationField = val3;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<float>((INotifyValueChanged<float>)(object)durationField, (EventCallback<ChangeEvent<float>>)delegate(ChangeEvent<float> evt)
			{
				float newValue = evt.newValue;
				if (float.IsNaN(newValue) || float.IsInfinity(newValue))
				{
					((BaseField<float>)(object)durationField).SetValueWithoutNotify(base.TypeData.DurationSeconds);
				}
				else
				{
					float duration = Mathf.Max(0f, newValue);
					context.ApplyEdit("대기 시간 변경", (Action)delegate
					{
						base.TypeData.DurationSeconds = duration;
						((BaseField<float>)(object)durationField).SetValueWithoutNotify(duration);
						RefreshPreview();
					});
				}
			});
			val.Add((VisualElement)(object)durationField);
			Toggle val4 = new Toggle("Use Unscaled Time");
			((BaseField<bool>)val4).value = base.TypeData.UseUnscaledTime;
			Toggle val5 = val4;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<bool>((INotifyValueChanged<bool>)(object)val5, (EventCallback<ChangeEvent<bool>>)delegate(ChangeEvent<bool> evt)
			{
				context.ApplyEdit("대기 시간 기준 변경", (Action)delegate
				{
					base.TypeData.UseUnscaledTime = evt.newValue;
					RefreshPreview();
				});
			});
			val.Add((VisualElement)(object)val5);
			val.Add((VisualElement)new HelpBox("Unscaled Time은 Time.timeScale이 0인 메뉴나 대화 중에도 시간이 흐릅니다.", (HelpBoxMessageType)1));
			return val;
		}

		private void RefreshPreview()
		{
			string arg = ((base.TypeData != null && base.TypeData.UseUnscaledTime) ? "Unscaled" : "Scaled");
			float num = base.TypeData?.DurationSeconds ?? 0f;
			((GraphElement)this).title = $"WAIT : {num:0.###}s ({arg})";
		}
	}
}
