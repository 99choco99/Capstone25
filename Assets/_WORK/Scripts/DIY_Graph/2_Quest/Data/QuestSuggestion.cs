namespace UniversalGraph
{
    /// <summary>퀘스트 제안 정보</summary>
    public sealed class QuestSuggestion
    {
        internal QuestSuggestion(QuestContainer container, DialogueEntryPoint dialogueEntryPoint, int priority, bool isAvailable, string blockReason, string sourceQuestEntryGuid, string sourceNodeGuid)
        {
            Container = container;
			DialogueEntryPoint = dialogueEntryPoint;
            Priority = priority;
            IsAvailable = isAvailable;
            BlockReason = blockReason ?? string.Empty;
            SourceQuestEntryGuid = sourceQuestEntryGuid;
            SourceNodeGuid = sourceNodeGuid;
        }

        /// <summary>Quest 정의</summary>
        public QuestContainer Container { get; }

        /// <summary>Quest 정의의 고정 ID</summary>
        public int QuestId => Container.QuestId;

        /// <summary>Quest 목록에 표시할 이름</summary>
        public string Name => Container.questName;

        /// <summary>Quest 목록에 표시할 설명</summary>
        public string Description => Container.description;



        /// <summary>우선순위</summary>
        public int Priority { get; }

        /// <summary>현재 이 Quest를 표시할 수 있는지</summary>
        public bool IsAvailable { get; }

        /// <summary>표시할 수 없을 때 UI에 표시할 이유</summary>
        public string BlockReason { get; }


        /// <summary>Quest 선택 시 재생할 선택적인 Dialogue 참조</summary>
        public DialogueEntryPoint DialogueEntryPoint { get; }

        /// <summary>선택 항목을 만든 상호작용 시작점입니다. 수락 직전 조건을 다시 검사할 때 사용합니다.</summary>
        internal string SourceQuestEntryGuid { get; }

        /// <summary>선택 항목을 만든 Quest Suggestion 노드입니다. 수락 직전 같은 경로인지 확인할 때 사용합니다.</summary>
        internal string SourceNodeGuid { get; }
    }
}
