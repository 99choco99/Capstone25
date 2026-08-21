using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>Starts a referenced quest and resumes its parent graph after that quest completes.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Flow/Sub-Quest Graph")]
    public sealed class QuestSubGraphNode : GraphNode<QuestSubGraphNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(200f, 100f);

        /// <summary>Creates the parent-flow input and continuation output ports.</summary>
        protected override void Draw()
        {
            RefreshTitle();

            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);

            Port next = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            next.portName = "Next";
            outputContainer.Add(next);

            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>Creates the sub-quest ID input.</summary>
        public override VisualElement CreateInspector(NodeInspectorContext context)
        {
            var root = new VisualElement();
            root.Add(new Label("Sub-Quest"));
            root.Add(new HelpBox("The parent flow resumes only after this referenced quest completes.", HelpBoxMessageType.Info));

            var questIdField = new IntegerField("Sub-Quest ID")
            {
                value = TypeData.SubQuestId,
                isDelayed = true
            };
            questIdField.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit("Change sub-quest ID", () =>
                {
                    TypeData.SubQuestId = change.newValue;
                    RefreshTitle();
                });
            });
            root.Add(questIdField);
            return root;
        }

        /// <summary>Updates the visible sub-quest identifier.</summary>
        private void RefreshTitle()
        {
            title = $"SUB-QUEST: {TypeData.SubQuestId}";
        }
    }
}
