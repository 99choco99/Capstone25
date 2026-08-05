using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public static class DialogueGraphSerializer
{
    /// <summary>
    /// 그래프를 메모리에 임시 저장
    /// </summary>
    public static void SaveGraphToMemory(DialogueGraphView view, DialogueContainer container)
    {
        if (container == null) return;

        List<Edge> Edges = view.GetEdges();
        List<DialogueNode> Nodes = view.GetNodes<DialogueNode>();


        container.NodeLinks.Clear();
        container.DialogueNodeData.Clear();

        // 연결 정보 담기
        Edge[] connectedPorts = Edges.Where(x => x.input.node != null && x.output.node != null).ToArray();
        foreach (var edge in connectedPorts)
        {
            DialogueNode outputNode = edge.output.node as DialogueNode; // 선이 출발한 노드
            DialogueNode inputNode = edge.input.node as DialogueNode;   // 선이 도착한 노드

            container.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = outputNode.GUID,
                PortName = edge.output.portName,
                TargetNodeGuid = inputNode.GUID
            });
        }

        // 노드의 텍스트 내용과 좌표 담기
        foreach (var dialogueNode in Nodes)
        {
            container.DialogueNodeData.Add(new DialogueNodeData
            {
                Guid = dialogueNode.GUID,
                DialogueText = dialogueNode.DialogueText,
                Position = dialogueNode.position
            });
        }

        //메모리에 저장
        EditorUtility.SetDirty(container);
    }

    /// <summary>
    /// 그래프 불러오기 기능
    /// </summary>
    public static void LoadGraph(DialogueGraphView view, DialogueContainer container)
    {
        if (container == null) return;
        ClearGraph(view);

        LoadNodes(view, container);

        LoadEdges(view, container);
    }

    /// <summary>
    /// 그래프 초기화
    /// </summary>
    private static void ClearGraph(DialogueGraphView view)
    {
        foreach (var edge in view.GetEdges())
        {
            view.RemoveElement(edge);
        }

        foreach (var node in view.GetNodes<DialogueNode>())
        {
            view.RemoveElement(node);
        }
    }

    /// <summary>
    /// 노드 불러오기
    /// </summary>
    private static void LoadNodes(DialogueGraphView view, DialogueContainer container)
    {
        foreach (var nodeData in container.DialogueNodeData)
        {
            DialogueNode tempNode = view.CreateNode(nodeData.DialogueText, nodeData.Position);
            tempNode.GUID = nodeData.Guid;
        }
    }


    /// <summary>
    /// 노드 연결 불러오기
    /// </summary>
    private static void LoadEdges(DialogueGraphView view, DialogueContainer container)
    {
        Dictionary<string, DialogueNode> nodesDict = view.GetNodes<DialogueNode>().ToDictionary(n => n.GUID);

        foreach (var nodeLink in container.NodeLinks)
        {
            if (nodesDict.TryGetValue(nodeLink.BaseNodeGuid, out DialogueNode baseNode) &&
                nodesDict.TryGetValue(nodeLink.TargetNodeGuid, out DialogueNode targetNode))
            {

                Port outputPort = baseNode.outputContainer.Children().OfType<Port>().FirstOrDefault(x => x.portName == nodeLink.PortName);
                Port inputPort = targetNode.inputContainer.Children().OfType<Port>().FirstOrDefault(x => x.direction == Direction.Input);

                Edge edge = new()
                {
                    output = outputPort,
                    input = inputPort
                };

                //지금 엣지만 포트정보를 알고있으니까 포트한테도 엣지 정보를 주는거
                inputPort.Connect(edge);
                outputPort.Connect(edge);

                // 도화지에 선 그리기
                view.AddElement(edge);
            }
        }
    }
}
