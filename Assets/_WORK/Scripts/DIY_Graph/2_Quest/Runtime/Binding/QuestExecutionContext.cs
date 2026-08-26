using System;

namespace UniversalGraph
{
    /// <summary>노드에 직렬화하지 않고 Attribute 메서드에 주입하는 Quest 런타임 상태입니다.</summary>
    public sealed class QuestExecutionContext
    {
        public QuestExecutionContext(
            IQuestController controller,
            QuestContainer quest,
            QuestProgress progress,
            NodeBaseData node)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller), "Quest 실행 Controller가 필요합니다.");
            Quest = quest ?? throw new ArgumentNullException(nameof(quest), "실행 중인 Quest 정의가 필요합니다.");
            Progress = progress;
            Node = node ?? throw new ArgumentNullException(nameof(node), "실행 중인 Quest 노드가 필요합니다.");
        }

        public IQuestController Controller { get; }
        public QuestContainer Quest { get; }
        /// <summary>현재 진행 기록입니다. 등록 전에 평가하는 조회 전용 경로에서는 null일 수 있습니다.</summary>
        public QuestProgress Progress { get; }
        public NodeBaseData Node { get; }
    }
}
