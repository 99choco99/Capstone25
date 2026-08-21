using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>Executes one registered dialogue action, then continues through its Next port.</summary>
    [GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Action Node")]
    public sealed class ActionNode : GraphNode<ActionNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(220f, 150f);

        /// <summary>Creates a single flow input and continuation output.</summary>
        protected override void Draw()
        {
            RefreshPreview();
            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);

            Port next = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            next.portName = "Next";
            outputContainer.Add(next);

            AddToClassList("action-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>Creates the action-key selector and its generated argument controls.</summary>
        public override VisualElement CreateInspector(NodeInspectorContext context)
        {
            var root = new VisualElement();
            var titleLabel = new Label("Action");
            titleLabel.AddToClassList("inspector-title");
            root.Add(titleLabel);
            root.Add(new HelpBox("This node executes once without displaying dialogue, then follows Next.", HelpBoxMessageType.Info));

            var missingActionWarning = new HelpBox("Select an action. A blank action node is invalid at runtime.", HelpBoxMessageType.Warning);
            root.Add(DialogueMethodBindingEditor.Create(
                context,
                "Action",
                DialogueMethodKind.Action,
                new DialogueMethodBindingAccessor(
                    () => TypeData.EventKey,
                    key =>
                    {
                        TypeData.EventKey = key;
                        RefreshPreview();
                        RefreshValidation();
                    },
                    () => TypeData.EventParam,
                    parameter => TypeData.EventParam = parameter,
                    () => TypeData.EventArguments,
                    arguments => TypeData.EventArguments = arguments)));
            root.Add(missingActionWarning);

            RefreshValidation();
            return root;

            void RefreshValidation()
            {
                missingActionWarning.style.display = string.IsNullOrWhiteSpace(TypeData.EventKey)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        /// <summary>Updates the node title with the currently bound action key.</summary>
        private void RefreshPreview()
        {
            title = string.IsNullOrWhiteSpace(TypeData?.EventKey) ? "ACTION: No Action" : $"ACTION: {TypeData.EventKey}";
        }
    }
}
