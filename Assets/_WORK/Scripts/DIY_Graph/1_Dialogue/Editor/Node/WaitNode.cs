using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>Pauses the current dialogue flow for a configured duration.</summary>
    [GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Wait Node")]
    public sealed class WaitNode : GraphNode<WaitNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(190f, 120f);

        /// <summary>Rejects invalid duration data before a malformed asset is rendered.</summary>
        protected override void ValidateDataForView(WaitNodeData data)
        {
            if (float.IsNaN(data.DurationSeconds) || float.IsInfinity(data.DurationSeconds) || data.DurationSeconds < 0f)
            {
                throw new InvalidOperationException("Wait duration must be a finite value greater than or equal to zero.");
            }
        }

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

            AddToClassList("wait-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>Creates duration and time-scale controls.</summary>
        public override VisualElement CreateInspector(NodeInspectorContext context)
        {
            var root = new VisualElement();
            var titleLabel = new Label("Wait");
            titleLabel.AddToClassList("inspector-title");
            root.Add(titleLabel);

            var durationField = new FloatField("Duration (Seconds)")
            {
                value = TypeData.DurationSeconds,
                isDelayed = true
            };
            durationField.RegisterValueChangedCallback(change =>
            {
                float duration = change.newValue;
                if (float.IsNaN(duration) || float.IsInfinity(duration))
                {
                    durationField.SetValueWithoutNotify(TypeData.DurationSeconds);
                    return;
                }

                duration = Mathf.Max(0f, duration);
                context.ApplyEdit("Change wait duration", () =>
                {
                    TypeData.DurationSeconds = duration;
                    durationField.SetValueWithoutNotify(duration);
                    RefreshPreview();
                });
            });
            root.Add(durationField);

            var useUnscaledTimeField = new Toggle("Use Unscaled Time")
            {
                value = TypeData.UseUnscaledTime
            };
            useUnscaledTimeField.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit("Change wait time source", () =>
                {
                    TypeData.UseUnscaledTime = change.newValue;
                    RefreshPreview();
                });
            });
            root.Add(useUnscaledTimeField);
            root.Add(new HelpBox("Unscaled time continues while Time.timeScale is zero.", HelpBoxMessageType.Info));
            return root;
        }

        /// <summary>Updates the title with the current duration and clock source.</summary>
        private void RefreshPreview()
        {
            string timeSource = TypeData != null && TypeData.UseUnscaledTime ? "Unscaled" : "Scaled";
            float duration = TypeData?.DurationSeconds ?? 0f;
            title = $"WAIT: {duration:0.###}s ({timeSource})";
        }
    }
}
