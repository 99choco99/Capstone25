using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>Pauses dialogue flow until a matching runtime dialogue signal is published.</summary>
    [GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Wait Signal Node")]
    public sealed class WaitSignalNode : GraphNode<WaitSignalNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(220f, 120f);

        /// <summary>Creates the flow input and continuation output ports.</summary>
        protected override void Draw()
        {
            RefreshPreview();
            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);

            Port next = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            next.portName = "Next";
            outputContainer.Add(next);

            AddToClassList("wait-signal-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>Creates the normalized, case-sensitive signal-key field.</summary>
        public override VisualElement CreateInspector(NodeInspectorContext context)
        {
            var root = new VisualElement();
            var titleLabel = new Label("Wait Signal");
            titleLabel.AddToClassList("inspector-title");
            root.Add(titleLabel);

            var signalKeyField = new TextField("Signal Key")
            {
                value = TypeData.GetNormalizedSignalKey(),
                isDelayed = true
            };
            root.Add(signalKeyField);

            var missingKeyWarning = new HelpBox("Enter the signal key to wait for. A blank key is invalid at runtime.", HelpBoxMessageType.Warning);
            root.Add(missingKeyWarning);
            signalKeyField.RegisterValueChangedCallback(change =>
            {
                string normalizedKey = WaitSignalNodeData.NormalizeSignalKey(change.newValue);
                context.ApplyEdit("Change signal key", () =>
                {
                    TypeData.SignalKey = normalizedKey;
                    signalKeyField.SetValueWithoutNotify(normalizedKey);
                    RefreshPreview();
                    RefreshValidation();
                });
            });

            root.Add(new HelpBox("Leading and trailing whitespace is removed. Signal keys are case-sensitive.", HelpBoxMessageType.Info));
            RefreshValidation();
            return root;

            void RefreshValidation()
            {
                missingKeyWarning.style.display = string.IsNullOrWhiteSpace(TypeData.SignalKey)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        /// <summary>Updates the title with the current signal key.</summary>
        private void RefreshPreview()
        {
            string key = TypeData?.GetNormalizedSignalKey();
            title = string.IsNullOrWhiteSpace(key) ? "WAIT SIGNAL: No Key" : $"WAIT SIGNAL: {key}";
        }
    }
}
