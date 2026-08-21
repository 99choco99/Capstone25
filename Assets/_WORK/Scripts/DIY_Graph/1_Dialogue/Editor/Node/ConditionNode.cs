using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>Branches dialogue flow through True or False after evaluating a registered condition.</summary>
    [GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Condition Node")]
    public sealed class ConditionNode : GraphNode<ConditionNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(220f, 150f);

        /// <summary>Creates the input and named True/False output ports.</summary>
        protected override void Draw()
        {
            RefreshPreview();

            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);

            Port truePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            truePort.portName = "True";
            outputContainer.Add(truePort);

            Port falsePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            falsePort.portName = "False";
            outputContainer.Add(falsePort);

            AddToClassList("condition-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>Creates the condition-key selector and its generated argument controls.</summary>
        public override VisualElement CreateInspector(NodeInspectorContext context)
        {
            var root = new VisualElement();
            var titleLabel = new Label("Condition");
            titleLabel.AddToClassList("inspector-title");
            root.Add(titleLabel);
            root.Add(new HelpBox("The selected condition chooses exactly one of the True and False ports.", HelpBoxMessageType.Info));

            root.Add(DialogueMethodBindingEditor.Create(
                context,
                "Condition",
                DialogueMethodKind.Condition,
                new DialogueMethodBindingAccessor(
                    () => TypeData.ConditionEventKey,
                    key =>
                    {
                        TypeData.ConditionEventKey = key;
                        RefreshPreview();
                    },
                    () => TypeData.ConditionEventParam,
                    parameter => TypeData.ConditionEventParam = parameter,
                    () => TypeData.ConditionEventArguments,
                    arguments => TypeData.ConditionEventArguments = arguments)));
            return root;
        }

        /// <summary>Updates the canvas title after a condition binding changes.</summary>
        private void RefreshPreview()
        {
            title = string.IsNullOrWhiteSpace(TypeData?.ConditionEventKey)
                ? "CONDITION: No Condition"
                : $"CONDITION: {TypeData.ConditionEventKey}";
        }
    }
}
