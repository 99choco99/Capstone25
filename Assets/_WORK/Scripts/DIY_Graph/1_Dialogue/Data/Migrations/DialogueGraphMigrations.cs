using System;
using System.Collections.Generic;

namespace UniversalGraph
{
    /// <summary>DialogueContainer에만 적용되는 스키마 변경을 정의합니다.</summary>
    internal static class DialogueGraphMigrations
    {
        /// <summary>Dialogue 전용 마이그레이션 단계를 등록합니다.</summary>
        internal static void Register(GraphAssetMigrationRegistry registry)
        {
            registry.Register<DialogueContainer>(0, MigrateVersion0To1);
        }

        /// <summary>구형 Dialogue 노드의 인수와 선택지 포트를 복구합니다.</summary>
        private static string MigrateVersion0To1(DialogueContainer container)
        {
            foreach (NodeBaseData node in container.Nodes)
            {
                switch (node)
                {
                    case DialogueLineNodeData lineData:
                        lineData.EnterAction ??= new MethodBindingData();
                        break;

                    case DialogueChoiceNodeData choiceNode:
                        choiceNode.Choices ??= new List<DialogueChoiceData>();
                        foreach (DialogueChoiceData choice in choiceNode.Choices)
                        {
                            if (choice == null)
                            {
                                continue;
                            }

                            choice.SelectionAction ??= new MethodBindingData();
                            choice.VisibilityCondition ??= new MethodBindingData();
                            if (string.IsNullOrWhiteSpace(choice.PortName))
                            {
                                choice.PortName = Guid.NewGuid().ToString();
                            }
                        }
                        break;

                    case DialogueActionNodeData action:
                        action.Action ??= new MethodBindingData();
                        break;

                    case DialogueConditionNodeData condition:
                        condition.Condition ??= new MethodBindingData();
                        break;
                }
            }

            return null;
        }
    }
}
