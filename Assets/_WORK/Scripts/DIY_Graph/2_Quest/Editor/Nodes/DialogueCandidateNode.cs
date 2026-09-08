using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>Dialogue 그래프 시작점 하나를 대화 선택기에 제공하는 Quest 그래프 종착 노드입니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Dialogue/Candidate")]
    public sealed class DialogueCandidateNode : GraphNode<DialogueCandidateNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(250f, 150f);

        /// <summary>종착 노드의 입력 포트를 만듭니다.</summary>
        protected override void Draw()
        {
            RefreshTitle();
            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            input.portName = QuestPortNames.Input;
            inputContainer.Add(input);
            AddToClassList("end-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>선택한 Dialogue 그래프와 시작점이 보이도록 노드 제목을 갱신합니다.</summary>
        private void RefreshTitle()
        {
            string graphName = NodeData.EntryPoint.GraphAsset == null ? "None" : NodeData.EntryPoint.GraphAsset.name;
            title = $"Candidate: {graphName} ({NodeData.EntryPoint.EntryId})";
        }

        /// <summary>Dialogue 시작점, 표시 이름과 선택 우선순위 입력 요소를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new HelpBox("이 종점에 도달하면 선택한 대화 진입점을 대화 선택기가 사용할 수 있습니다.", HelpBoxMessageType.Info));
            root.Add(QuestEditorFields.CreateDialogueEntryPointField(
                NodeData.EntryPoint,
                editHandler,
                entryPoint =>
                {
                    NodeData.EntryPoint = entryPoint;
                    RefreshTitle();
                }));

            var displayNameField = new TextField("Display Name")
            {
                value = NodeData.DisplayName ?? string.Empty,
                isDelayed = true
            };
            displayNameField.RegisterValueChangedCallback(change =>
                editHandler.ApplyDataEdit("Change dialogue display name", () =>
                {
                    NodeData.DisplayName = change.newValue;
                    RefreshTitle();
                }));
            root.Add(displayNameField);

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
    }
}
