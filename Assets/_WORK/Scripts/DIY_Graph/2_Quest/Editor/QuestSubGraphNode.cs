using UnityEditor.Experimental.GraphView;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>참조한 Quest를 시작하고 완료되면 상위 그래프를 다시 진행합니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Flow/Sub-Quest Graph")]
    public sealed class QuestSubGraphNode : GraphNode<QuestSubGraphNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(200f, 100f);

        /// <summary>상위 흐름 입력 포트와 다음 흐름 출력 포트를 만듭니다.</summary>
        protected override void Draw()
        {
            RefreshTitle();

            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);

            Port next = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            next.portName = "Next";
            outputContainer.Add(next);

            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>표시되는 하위 Quest 식별자를 갱신합니다.</summary>
        private void RefreshTitle()
        {
            title = $"SUB-QUEST: {NodeData.SubQuestId}";
        }

        /// <summary>하위 Quest ID 선택 필드를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new Label("Sub-Quest"));
            root.Add(new HelpBox("참조한 하위 Quest가 완료된 뒤에만 상위 흐름을 다시 진행합니다.", HelpBoxMessageType.Info));

            Button openButton = new Button(OpenReferencedQuest)
            {
                text = "OpenWindow Sub-Quest Graph"
            };
            PopupField<int> questField = QuestEditorFields.CreateQuestIdField(
                "Sub-Quest",
                NodeData.SubQuestId,
                "Change sub-quest ID",
                editHandler,
                value =>
                {
                    NodeData.SubQuestId = value;
                    RefreshTitle();
                    RefreshOpenButton();
                });
            root.Add(questField);
            root.Add(openButton);
            RefreshOpenButton();
            return root;

            void RefreshOpenButton()
            {
                openButton.SetEnabled(QuestAssetIndex.Quests.Count(quest =>
                    quest != null && quest.questId == NodeData.SubQuestId) == 1);
            }

            void OpenReferencedQuest()
            {
                QuestContainer quest = QuestAssetIndex.Quests.SingleOrDefault(candidate =>
                    candidate != null && candidate.questId == NodeData.SubQuestId);
                if (quest != null)
                {
                    UniversalGraphWindow.OpenWindow(quest);
                }
            }
        }
    }
}
