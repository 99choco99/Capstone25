using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>Dialogue 노드의 인스펙터를 그리는 클래스(들어갈게 많아서 따로 뺌)</summary>
    internal static class DialogueNodeInspectorDrawer
    {
        /// <summary>선택한 Dialogue 노드의 인스펙터를 생성</summary>
        public static VisualElement Draw(DialogueNode selectedNode, NodeInspectorEditHandler editHandler)
        {
            VisualElement root = new ();

            root.Add(CreateSpeakerField(selectedNode, editHandler));
            root.Add(CreateDialogueField(selectedNode, editHandler));

            root.Add(MethodCallEditor.Create(editHandler, "진입 시 실행할 이벤트", selectedNode.NodeData.Event, DialogueMethodCatalog.GetMethods(MethodKind.Action)));
            return root;
        }

        /// <summary>인스펙터에 화자 이름을 넣는 필드 생성</summary>
        private static TextField CreateSpeakerField(DialogueNode selectedNode, NodeInspectorEditHandler editHandler)
        {
            TextField field = new ("Speaker")
            {
                value = selectedNode.NodeData.SpeakerName ?? string.Empty
            };

            field.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit("Change dialogue speaker", () =>
                {
                    selectedNode.NodeData.SpeakerName = change.newValue;
                    selectedNode.RefreshPreview();
                });
            });
            return field;
        }

        /// <summary>인스펙터에 대화 내용을 적는 필드 생성</summary>
        private static TextField CreateDialogueField(DialogueNode selectedNode, NodeInspectorEditHandler editHandler)
        {
            TextField field = new("Dialogue")
            {
                value = selectedNode.NodeData.DialogueText ?? string.Empty,
                multiline = true
            };
            field.AddToClassList("dialogue-field");

            field.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit("Change dialogue text", () =>
                {
                    selectedNode.NodeData.DialogueText = change.newValue;
                    selectedNode.RefreshPreview();
                });
            });
            return field;
        }

    }
}
