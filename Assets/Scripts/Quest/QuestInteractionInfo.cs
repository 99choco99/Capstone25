using UnityEngine;

public enum QuestInteractionType
{
    None,
    Start,
    Talk,
    Complete
}

public class QuestInteractionInfo
{
    public string DialogueKey { get; private set; }
    public int QuestId { get; private set; }
    public int NpcId { get; private set; } // 어떤 NPC와의 상호작용인지 ID를 저장
    public QuestInteractionType Type { get; private set; }

    public QuestInteractionInfo(string key, int questId, int npcId, QuestInteractionType type)
    {
        DialogueKey = key;
        QuestId = questId;
        NpcId = npcId;
        Type = type;
    }
}