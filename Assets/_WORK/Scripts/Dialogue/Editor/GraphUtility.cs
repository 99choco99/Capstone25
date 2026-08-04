using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GraphUtility
{
    private readonly DialogueGraphView targetGraphView;

    // 생성자를 public으로 만들어서 외부에서 new로 팍팍 찍어낼 수 있게 엽니다!
    public GraphUtility(DialogueGraphView targetGraphView)
    {
        this.targetGraphView = targetGraphView;
    }

    // 현재 그래프의 모든 선과 노드를 리스트로 가져오기
    private List<Edge> Edges => targetGraphView.edges.ToList();
    private List<DialogueNode> Nodes => targetGraphView.nodes.ToList().Cast<DialogueNode>().ToList();

    // 저장 기능 (이미 열려있는 컨테이너 덮어쓰기)
    public void SaveGraph(DialogueContainer dialogueContainer)
    {
        if (dialogueContainer == null) return;

        // 1. 기존 데이터 초기화 (덮어쓰기 위해)
        dialogueContainer.NodeLinks.Clear();
        dialogueContainer.DialogueNodeData.Clear();

        // 2. 연결 정보 저장하기
        // 양쪽 끝이 정상적으로 연결된 선들만
        Edge[] connectedPorts = Edges.Where(x => x.input.node != null).ToArray();
        foreach (var edge in connectedPorts)
        {
            var outputNode = edge.output.node as DialogueNode; // 선이 출발한 노드
            var inputNode = edge.input.node as DialogueNode;   // 선이 도착한 노드

            dialogueContainer.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = outputNode.GUID,
                PortName = edge.output.portName,
                TargetNodeGuid = inputNode.GUID
            });
        }

        // 3. 노드(네모 박스)들의 텍스트 내용과 화면상 픽셀 좌표 저장하기
        foreach (var dialogueNode in Nodes)
        {
            dialogueContainer.DialogueNodeData.Add(new DialogueNodeData
            {
                Guid = dialogueNode.GUID,
                DialogueText = dialogueNode.DialogueText,
                Position = dialogueNode.GetPosition().position
            });
        }

        // 4. 에셋이 변경되었음을 유니티에 알리고 디스크에 저장!
        EditorUtility.SetDirty(dialogueContainer);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"저장 완료! {dialogueContainer.name} 파일이 성공적으로 덮어씌워졌습니다.");
    }

    // 불러오기 기능
    public void LoadGraph(DialogueContainer dialogueContainer)
    {
        if (dialogueContainer == null) return;

        // 1. 도화지(캔버스) 위에 있던 기존 찌꺼기들 싹 지우기
        ClearGraph();

        // 2. 읽어온 데이터(글자)를 바탕으로 네모 박스(Node) 그래픽 다시 생성하기
        GenerateNodes(dialogueContainer);

        // 3. 읽어온 데이터(글자)를 바탕으로 선(Edge) 그래픽 다시 긋기
        ConnectNodes(dialogueContainer);
    }

    private void ClearGraph()
    {
        // 도화지에 있는 선 지우기
        foreach (var edge in Edges)
        {
            targetGraphView.RemoveElement(edge);
        }

        // 도화지에 있는 노드 지우기
        foreach (var node in Nodes)
        {
            targetGraphView.RemoveElement(node);
        }
    }

    private void GenerateNodes(DialogueContainer dialogueContainer)
    {
        foreach (var nodeData in dialogueContainer.DialogueNodeData)
        {
            // 노드 껍데기를 생성하고, 옛날 좌표 위치에 똑같이 갖다 놓는다.
            var tempNode = targetGraphView.CreateNode(nodeData.DialogueText, nodeData.Position);
            
            // 💡[핵심] 새로 생성된 노드의 고유 ID를 방금 생성된 랜덤값이 아니라, 저장되어 있던 옛날 ID로 강제 덮어쓰기!
            tempNode.GUID = nodeData.Guid; 
        }
    }

    private void ConnectNodes(DialogueContainer dialogueContainer)
    {
        // 빠른 검색을 위해 도화지에 뿌려진 노드들을 딕셔너리로 묶어둔다 (고유 ID를 열쇠로 사용)
        var nodesDict = Nodes.ToDictionary(node => node.GUID, node => node);

        foreach (var nodeLink in dialogueContainer.NodeLinks)
        {
            // 선이 출발했던 옛날 노드(Base)와 도착했던 옛날 노드(Target)를 도화지에서 찾는다.
            if (nodesDict.TryGetValue(nodeLink.BaseNodeGuid, out var baseNode) &&
                nodesDict.TryGetValue(nodeLink.TargetNodeGuid, out var targetNode))
            {
                // 출발 노드에서 옛날과 똑같은 이름을 가진 Output 구멍 찾기
                var outputPort = baseNode.outputContainer.Children().Cast<Port>().First(x => x.portName == nodeLink.PortName);
                
                // 도착 노드에서 Input 구멍 찾기
                var inputPort = targetNode.inputContainer.Children().Cast<Port>().First(x => x.direction == Direction.Input);

                // 그래픽 선(Edge)을 생성하고 양쪽 구멍에 꽂는다.
                var edge = new Edge
                {
                    output = outputPort,
                    input = inputPort
                };

                edge?.input.Connect(edge);
                edge?.output.Connect(edge);

                // 도화지에 선 그리기
                targetGraphView.AddElement(edge);
            }
        }
    }
}
