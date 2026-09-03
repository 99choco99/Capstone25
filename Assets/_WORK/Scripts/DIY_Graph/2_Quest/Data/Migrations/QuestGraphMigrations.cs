namespace UniversalGraph
{
    /// <summary>QuestContainer에만 적용되는 스키마 변경을 정의합니다.</summary>
    internal static class QuestGraphMigrations
    {
        /// <summary>Quest 전용 마이그레이션 단계를 등록합니다.</summary>
        internal static void Register(GraphAssetMigrationRegistry registry)
        {
            registry.Register<QuestContainer>(0, MigrateVersion0To1);
            registry.Register<QuestContainer>(1, MigrateVersion1To2);
        }

        /// <summary>구형 Quest Action, Condition과 Reward 노드의 저장 인수를 복구합니다.</summary>
        private static string MigrateVersion0To1(QuestContainer container)
        {
            foreach (NodeBaseData node in container.Nodes)
            {
                switch (node)
                {
                    case QuestActionNodeData action:
                        action.Action ??= new MethodCallData();
                        break;

                    case QuestConditionNodeData condition:
                        condition.Condition ??= new MethodCallData();
                        break;

                    case QuestRewardNodeData reward:
                        reward.RewardAction ??= new MethodCallData();
                        break;
                }
            }

            return null;
        }

        /// <summary>Locked와 Ready로 나뉘어 있던 시작 전 상태를 NotStarted로 통합합니다.</summary>
        private static string MigrateVersion1To2(QuestContainer container)
        {
            foreach (NodeBaseData node in container.Nodes)
            {
                if (node is QuestStateConditionNodeData condition && (int)condition.TargetState == 1)
                {
                    condition.TargetState = QuestState.NotStarted;
                }
                else if (node is QuestStateChangeNodeData stateChange && (int)stateChange.NewState == 1)
                {
                    stateChange.NewState = QuestState.NotStarted;
                }
            }

            return null;
        }
    }
}
