using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>Explicitly ends the active dialogue session.</summary>
    [GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add End Node")]
    public sealed class EndNode : GraphNode<EndNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(140f, 100f);

        /// <summary>Creates the terminal input port.</summary>
        protected override void Draw()
        {
            title = "END";
            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);
            AddToClassList("end-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>Explains the terminal behavior in the inspector.</summary>
        public override VisualElement CreateInspector(NodeInspectorContext context)
        {
            var root = new VisualElement();
            var titleLabel = new Label("End");
            titleLabel.AddToClassList("inspector-title");
            root.Add(titleLabel);
            root.Add(new HelpBox("Reaching this node completes the dialogue. It has no output port.", HelpBoxMessageType.Info));
            return root;
        }
    }
}
