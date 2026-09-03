using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>Quest 진행을 시작하는 명시적인 시작 노드입니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Entry/Quest Start")]
    public sealed class QuestStartNode : GraphNode<QuestStartNodeData>
    {
        public override Vector2 DefaultSize => new(170f, 90f);

        /// <summary>같은 그래프에 두 번째 Quest Start가 생성되는 것을 막습니다.</summary>
        protected override void InitializeNewData(
            QuestStartNodeData data,
            GraphNodeCreationContext creationContext)
        {
            if (creationContext.ExistingNodes.Any(node => node is QuestStartNodeData))
            {
                throw new System.InvalidOperationException("Quest 그래프에는 Quest Start 노드를 하나만 만들 수 있습니다.");
            }
        }

        /// <summary>시작 노드의 단일 다음 흐름 출력 포트를 만듭니다.</summary>
        protected override void Draw()
        {
            title = "QUEST START";
            Port next = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            next.portName = QuestPortNames.Next;
            outputContainer.Add(next);
            AddToClassList("start-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>진행 시작점의 역할을 설명하며 별도로 수정할 필드는 제공하지 않습니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            return new HelpBox(
                "Quest 진행은 여기에서 시작합니다. 상호작용 진입점은 별도이며 Quest를 시작하지 않습니다.",
                HelpBoxMessageType.Info);
        }
    }

    /// <summary>플레이어가 프로젝트에서 정의한 대상과 상호작용할 때 조회 경로 탐색에 사용하는 시작점입니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Entry/Interaction")]
    public sealed class QuestInteractionEntryNode : GraphNode<QuestInteractionEntryNodeData>
    {
        public override Vector2 DefaultSize => new(190f, 100f);

        /// <summary>상호작용 경로의 다음 흐름 출력 포트를 만듭니다.</summary>
        protected override void Draw()
        {
            RefreshTitle();
            Port next = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            next.portName = QuestPortNames.Next;
            outputContainer.Add(next);
            AddToClassList("quest-entry-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        private void RefreshTitle()
        {
            title = $"INTERACT: {(string.IsNullOrWhiteSpace(NodeData.TargetId) ? "Any" : NodeData.TargetId)}";
        }

        /// <summary>프로젝트에서 정의하는 상호작용 대상 필드를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new Label("Interaction Entry"));
            root.Add(new HelpBox(
                "상호작용 대상에게 제공할 대화 또는 Quest 후보를 찾는 진입점입니다. " +
                "모든 대상과 일치시키려면 Target ID를 비워 두세요.",
                HelpBoxMessageType.Info));

            var targetField = new TextField("Target ID")
            {
                value = NodeData.TargetId ?? string.Empty,
                isDelayed = true
            };
            targetField.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit("Change interaction target", () =>
                {
                    NodeData.TargetId = change.newValue?.Trim() ?? string.Empty;
                    RefreshTitle();
                });
            });
            root.Add(targetField);
            return root;
        }
    }
}
