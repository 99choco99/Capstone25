using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>Dialogue 그래프 시작점 하나를 대화 선택기에 제공하는 Quest 그래프 종착 노드입니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Dialogue/Request")]
    public sealed class DialogueRequestNode : GraphNode<DialogueRequestNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(250f, 150f);

        /// <summary>종착 노드의 입력 포트를 만듭니다.</summary>
        protected override void Draw()
        {
            RefreshTitle();
            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);
            AddToClassList("end-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>선택한 Dialogue 그래프와 시작점이 보이도록 노드 제목을 갱신합니다.</summary>
        private void RefreshTitle()
        {
            string graphName = NodeData.DialogueReference.GraphAsset == null ? "None" : NodeData.DialogueReference.GraphAsset.name;
            title = $"Request: {graphName} ({NodeData.DialogueReference.EntryId})";
        }

        /// <summary>Dialogue 참조, 주제명과 우선순위 입력 요소를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new HelpBox("이 종점에 도달하면 선택한 대화 진입점을 대화 선택기가 사용할 수 있습니다.", HelpBoxMessageType.Info));

            var graphField = new ObjectField("Graph Asset")
            {
                objectType = typeof(DialogueContainer),
                allowSceneObjects = false,
                value = NodeData.DialogueReference.GraphAsset
            };
            var entryField = new PopupField<string>(
                "Entry ID",
                GetEntryChoices(NodeData.DialogueReference.GraphAsset, NodeData.DialogueReference.EntryId),
                0);
            SelectCurrentEntry(entryField, NodeData.DialogueReference.EntryId);
            entryField.SetEnabled(NodeData.DialogueReference.GraphAsset != null);
            var openGraphButton = new Button(() =>
            {
                if (NodeData.DialogueReference.GraphAsset != null)
                {
                    UniversalGraphWindow.OpenWindow(NodeData.DialogueReference.GraphAsset);
                }
            })
            {
                text = "OpenWindow Dialogue Graph"
            };
            openGraphButton.SetEnabled(NodeData.DialogueReference.GraphAsset != null);

            graphField.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit("Change dialogue graph", () =>
                {
                    NodeData.DialogueReference.GraphAsset = change.newValue as DialogueContainer;
                    List<string> entries = GetEntryChoices(
                        NodeData.DialogueReference.GraphAsset,
                        DialogueStartNodeData.DefaultEntryId);
                    NodeData.DialogueReference.EntryId = entries[0];
                    entryField.choices = entries;
                    entryField.SetValueWithoutNotify(entries[0]);
                    entryField.SetEnabled(NodeData.DialogueReference.GraphAsset != null);
                    openGraphButton.SetEnabled(NodeData.DialogueReference.GraphAsset != null);
                    RefreshTitle();
                });
            });
            root.Add(graphField);

            entryField.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit("Change dialogue entry", () =>
            {
                NodeData.DialogueReference.EntryId = change.newValue;
                RefreshTitle();
            }));
            root.Add(entryField);
            root.Add(openGraphButton);

            root.Add(CreateDelayedTextField("Topic Name", NodeData.TopicName, "Change dialogue topic", value =>
            {
                NodeData.TopicName = value;
                RefreshTitle();
            }, editHandler));

            var priorityField = new IntegerField("Priority")
            {
                value = NodeData.Priority,
                isDelayed = true
            };
            priorityField.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit("Change dialogue priority", () =>
                {
                    NodeData.Priority = change.newValue;
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
            NodeInspectorEditHandler editHandler)
        {
            var field = new TextField(label)
            {
                value = value ?? string.Empty,
                isDelayed = true
            };
            field.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit(undoName, () => applyValue(change.newValue)));
            return field;
        }

        private static List<string> GetEntryChoices(DialogueContainer graph, string currentEntryId)
        {
            var entries = graph?.Nodes?
                .OfType<DialogueStartNodeData>()
                .Select(entry => entry.EntryId)
                .Distinct()
                .OrderBy(entry => entry == DialogueStartNodeData.DefaultEntryId ? 0 : 1)
                .ThenBy(entry => entry, System.StringComparer.Ordinal)
                .ToList()
                ?? new List<string>();

            if (entries.Count == 0)
            {
                entries.Add(currentEntryId);
            }
            else if (!entries.Contains(currentEntryId))
            {
                entries.Add(currentEntryId);
            }

            return entries;
        }

        private static void SelectCurrentEntry(PopupField<string> field, string entryId)
        {
            string selected = field.choices.FirstOrDefault(choice => choice == entryId)
                              ?? field.choices[0];
            field.SetValueWithoutNotify(selected);
        }
    }
}
