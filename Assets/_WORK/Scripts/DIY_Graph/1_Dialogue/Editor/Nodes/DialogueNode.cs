using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>화자와 대화문 한 줄을 표시</summary>
    [GraphNodeEditor(typeof(DialogueContainer), "Dialogue/Dialogue Node")]
    public class DialogueNode : GraphNode<DialogueNodeData>
    {
        //노드밑부분에 있는 대화문 보여주는 곳
        private Label fullTextLabel;

        public override Vector2 DefaultSize => new(150f, 200f);

        /// <summary>흐름 입력 하나와 다음 흐름 출력 하나를 만들기</summary>
        protected override void Draw()
        {
            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);

            Port next = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            next.portName = "Next";
            outputContainer.Add(next);

            fullTextLabel = new Label();
            fullTextLabel.AddToClassList("node-full-text");
            extensionContainer.Add(fullTextLabel);

            RefreshPreview();
            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>대화문을 수정하면 제목과 전체 문장 미리보기를 갱신</summary>
        public void RefreshPreview()
        {
            string speaker = string.IsNullOrEmpty(NodeData.SpeakerName) ? "Unknown" : NodeData.SpeakerName;
            string preview = NodeData.DialogueText ?? string.Empty;
            if (preview.Length > 15)
            {
                preview = preview.Substring(0, 15) + "...";
            }

            title = $"{speaker} : {preview}";
            if (fullTextLabel != null)
            {
                fullTextLabel.text = string.IsNullOrEmpty(NodeData.DialogueText)? "(대사 없음)" : NodeData.DialogueText;
            }
        }

        /// <summary>대화문, 화자와 진입 이벤트 편집 요소를 인스펙터에 그리기</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            return DialogueNodeInspectorDrawer.Draw(this, editHandler);
        }
    }
}
