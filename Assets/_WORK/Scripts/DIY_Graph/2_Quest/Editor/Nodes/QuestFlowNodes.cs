using System;
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
            input.portName = "Input";
            inputContainer.Add(input);

            if (HasOutput)
            {
                Port next = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
                next.portName = "Next";
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

            root.Add(CreateTextField("Event Type", NodeData.ObjectiveType, "Change objective event", value =>
            {
                NodeData.ObjectiveType = value.Trim();
                RefreshTitle();
            }, editHandler));

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

        private static TextField CreateTextField(
            string label,
            string value,
            string undoName,
            Action<string> apply,
            NodeInspectorEditHandler editHandler)
        {
            var field = new TextField(label) { value = value ?? string.Empty, isDelayed = true };
            field.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit(undoName, () => apply(change.newValue ?? string.Empty)));
            return field;
        }
    }

    /// <summary>게임이 제공하는 Condition 결과를 통해 Quest 흐름을 분기합니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Condition/Custom")]
    public sealed class QuestConditionBranchNode : GraphNode<QuestConditionBranchNodeData>
    {
        public override Vector2 DefaultSize => new(210f, 120f);

        /// <summary>입력 하나와 서로 배타적인 True·False 분기 출력을 만듭니다.</summary>
        protected override void Draw()
        {
            RefreshTitle();
            Port input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);

            AddOutput("True");
            AddOutput("False");
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

        /// <summary>Attribute Condition 선택기와 구형 Resolver 인수 필드를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new HelpBox(
                "코드 작성 없이 그래프에 연결하려면 Attribute가 붙은 메서드를 선택하세요. " +
                "등록되지 않은 키도 IQuestConditionResolver와 호환됩니다.",
                HelpBoxMessageType.Info));

            root.Add(MethodCallEditor.Create(
                editHandler,
                "Quest Condition",
                NodeData.Condition,
                QuestMethodCatalog.GetMethods(MethodKind.Condition),
                RefreshTitle));

            root.Add(new Label("Legacy Resolver Parameters"));
            root.Add(CreateIntegerField("Target ID", NodeData.TargetId, "Change condition target", value => NodeData.TargetId = value, editHandler));
            root.Add(CreateIntegerField("Required Value", NodeData.RequiredValue, "Change condition value", value => NodeData.RequiredValue = value, editHandler));
            return root;
        }

        private static IntegerField CreateIntegerField(
            string label,
            int value,
            string undoName,
            Action<int> apply,
            NodeInspectorEditHandler editHandler)
        {
            var field = new IntegerField(label) { value = value, isDelayed = true };
            field.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit(undoName, () => apply(change.newValue)));
            return field;
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
    public sealed class QuestActionTriggerNode : QuestFlowNode<QuestActionTriggerNodeData>
    {
        protected override string NodeTitle =>
            $"ACTION: {(string.IsNullOrWhiteSpace(NodeData.Action.Key) ? "Unassigned" : NodeData.Action.Key)}";

        /// <summary>Quest Attribute Action 선택기와 타입 기반 인수 입력 요소를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new HelpBox(
                "코드 작성 없이 그래프에 연결하려면 Attribute가 붙은 메서드를 선택하세요. " +
                "등록되지 않은 키도 IQuestActionReceiver 및 QuestEventManager와 호환됩니다.",
                HelpBoxMessageType.Info));
            root.Add(MethodCallEditor.Create(
                editHandler,
                "Quest Action",
                NodeData.Action,
                QuestMethodCatalog.GetMethods(MethodKind.Action),
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
            var field = new EnumField("New State", NodeData.NewState);
            field.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit("Change quest state", () =>
            {
                NodeData.NewState = (QuestState)change.newValue;
                RefreshTitle();
            }));
            return field;
        }
    }

    /// <summary>게임 Controller에 보상 지급과 현재 Quest 완료 처리를 요청합니다.</summary>
    [GraphNodeEditor(typeof(QuestContainer), "Quest/Completion/Reward")]
    public sealed class QuestRewardNode : QuestFlowNode<QuestRewardNodeData>
    {
        protected override string NodeTitle => string.IsNullOrWhiteSpace(NodeData.RewardAction.Key)
            ? "REWARD / TURN IN"
            : $"REWARD: {NodeData.RewardAction.Key}";

        /// <summary>Quest 완료 처리 전에 실행할 선택적인 타입 기반 보상 Action을 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var root = new VisualElement();
            root.Add(new HelpBox(
                "Quest 상태를 CanComplete로 바꾼 뒤 IQuestController.TurnInQuest를 호출합니다. " +
                "선택한 보상 Action은 아이템·재화·업적·연출 처리를 위해 그보다 먼저 실행됩니다.",
                HelpBoxMessageType.Info));
            root.Add(MethodCallEditor.Create(
                editHandler,
                "Optional Reward Action",
                NodeData.RewardAction,
                QuestMethodCatalog.GetMethods(MethodKind.Action),
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

        /// <summary>실패 종료 노드에 저장할 실패 이유 필드를 만듭니다.</summary>
        public override VisualElement CreateInspector(NodeInspectorEditHandler editHandler)
        {
            var reason = new TextField("Failure Reason")
            {
                value = NodeData.FailReason ?? string.Empty,
                multiline = true,
                isDelayed = true
            };
            reason.RegisterValueChangedCallback(change => editHandler.ApplyDataEdit(
                "Change failure reason",
                () => NodeData.FailReason = change.newValue));
            return reason;
        }
    }
}
