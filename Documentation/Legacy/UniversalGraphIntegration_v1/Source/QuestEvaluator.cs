using System.Collections.Generic;
// UniversalGraph v1 게임 연결 코드 보관본
using UniversalGraph;

/// <summary>Current-game adapter that converts Player/NPC objects into portable quest dialogue routing inputs.</summary>
public static class QuestEvaluator
{
    /// <summary>Returns dialogue candidates for the NPC's numeric ID and display-name alias.</summary>
    public static List<DialogueRequest> Evaluate(
        IEnumerable<QuestContainer> questGraphs,
        Player player,
        NPC npc)
    {
        if (player?.Quest == null || npc == null)
        {
            return new List<DialogueRequest>();
        }

        return QuestDialogueRouter.Evaluate(
            questGraphs,
            player.Quest,
            new[] { npc.id.ToString(), npc.NPC_Name });
    }
}
