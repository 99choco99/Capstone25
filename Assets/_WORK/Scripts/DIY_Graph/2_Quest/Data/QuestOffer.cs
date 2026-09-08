namespace UniversalGraph
{
    /// <summary>Quest 그래프가 UI와 게임 코드에 제공하는 수락 후보 하나입니다.</summary>
    public sealed class QuestOffer
    {
        internal QuestOffer(
            QuestContainer definition,
			DialogueEntryPoint dialogueEntryPoint,
            int priority,
            bool isAvailable,
            string blockReason,
            string sourceEntryGuid,
            string sourceNodeGuid)
        {
            Definition = definition;
			DialogueEntryPoint = dialogueEntryPoint;
            Priority = priority;
            IsAvailable = isAvailable;
            BlockReason = blockReason ?? string.Empty;
            SourceEntryGuid = sourceEntryGuid;
            SourceNodeGuid = sourceNodeGuid;
        }

        /// <summary>이 후보를 만든 Quest 정의 에셋입니다.</summary>
        public QuestContainer Definition { get; }

        /// <summary>Quest 정의의 고정 ID입니다.</summary>
        public int QuestId => Definition.QuestId;

        /// <summary>Quest 목록에 표시할 이름입니다.</summary>
        public string Name => Definition.questName;

        /// <summary>Quest 목록에 표시할 설명입니다.</summary>
        public string Description => Definition.description;

        /// <summary>Quest 선택 뒤 재생할 선택적인 Dialogue 참조입니다.</summary>
		public DialogueEntryPoint DialogueEntryPoint { get; }

        /// <summary>여러 후보를 정렬하거나 자동 선택할 때 사용할 값입니다.</summary>
        public int Priority { get; }

        /// <summary>현재 이 Quest를 수락할 수 있는지 나타냅니다.</summary>
        public bool IsAvailable { get; }

        /// <summary>수락할 수 없을 때 UI에 표시할 기획자 작성 이유입니다.</summary>
        public string BlockReason { get; }

        /// <summary>후보를 만든 상호작용 시작점입니다. 수락 직전 조건을 다시 검사할 때 사용합니다.</summary>
        internal string SourceEntryGuid { get; }

        /// <summary>후보를 만든 Offer 노드입니다. 수락 직전 같은 경로인지 확인할 때 사용합니다.</summary>
        internal string SourceNodeGuid { get; }
    }
}
