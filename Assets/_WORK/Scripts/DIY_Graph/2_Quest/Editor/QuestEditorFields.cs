using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>실수하기 쉬운 숫자 직접 입력을 대신하는 재사용 가능한 Quest 참조 필드입니다.</summary>
    internal static class QuestEditorFields
    {
        /// <summary>프로젝트 에셋 기반 Quest 선택기를 만들며, 누락된 구형 ID는 복구할 수 있도록 유지합니다.</summary>
        public static PopupField<int> CreateQuestIdField(
            string label,
            int currentQuestId,
            string undoName,
            NodeInspectorEditHandler editHandler,
            Action<int> apply)
        {
            var ids = QuestAssetIndex.Quests
                .Where(quest => quest != null)
                .Select(quest => quest.questId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
            if (!ids.Contains(currentQuestId))
            {
                ids.Add(currentQuestId);
                ids.Sort();
            }
            if (ids.Count == 0)
            {
                ids.Add(0);
            }

            int selectedIndex = Math.Max(0, ids.IndexOf(currentQuestId));
            var field = new PopupField<int>(label, ids, selectedIndex, FormatQuest, FormatQuest);
            field.RegisterValueChangedCallback(change =>
                editHandler.ApplyDataEdit(undoName, () => apply(change.newValue)));
            return field;
        }

        private static string FormatQuest(int questId)
        {
            QuestContainer[] matches = QuestAssetIndex.Quests
                .Where(quest => quest != null && quest.questId == questId)
                .ToArray();
            return matches.Length switch
            {
                0 => $"<존재하지 않음> {questId}",
                1 => $"{questId} - {matches[0].questName}",
                _ => $"<Duplicate> {questId} ({matches.Length} assets)"
            };
        }
    }
}
