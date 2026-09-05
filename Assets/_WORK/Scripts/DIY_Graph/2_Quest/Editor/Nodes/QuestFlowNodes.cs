using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>흐름을 받고 여러 분기로 이어질 수 있는 Quest 노드의 공통 포트 구조입니다.</summary>
    public abstract class QuestFlowNode<T> : GraphNode<T> where T : NodeBaseData, new()
    {
        protected abstract string NodeTitle { get; }

        protected virtual bool HasOutput => true;

        /// <summary>공통 다중 입력 포트와 선택적인 다중 출력 포트를 만듭니다.</summary>
        protected override void Draw()
        {
            RefreshTitle();

            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            input.portName = QuestPortNames.Input;
            inputContainer.Add(input);

            if (HasOutput)
            {
                Port next = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
                next.portName = QuestPortNames.Next;
                outputContainer.Add(next);
            }

            RefreshPorts();
            RefreshExpandedState();
        }

        /// <summary>현재 직렬화 데이터로 화면에 표시되는 노드 제목을 다시 만듭니다.</summary>
        protected void RefreshTitle()
        {
            title = NodeTitle;
        }
    }

    /// <summary>일치하는 게임 이벤트가 목표량에 도달할 때까지 기다립니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Progress/Objective")]
    public sealed class QuestObjectiveNode : QuestFlowNode<QuestObjectiveNodeData>
    {
        protected override string NodeTitle =>
            $"OBJECTIVE: {(string.IsNullOrWhiteSpace(NodeData.ObjectiveType) ? "Unassigned" : NodeData.ObjectiveType)} " +
            $"x{Math.Max(1, NodeData.RequiredAmount)}";

        /// <summary>목표 이벤트, 대상, 수량, 참조와 설명 필드를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new Label("Objective"));

            var objectiveType = new TextField("Objective Type")
            {
                value = NodeData.ObjectiveType ?? string.Empty,
                isDelayed = true
            };
            objectiveType.RegisterValueChangedCallback(change =>
                editHandler.ApplyDataEdit("Change objective type", () =>
                {
                    NodeData.ObjectiveType = (change.newValue ?? string.Empty).Trim();
                    RefreshTitle();
                }));
            root.Add(objectiveType);

            var targetId = new IntegerField("Target ID") { value = NodeData.TargetId, isDelayed = true };
            targetId.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit("Change objective target", () =>
            {
                NodeData.TargetId = change.newValue;
                RefreshTitle();
            }));
            root.Add(targetId);

            var targetPrefab = new ObjectField("Authoring Reference")
            {
                objectType = typeof(UnityEngine.Object),
                allowSceneObjects = false,
                value = NodeData.TargetPrefab
            };
            targetPrefab.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit(
                "Change objective reference",
                () => NodeData.TargetPrefab = change.newValue));
            root.Add(targetPrefab);

            var required = new IntegerField("Required Amount")
            {
                value = Math.Max(1, NodeData.RequiredAmount),
                isDelayed = true
            };
            required.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit("Change objective amount", () =>
            {
                NodeData.RequiredAmount = Math.Max(1, change.newValue);
                RefreshTitle();
            }));
            root.Add(required);

            var description = new TextField("Description")
            {
                value = NodeData.ObjectiveDescription ?? string.Empty,
                multiline = true,
                isDelayed = true
            };
            description.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit(
                "Change objective description",
                () => NodeData.ObjectiveDescription = change.newValue));
            root.Add(description);
            return root;
        }

    }

    /// <summary>게임이 제공하는 Condition 결과를 통해 Quest 흐름을 분기합니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Condition/Custom")]
    public sealed class QuestConditionNode : GraphNode<QuestConditionNodeData>
    {
        public override Vector2 DefaultSize => new(210f, 120f);

        /// <summary>입력 하나와 서로 배타적인 True·False 분기 출력을 만듭니다.</summary>
        protected override void Draw()
        {
            RefreshTitle();
            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            input.portName = QuestPortNames.Input;
            inputContainer.Add(input);

            AddOutput(QuestPortNames.True);
            AddOutput(QuestPortNames.False);
            AddToClassList("condition-node");
            RefreshPorts();
            RefreshExpandedState();
        }

        private void AddOutput(string portName)
        {
            Port port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            port.portName = portName;
            outputContainer.Add(port);
        }

        private void RefreshTitle()
        {
            title = $"IF: {(string.IsNullOrWhiteSpace(NodeData.Condition.Key) ? "Unassigned" : NodeData.Condition.Key)}";
        }

        /// <summary>Attribute Condition 선택기와 인수 필드를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new HelpBox(
                "코드 작성 없이 그래프에 연결하려면 Attribute가 붙은 메서드를 선택하세요.",
                HelpBoxMessageType.Info));

            root.Add(MethodBindingInspector.Create(
                editHandler,
                "Quest Condition",
                NodeData.Condition,
                QuestMethodCatalog.GetMethodList(MethodKind.Condition),
                RefreshTitle));

            return root;
        }
    }

    /// <summary>서로 다른 모든 입력 분기가 도착하면 다음 흐름으로 진행합니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Flow/AND Gate")]
    public sealed class QuestAndGateNode : QuestFlowNode<QuestAndGateNodeData>
    {
        protected override string NodeTitle => "AND GATE";

        /// <summary>연결 상태에서 자동 계산한 필수 입력 분기 수를 설명합니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            int connectedSources = inputContainer.Children()
                .OfType<Port>()
                .SelectMany(port => port.connections)
                .Where(edge => edge?.output?.node != null)
                .Select(edge => edge.output.node)
                .Distinct()
                .Count();
            root.Add(new HelpBox(
                $"서로 다른 입력 분기 {connectedSources}개가 모두 도착할 때까지 기다립니다. " +
                "필요한 분기 수는 연결 상태에서 자동으로 계산합니다.",
                connectedSources >= 2 ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning));
            return root;
        }
    }

    /// <summary>노드에 도달하면 프로젝트에서 정의한 Action을 실행합니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Flow/Action")]
    public sealed class QuestActionNode : QuestFlowNode<QuestActionNodeData>
    {
        protected override string NodeTitle =>
            $"ACTION: {(string.IsNullOrWhiteSpace(NodeData.Action.Key) ? "Unassigned" : NodeData.Action.Key)}";

        /// <summary>Quest Attribute Action 선택기와 타입 기반 인수 입력 요소를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new HelpBox(
                "코드 작성 없이 그래프에 연결하려면 Attribute가 붙은 메서드를 선택하세요.",
                HelpBoxMessageType.Info));
            root.Add(MethodBindingInspector.Create(
                editHandler,
                "Quest Action",
                NodeData.Action,
                QuestMethodCatalog.GetMethodList(MethodKind.Action),
                RefreshTitle));
            return root;
        }
    }

    /// <summary>현재 Quest의 진행 단계를 변경합니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Flow/Change State")]
    public sealed class QuestStateChangeNode : QuestFlowNode<QuestStateChangeNodeData>
    {
        protected override string NodeTitle => $"STATE: {NodeData.NewState}";

        /// <summary>진행 단계 선택기를 만들고 수정 후 노드 제목을 갱신합니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var allowedStates = new List<QuestState>
            {
                QuestState.InProgress,
                QuestState.CanComplete,
                QuestState.TurnedIn
            };
            int selectedIndex = Math.Max(0, allowedStates.IndexOf(NodeData.NewState));
            var field = new PopupField<QuestState>("New State", allowedStates, selectedIndex);
            field.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit("Change quest state", () =>
            {
                NodeData.NewState = change.newValue;
                RefreshTitle();
            }));
            return field;
        }
    }

    /// <summary>선택적인 보상 Action을 실행하고 다음 노드로 진행합니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Completion/Reward")]
    public sealed class QuestRewardNode : QuestFlowNode<QuestRewardNodeData>
    {
        protected override string NodeTitle => string.IsNullOrWhiteSpace(NodeData.RewardAction.Key)
            ? "REWARD"
            : $"REWARD: {NodeData.RewardAction.Key}";

        /// <summary>실행할 선택적인 타입 기반 보상 Action을 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new HelpBox(
                "보상 Action만 실행합니다. Quest 상태 변경이 필요하면 State Change 노드를 연결하세요.",
                HelpBoxMessageType.Info));
            root.Add(MethodBindingInspector.Create(
                editHandler,
                "Optional Reward Action",
                NodeData.RewardAction,
                QuestMethodCatalog.GetMethodList(MethodKind.Action),
                RefreshTitle));
            return root;
        }
    }

    /// <summary>현재 Quest를 실패 상태로 종료합니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Completion/Fail")]
    public sealed class QuestFailNode : QuestFlowNode<QuestFailNodeData>
    {
        protected override string NodeTitle => "FAIL QUEST";
        protected override bool HasOutput => false;

        /// <summary>이 노드가 현재 Quest를 즉시 실패 상태로 종료함을 설명합니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            return new HelpBox(
                "이 노드에 도달하면 현재 Quest를 실패 상태로 종료합니다.",
                HelpBoxMessageType.Info);
        }
    }
}
