using UnityEditor.Experimental.GraphView;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>참조한 Quest를 시작하고 지정 상태가 될 때까지 현재 흐름을 기다립니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Flow/Wait For Quest")]
    public sealed class QuestWaitForQuestNode : GraphNode<QuestWaitForQuestNodeData>
    {
        /// <inheritdoc />
        public override Vector2 DefaultSize => new Vector2(200f, 100f);

        /// <summary>상위 흐름 입력 포트와 다음 흐름 출력 포트를 만듭니다.</summary>
        protected override void Draw()
        {
            RefreshTitle();

            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            input.portName = QuestPortNames.Input;
            inputContainer.Add(input);

            Port next = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            next.portName = QuestPortNames.Next;
            outputContainer.Add(next);

            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>표시되는 대상 Quest 식별자를 갱신합니다.</summary>
        private void RefreshTitle()
        {
            title = $"WAIT QUEST: {NodeData.TargetQuestId}";
        }

        /// <summary>기다릴 Quest ID와 상태 선택 필드를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new Label("Wait For Quest"));
            root.Add(new HelpBox("참조한 Quest가 지정한 상태가 되면 현재 흐름을 다시 진행합니다.", HelpBoxMessageType.Info));

            Button openButton = new Button(OpenReferencedQuest)
            {
                text = "Open Target Quest Graph"
            };
            PopupField<int> questField = QuestEditorFields.CreateQuestIdField(
                "Target Quest",
                NodeData.TargetQuestId,
                "Change target quest ID",
                editHandler,
                value =>
                {
                    NodeData.TargetQuestId = value;
                    RefreshTitle();
                    RefreshOpenButton();
                });
            root.Add(questField);
            var stateField = new EnumField("Required State", NodeData.RequiredState);
            stateField.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit("Change required quest state", () =>
            {
                NodeData.RequiredState = (QuestState)change.newValue;
            }));
            root.Add(stateField);
            root.Add(openButton);
            RefreshOpenButton();
            return root;

            void RefreshOpenButton()
            {
                openButton.SetEnabled(QuestAssetIndex.Quests.Count(quest =>
                    quest != null && quest.QuestId == NodeData.TargetQuestId) == 1);
            }

            void OpenReferencedQuest()
            {
                QuestContainer quest = QuestAssetIndex.Quests.SingleOrDefault(candidate =>
                    candidate != null && candidate.QuestId == NodeData.TargetQuestId);
                if (quest != null)
                {
                    UniversalGraphWindow.OpenWindow(quest);
                }
            }
        }
    }
}
