using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>Visual editor node for a named dialogue entry point.</summary>
    [GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Add Entry Node")]
    public class StartNode : GraphNode<StartNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(180f, 100f);

        /// <summary>Assigns a unique default entry identifier to a newly created node.</summary>
        protected override void InitializeNewData(StartNodeData data, GraphNodeCreationContext context)
        {
            var usedEntryIds = new HashSet<string>(
                context.ExistingNodes
                    .OfType<StartNodeData>()
                    .Select(entry => entry.GetNormalizedEntryId()),
                StringComparer.OrdinalIgnoreCase);

            if (!usedEntryIds.Contains(StartNodeData.DefaultEntryId))
            {
                data.EntryId = StartNodeData.DefaultEntryId;
                return;
            }

            int index = 2;
            string entryId;
            do
            {
                entryId = $"Entry_{index++}";
            }
            while (usedEntryIds.Contains(entryId));

            data.EntryId = entryId;
        }

        /// <summary>Creates the node's single outgoing flow port.</summary>
        protected override void Draw()
        {
            RefreshTitle();
            Port nextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            nextPort.portName = "Next";
            outputContainer.Add(nextPort);
            AddToClassList("start-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>Creates the entry identifier editor and reports duplicate names immediately.</summary>
        public override VisualElement CreateInspector(NodeInspectorContext context)
        {
            var root = new VisualElement();
            var title = new Label("Entry Point");
            title.AddToClassList("inspector-title");
            root.Add(title);
            root.Add(new Label("External code starts a dialogue by providing this graph and Entry ID."));

            var entryIdField = new TextField("Entry ID")
            {
                value = TypeData.GetNormalizedEntryId(),
                isDelayed = true
            };
            root.Add(entryIdField);

            var duplicateWarning = new HelpBox("Entry IDs must be unique within a graph.", HelpBoxMessageType.Error);
            root.Add(duplicateWarning);

            entryIdField.RegisterValueChangedCallback(change =>
            {
                string normalizedEntryId = StartNodeData.NormalizeEntryId(change.newValue);
                context.ApplyEdit("Change entry ID", () =>
                {
                    TypeData.EntryId = normalizedEntryId;
                    entryIdField.SetValueWithoutNotify(normalizedEntryId);
                    RefreshTitle();
                    RefreshValidation();
                });
            });

            RefreshValidation();
            return root;

            void RefreshValidation()
            {
                string currentEntryId = StartNodeData.NormalizeEntryId(entryIdField.value);
                UniversalGraphView graphView = this.GetFirstAncestorOfType<UniversalGraphView>();
                int matches = graphView == null
                    ? 0
                    : graphView.nodes
                        .OfType<StartNode>()
                        .Count(node => node.TypeData != null
                            && string.Equals(node.TypeData.GetNormalizedEntryId(), currentEntryId, StringComparison.OrdinalIgnoreCase));
                duplicateWarning.style.display = matches > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Updates the canvas title after the Entry ID changes.</summary>
        public void RefreshTitle()
        {
            title = $"ENTRY: {TypeData.GetNormalizedEntryId()}";
        }
    }
}
