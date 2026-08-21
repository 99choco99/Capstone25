using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	internal static class DialogueNodeInspectorDrawer
	{
		public static VisualElement Create(DialogueNode selectedNode, NodeInspectorContext context)
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Expected O, but got Unknown
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			Label val2 = new Label("Node Inspector");
			((VisualElement)val2).AddToClassList("inspector-title");
			val.Add((VisualElement)(object)val2);
			val.Add((VisualElement)(object)CreateSpeakerField(selectedNode, context));
			val.Add((VisualElement)(object)CreateDialogueField(selectedNode, context));
			val.Add(DialogueMethodBindingEditor.Create(context, "On Enter Action", (DialogueMethodKind)0, new DialogueMethodBindingAccessor(() => ((GraphNode<DialogueNodeData>)selectedNode).TypeData.EventKey, delegate(string key)
			{
				((GraphNode<DialogueNodeData>)selectedNode).TypeData.EventKey = key;
			}, () => ((GraphNode<DialogueNodeData>)selectedNode).TypeData.EventParam, delegate(string parameter)
			{
				((GraphNode<DialogueNodeData>)selectedNode).TypeData.EventParam = parameter;
			}, () => ((GraphNode<DialogueNodeData>)selectedNode).TypeData.EventArguments, delegate(List<DialogueArgumentData> arguments)
			{
				((GraphNode<DialogueNodeData>)selectedNode).TypeData.EventArguments = arguments;
			})));
			val.Add(CreateChoicesField(selectedNode, context));
			return val;
		}

		private static TextField CreateSpeakerField(DialogueNode selectedNode, NodeInspectorContext context)
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Expected O, but got Unknown
			//IL_0037: Expected O, but got Unknown
			TextField val = new TextField("Speaker");
			((BaseField<string>)val).value = ((GraphNode<DialogueNodeData>)selectedNode).TypeData.SpeakerName;
			TextField val2 = val;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)val2, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
			{
				context.ApplyEdit("화자 변경", (Action)delegate
				{
					((GraphNode<DialogueNodeData>)selectedNode).TypeData.SpeakerName = evt.newValue;
					selectedNode.RefreshPreview();
				});
			});
			return val2;
		}

		private static TextField CreateDialogueField(DialogueNode selectedNode, NodeInspectorContext context)
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Expected O, but got Unknown
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			TextField val = new TextField("Dialogue");
			((BaseField<string>)val).value = ((GraphNode<DialogueNodeData>)selectedNode).TypeData.DialogueText;
			val.multiline = true;
			TextField val2 = val;
			((VisualElement)val2).AddToClassList("dialogue-field");
			INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)val2, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
			{
				context.ApplyEdit("대화문 변경", (Action)delegate
				{
					((GraphNode<DialogueNodeData>)selectedNode).TypeData.DialogueText = evt.newValue;
					selectedNode.RefreshPreview();
				});
			});
			return val2;
		}

		private static VisualElement CreateChoicesField(DialogueNode selectedNode, NodeInspectorContext context)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Expected O, but got Unknown
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			Label val2 = new Label("Choices");
			((VisualElement)val2).AddToClassList("choice-title");
			val.Add((VisualElement)(object)val2);
			VisualElement choicesContainer = new VisualElement();
			val.Add(choicesContainer);
			RedrawChoices();
			Button val3 = new Button((Action)delegate
			{
				context.ApplyEdit("선택지 추가", (Action)delegate
				{
					//IL_0001: Unknown result type (might be due to invalid IL or missing references)
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					//IL_001f: Unknown result type (might be due to invalid IL or missing references)
					//IL_002a: Unknown result type (might be due to invalid IL or missing references)
					//IL_0035: Unknown result type (might be due to invalid IL or missing references)
					//IL_0041: Expected O, but got Unknown
					DialogueChoiceData val4 = new DialogueChoiceData
					{
						PortName = Guid.NewGuid().ToString(),
						ChoiceText = "Choice Text",
						ChoiceEventKey = string.Empty,
						ChoiceEventParam = string.Empty
					};
					((GraphNode<DialogueNodeData>)selectedNode).TypeData.Choices.Add(val4);
					selectedNode.AddChoicePort(val4);
					RedrawChoices();
				});
			})
			{
				text = "+ Add Choice"
			};
			((VisualElement)val3).AddToClassList("add-choice-btn");
			val.Add((VisualElement)(object)val3);
			return val;
			void RedrawChoices()
			{
				choicesContainer.Clear();
				foreach (DialogueChoiceData choice in ((GraphNode<DialogueNodeData>)selectedNode).TypeData.Choices)
				{
					if (choice != null)
					{
						choicesContainer.Add((VisualElement)(object)CreateChoiceBox(selectedNode, choice, context, RedrawChoices));
					}
				}
			}
		}

		private static Box CreateChoiceBox(DialogueNode selectedNode, DialogueChoiceData choice, NodeInspectorContext context, Action onRedrawNeeded)
		{
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Expected O, but got Unknown
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Expected O, but got Unknown
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Expected O, but got Unknown
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Expected O, but got Unknown
			Box val = new Box();
			((VisualElement)val).AddToClassList("choice-box");
			TextField val2 = new TextField("Text");
			((BaseField<string>)val2).value = choice.ChoiceText;
			val2.multiline = true;
			TextField val3 = val2;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)val3, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
			{
				context.ApplyEdit("선택지 텍스트 변경", (Action)delegate
				{
					choice.ChoiceText = evt.newValue;
				});
			});
			((VisualElement)val).Add((VisualElement)(object)val3);
			((VisualElement)val).Add(DialogueMethodBindingEditor.Create(context, "On Select Action", (DialogueMethodKind)0, new DialogueMethodBindingAccessor(() => choice.ChoiceEventKey, delegate(string key)
			{
				choice.ChoiceEventKey = key;
			}, () => choice.ChoiceEventParam, delegate(string parameter)
			{
				choice.ChoiceEventParam = parameter;
			}, () => choice.ChoiceEventArguments, delegate(List<DialogueArgumentData> arguments)
			{
				choice.ChoiceEventArguments = arguments;
			})));
			Button val4 = new Button((Action)delegate
			{
				context.ApplyEdit("선택지 삭제", (Action)delegate
				{
					selectedNode.RemoveChoicePort(choice.PortName);
					((GraphNode<DialogueNodeData>)selectedNode).TypeData.Choices.Remove(choice);
					onRedrawNeeded?.Invoke();
				});
			})
			{
				text = "Delete Choice"
			};
			((VisualElement)val).Add((VisualElement)(object)val4);
			return val;
		}
	}
}
