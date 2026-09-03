using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>현재 Quest를 선택 가능한 후보 또는 차단 사유가 있는 후보로 제공하는 종착 노드입니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Dialogue/Offer Quest")]
    public sealed class QuestOfferNode : GraphNode<QuestOfferNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(250f, 180f);

        /// <summary>여러 조건 경로가 도달할 수 있는 입력 포트 하나를 만듭니다.</summary>
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

        /// <summary>후보 상태, 차단 이유, 선택적인 대화와 우선순위 입력 요소를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new HelpBox(
                "이 종점에 도달하면 현재 Quest가 상호작용 목록에 표시됩니다. " +
                "수락할 수 없는 경로도 표시하려면 Is Available을 끄고 이유를 작성하세요.",
                HelpBoxMessageType.Info));

            var availableField = new Toggle("Is Available")
            {
                value = NodeData.IsAvailable
            };
            var reasonField = new TextField("Block Reason")
            {
                value = NodeData.BlockReason ?? string.Empty,
                multiline = true,
                isDelayed = true
            };
            reasonField.SetEnabled(!NodeData.IsAvailable);

            availableField.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit("Change quest offer availability", () =>
                {
                    NodeData.IsAvailable = change.newValue;
                    reasonField.SetEnabled(!change.newValue);
                    RefreshTitle();
                });
            });
            root.Add(availableField);

            reasonField.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit(
                    "Change quest offer block reason",
                    () => NodeData.BlockReason = change.newValue);
            });
            root.Add(reasonField);

            root.Add(QuestEditorFields.CreateDialogueEntryPointField(
                NodeData.DialogueEntryPoint,
                editHandler,
                entryPoint => NodeData.DialogueEntryPoint = entryPoint));

            var priorityField = new IntegerField("Priority")
            {
                value = NodeData.Priority,
                isDelayed = true
            };
            priorityField.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit(
                    "Change quest offer priority",
                    () => NodeData.Priority = change.newValue);
            });
            root.Add(priorityField);
            return root;
        }

        private void RefreshTitle()
        {
            title = NodeData.IsAvailable ? "QUEST OFFER" : "QUEST OFFER: BLOCKED";
        }
    }
}
