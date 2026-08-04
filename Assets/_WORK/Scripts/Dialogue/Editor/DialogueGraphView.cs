using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    public DialogueGraphView()
    {
        //줌 기능 활성화
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());    //맵을 이동시킬 수 있음
        this.AddManipulator(new SelectionDragger());  //노드를 드래그 하여 이동시킬 수 있음
        this.AddManipulator(new RectangleSelector()); //노드를 드래그로 여러개 선택 가능

        //배경에 Grid 무늬 깔기
        GridBackground grid = new();
        Insert(0, grid);
        grid.StretchToParentSize();

        string[] guids = UnityEditor.AssetDatabase.FindAssets("DialogueGraphWindow t:StyleSheet");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            var styleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (styleSheet != null)
                styleSheets.Add(styleSheet);
        }
    }

    /// <summary>
    /// 노드 생성
    /// </summary>
    public DialogueNode CreateNode(string nodeName, Vector2 position)
    {
        DialogueNode dialogueNode = new()
        {
            title = nodeName,
            DialogueText = nodeName,
            GUID = Guid.NewGuid().ToString() // 고유 ID 부여
        };

        var inputPort = GeneratePort(dialogueNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        dialogueNode.inputContainer.Add(inputPort);

        var outputPort = GeneratePort(dialogueNode, Direction.Output, Port.Capacity.Single);
        outputPort.portName = "Next";
        dialogueNode.outputContainer.Add(outputPort);

        dialogueNode.RefreshExpandedState();        //현재 상태 그대로 데이터 새로고침
        dialogueNode.RefreshPorts();                //포트들 갱신

        // 마우스 위치 정확히 계산
        Vector2 localMousePos = contentViewContainer.WorldToLocal(position);
        dialogueNode.SetPosition(new Rect(localMousePos, new Vector2(150, 200)));

        // 캔버스에 노드 추가
        AddElement(dialogueNode);

        return dialogueNode;
    }

    /// <summary>
    /// 포트를 생성하는 함수
    /// </summary>
    private Port GeneratePort(DialogueNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        ///포트를 옆으로 쌓을지 위로 쌓을지, 
        ///포트가 나가는 곳인지 들어오는 곳인지, 
        ///하나만 가능한지 여러개 가능한지,
        ///마지막은 쉐이더 그래프. 현재는 아무값이나 넣어도 됨.
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }



    //================= override 해서 기능 추가 ===================

    /// <summary>
    /// 마우스 우클릭 메뉴 만들기
    /// </summary>
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);
        evt.menu.AppendAction("Add Dialogue Node", actionEvent => CreateNode("DialogueText", actionEvent.eventInfo.localMousePosition));
    }


    /// <summary>
    /// 연결할 두 포트가 서로 호환이 되는지 검사
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        List<Port> compatiblePorts = new();

        // 도화지에 있는 모든 포트들을 검사
        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });

        return compatiblePorts;
    }
}
