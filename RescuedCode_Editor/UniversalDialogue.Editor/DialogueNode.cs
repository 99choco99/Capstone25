using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	[GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Dialogue Node")]
	public class DialogueNode : GraphNode<DialogueNodeData>
	{
		private Label fullTextLabel;

		public override Vector2 DefaultSize => new Vector2(150f, 200f);

		protected override void ValidateDataForView(DialogueNodeData data)
		{
			if (data.Choices == null)
			{
				throw new InvalidOperationException("Dialogue 선택지 목록이 null입니다.");
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal) { "Next" };
			foreach (DialogueChoiceData choice in data.Choices)
			{
				if (choice == null)
				{
					throw new InvalidOperationException("Dialogue 선택지 목록에 null 데이터가 있습니다.");
				}
				if (string.IsNullOrWhiteSpace(choice.PortName))
				{
					throw new InvalidOperationException("Dialogue 선택지에 Port ID가 없습니다.");
				}
				if (!hashSet.Add(choice.PortName))
				{
					throw new InvalidOperationException("Dialogue 출력 Port ID '" + choice.PortName + "'가 중복됐습니다.");
				}
			}
		}

		public override VisualElement CreateInspector(NodeInspectorContext context)
		{
			return DialogueNodeInspectorDrawer.Create(this, context);
		}

		protected override void Draw()
		{
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Expected O, but got Unknown
			Port val = ((Node)this).InstantiatePort((Orientation)0, (Direction)0, (Capacity)1, typeof(float));
			val.portName = "Input";
			((Node)this).inputContainer.Add((VisualElement)(object)val);
			Port val2 = ((Node)this).InstantiatePort((Orientation)0, (Direction)1, (Capacity)0, typeof(float));
			val2.portName = "Next";
			((Node)this).outputContainer.Add((VisualElement)(object)val2);
			((Node)this).RefreshPorts();
			if (base.TypeData != null && base.TypeData.Choices != null)
			{
				foreach (DialogueChoiceData choice in base.TypeData.Choices)
				{
					AddChoicePort(choice);
				}
			}
			fullTextLabel = new Label();
			((VisualElement)fullTextLabel).AddToClassList("node-full-text");
			((Node)this).extensionContainer.Add((VisualElement)(object)fullTextLabel);
			RefreshPreview();
			((Node)this).RefreshExpandedState();
		}

		public void RefreshPreview()
		{
			string text = (string.IsNullOrEmpty(base.TypeData.SpeakerName) ? "Unknown" : base.TypeData.SpeakerName);
			string text2 = (string.IsNullOrEmpty(base.TypeData.DialogueText) ? "" : base.TypeData.DialogueText);
			if (text2.Length > 15)
			{
				text2 = text2.Substring(0, 15) + "...";
			}
			((GraphElement)this).title = text + " : " + text2;
			((TextElement)fullTextLabel).text = (string.IsNullOrEmpty(base.TypeData.DialogueText) ? "(No Dialogue)" : base.TypeData.DialogueText);
		}

		public void AddChoicePort(DialogueChoiceData choiceData)
		{
			Port val = ((Node)this).InstantiatePort((Orientation)0, (Direction)1, (Capacity)0, typeof(float));
			val.portName = choiceData.PortName;
			Label val2 = UQueryExtensions.Q<Label>(((VisualElement)val).contentContainer, "type", (string)null);
			if (val2 != null)
			{
				((TextElement)val2).text = "Choice";
			}
			((Node)this).outputContainer.Add((VisualElement)(object)val);
			((Node)this).RefreshPorts();
			((Node)this).RefreshExpandedState();
		}

		public void RemoveChoicePort(string targetPortName)
		{
			Port val = ((Node)this).outputContainer.Children().OfType<Port>().FirstOrDefault((Port p) => p.portName == targetPortName);
			if (val == null)
			{
				return;
			}
			if (val.connected)
			{
				GraphView firstAncestorOfType = ((VisualElement)this).GetFirstAncestorOfType<GraphView>();
				List<Edge> list = val.connections.ToList();
				foreach (Edge item in list)
				{
					Port input = item.input;
					if (input != null)
					{
						input.Disconnect(item);
					}
					Port output = item.output;
					if (output != null)
					{
						output.Disconnect(item);
					}
					firstAncestorOfType.RemoveElement((GraphElement)(object)item);
				}
			}
			((Node)this).outputContainer.Remove((VisualElement)(object)val);
			((Node)this).RefreshPorts();
			((Node)this).RefreshExpandedState();
		}
	}
}
