using System;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>Choice 노드의 선택지 목록을 인스펙터에 그리는 클래스</summary>
    internal static class DialogueChoiceNodeInspectorDrawer
    {
        /// <summary>선택한 Choice 노드의 인스펙터를 생성</summary>
        public static VisualElement Draw(DialogueChoiceNode selectedNode, NodeInspectorEditHandler editHandler)
        {
            VisualElement root = new();

            root.Add(new HelpBox("표시 가능한 선택지가 없으면 Default 포트로 즉시 진행합니다.", HelpBoxMessageType.Info));
            root.Add(CreateChoicesSector(selectedNode, editHandler));
            return root;
        }

        /// <summary>선택지 추가 버튼과 선택지들이 들어갈 공간을 생성</summary>
        private static VisualElement CreateChoicesSector(DialogueChoiceNode selectedNode, NodeInspectorEditHandler editHandler)
        {
            VisualElement root = new();
            Label title = new("Choices");
            title.AddToClassList("choice-title");
            root.Add(title);

            VisualElement choicesContainer = new();
            root.Add(choicesContainer);

            //동적으로 생성하는 버튼
            Button addButton = new(() =>
            {
                editHandler.ApplyStructureEdit("Add dialogue choice", () =>
                {
                    DialogueChoiceData choice = new()
                    {
                        PortName = Guid.NewGuid().ToString(),
                        ChoiceText = "New Choice"
                    };
                    selectedNode.NodeData.Choices.Add(choice);
                    selectedNode.AddChoicePort(choice);
                    RedrawChoices();
                });
            })
            {
                text = "+ Add Choice"
            };
            addButton.AddToClassList("add-choice-btn");
            root.Add(addButton);

            RedrawChoices();
            return root;

            //내부함수. 선택지 생성 후 다시 인스펙터에서 선택지UI 를 그려야 하기 때문
            void RedrawChoices()
            {
                choicesContainer.Clear();
                foreach (DialogueChoiceData choice in selectedNode.NodeData.Choices)
                {
                    if (choice != null)
                    {
                        choicesContainer.Add(CreateChoiceField(selectedNode, choice, editHandler, RedrawChoices));
                    }
                }
            }
        }

        /// <summary>인스펙터에 선택지 하나에 대한 정보를 넣을 수 있는 필드 생성</summary>
        private static Box CreateChoiceField(DialogueChoiceNode selectedNode, DialogueChoiceData choice, NodeInspectorEditHandler editHandler, Action redrawChoices)
        {
            Box box = new();
            box.AddToClassList("choice-box");

            //삭제 버튼 추가
            Button deleteButton = new(() =>
            {
                editHandler.ApplyStructureEdit("Delete dialogue choice", () =>
                {
                    selectedNode.RemoveChoicePort(choice);
                    selectedNode.NodeData.Choices.Remove(choice);
                    selectedNode.RefreshPreview();
                    redrawChoices?.Invoke();
                });
            })
            {
                text = "×",
                tooltip = "선택지 삭제"
            };
            deleteButton.AddToClassList("choice-delete-btn");
            box.Add(deleteButton);

            //텍스트 영역
            TextField textField = new("Text")
            {
                value = choice.ChoiceText ?? string.Empty,
                multiline = true
            };
            textField.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit("Change choice text", () => choice.ChoiceText = change.newValue);
            });
            box.Add(textField);

            //선택 시 실행할 Action
            box.Add(MethodBindingInspector.Create(editHandler, "선택지 공개 조건", choice.VisibilityCondition, DialogueMethodCatalog.GetMethodList(MethodKind.Condition)));
            box.Add(MethodBindingInspector.Create(editHandler, "실행할 Action", choice.SelectionAction, DialogueMethodCatalog.GetMethodList(MethodKind.Action)));

            return box;
        }
    }
}
