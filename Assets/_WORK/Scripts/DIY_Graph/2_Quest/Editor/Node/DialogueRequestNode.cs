using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>Quest graph endpoint that offers one dialogue graph entry to the conversation resolver.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Dialogue Request Endpoint")]
    public sealed class DialogueRequestNode : GraphNode<DialogueRequestNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(250f, 150f);

        /// <summary>Creates the endpoint's input port.</summary>
        protected override void Draw()
        {
            RefreshTitle();
            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);
            AddToClassList("end-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>Creates the dialogue reference, topic, and priority controls.</summary>
        public override VisualElement CreateInspector(NodeInspectorContext context)
        {
            var root = new VisualElement();
            var title = new Label("Dialogue Request");
            title.AddToClassList("inspector-title");
            root.Add(title);
            root.Add(new HelpBox("Reaching this endpoint makes the selected dialogue entry available to the conversation resolver.", HelpBoxMessageType.Info));

            var graphField = new ObjectField("Graph Asset")
            {
                objectType = typeof(DialogueContainer),
                allowSceneObjects = false,
                value = TypeData.DialogueReference.GraphAsset
            };
            graphField.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit("Change dialogue graph", () =>
                {
                    TypeData.DialogueReference.GraphAsset = change.newValue as DialogueContainer;
                    RefreshTitle();
                });
            });
            root.Add(graphField);

            root.Add(CreateDelayedTextField("Entry ID", TypeData.DialogueReference.EntryId, "Change dialogue entry", value =>
            {
                TypeData.DialogueReference.EntryId = value;
                RefreshTitle();
            }, context));

            root.Add(CreateDelayedTextField("Topic Name", TypeData.TopicName, "Change dialogue topic", value =>
            {
                TypeData.TopicName = value;
                RefreshTitle();
            }, context));

            var priorityField = new IntegerField("Priority")
            {
                value = TypeData.Priority,
                isDelayed = true
            };
            priorityField.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit("Change dialogue priority", () =>
                {
                    TypeData.Priority = change.newValue;
                    RefreshTitle();
                });
            });
            root.Add(priorityField);
            return root;
        }

        private static TextField CreateDelayedTextField(
            string label,
            string value,
            string undoName,
            System.Action<string> applyValue,
            NodeInspectorContext context)
        {
            var field = new TextField(label)
            {
                value = value ?? string.Empty,
                isDelayed = true
            };
            field.RegisterValueChangedCallback(change => context.ApplyEdit(undoName, () => applyValue(change.newValue)));
            return field;
        }

        /// <summary>Updates the canvas title to show the selected dialogue graph and entry.</summary>
        private void RefreshTitle()
        {
            string graphName = TypeData.DialogueReference.GraphAsset == null ? "None" : TypeData.DialogueReference.GraphAsset.name;
            title = $"Request: {graphName} ({TypeData.DialogueReference.EntryId})";
        }
    }
}
