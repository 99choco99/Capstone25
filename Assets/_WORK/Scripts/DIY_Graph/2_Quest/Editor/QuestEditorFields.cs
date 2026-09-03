using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>실수하기 쉬운 숫자 직접 입력을 대신하는 재사용 가능한 Quest 참조 필드입니다.</summary>
    internal static class QuestEditorFields
    {
        /// <summary>Dialogue 그래프와 Entry를 함께 선택하는 공통 입력 영역을 만듭니다.</summary>
        public static VisualElement CreateDialogueEntryPointField(
            DialogueEntryPoint current,
            NodeInspectorEditHandler editHandler,
            Action<DialogueEntryPoint> apply)
        {
            var root = new VisualElement();
            DialogueEntryPoint entryPoint = current;

            var graphField = new ObjectField("Graph Asset")
            {
                objectType = typeof(DialogueContainer),
                allowSceneObjects = false,
                value = entryPoint.GraphAsset
            };
            var entryField = new PopupField<string>(
                "Entry ID",
                GetEntryChoices(entryPoint.GraphAsset, entryPoint.EntryId),
                0);
            string selectedEntryId = entryField.choices.FirstOrDefault(choice => choice == entryPoint.EntryId)
                                     ?? entryField.choices[0];
            entryField.SetValueWithoutNotify(selectedEntryId);
            entryField.SetEnabled(entryPoint.GraphAsset != null);

            var openGraphButton = new Button(() =>
            {
                if (entryPoint.GraphAsset != null)
                {
                    UniversalGraphWindow.OpenWindow(entryPoint.GraphAsset);
                }
            })
            {
                text = "Open Dialogue Graph"
            };
            openGraphButton.SetEnabled(entryPoint.GraphAsset != null);

            graphField.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit("Change dialogue graph", () =>
                {
                    entryPoint.GraphAsset = change.newValue as DialogueContainer;
                    List<string> entries = GetEntryChoices(
                        entryPoint.GraphAsset,
                        DialogueEntryNodeData.DefaultEntryId);
                    entryPoint.EntryId = entries[0];
                    apply(entryPoint);

                    entryField.choices = entries;
                    entryField.SetValueWithoutNotify(entries[0]);
                    entryField.SetEnabled(entryPoint.GraphAsset != null);
                    openGraphButton.SetEnabled(entryPoint.GraphAsset != null);
                });
            });
            root.Add(graphField);

            entryField.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit("Change dialogue entry", () =>
                {
                    entryPoint.EntryId = change.newValue;
                    apply(entryPoint);
                });
            });
            root.Add(entryField);
            root.Add(openGraphButton);
            return root;
        }

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
                .Select(quest => quest.QuestId)
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

        private static List<string> GetEntryChoices(DialogueContainer graph, string currentEntryId)
        {
            var entries = graph?.Nodes?
                .OfType<DialogueEntryNodeData>()
                .Select(entry => entry.EntryId)
                .Distinct()
                .OrderBy(entry => entry == DialogueEntryNodeData.DefaultEntryId ? 0 : 1)
                .ThenBy(entry => entry, StringComparer.Ordinal)
                .ToList()
                ?? new List<string>();

            if (entries.Count == 0)
            {
                entries.Add(currentEntryId);
            }
            else if (!entries.Contains(currentEntryId))
            {
                entries.Add(currentEntryId);
            }

            return entries;
        }

        private static string FormatQuest(int questId)
        {
            QuestContainer[] matches = QuestAssetIndex.Quests
                .Where(quest => quest != null && quest.QuestId == questId)
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
