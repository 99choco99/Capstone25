using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary><see cref="QuestProgress"/>의 Dictionary 항목 하나를 직렬화하기 위한 대체 데이터입니다.</summary>
    [Serializable]
    public sealed class QuestNodeProgressSaveData
    {
        public string nodeGuid;
        public int count;
    }

    /// <summary>특정 Serializer에 의존하지 않는 Quest 하나의 전체 런타임 상태 스냅샷입니다.</summary>
    [Serializable]
    public sealed class QuestProgressSaveData
    {
        public int questId;
        /// <summary>이 진행 기록을 저장할 때 사용한 그래프 정의의 스키마 버전입니다.</summary>
        public int definitionSchemaVersion;
        public QuestState state;
        public List<string> activeNodeGuids = new();
        public List<QuestNodeProgressSaveData> nodeProgressCounts = new();
        public List<string> completedNodeGuids = new();
        public List<string> completedGateInputs = new();

        /// <summary>변경 가능한 런타임 상태를 JsonUtility가 지원하는 List 기반 데이터로 복사합니다.</summary>
        public static QuestProgressSaveData Capture(QuestProgress progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress), "저장할 Quest 진행 기록이 필요합니다.");
            }

            QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
            if (registry == null
                || !registry.TryGetQuestIndex(
                    progress.questId,
                    out QuestContainer definition,
                    out QuestGraphIndex graphIndex))
            {
                throw new InvalidOperationException(
                    $"Quest {progress.questId} 진행 기록과 일치하는 등록 정의가 없거나 읽을 수 없습니다. " +
                    "저장 전에 QuestDefinitionRegistry.Initialize를 호출하세요.");
            }

            progress.EnsureCollections();
            var saveData = new QuestProgressSaveData
            {
                questId = progress.questId,
                definitionSchemaVersion = definition.SchemaVersion,
                state = progress.state,
                activeNodeGuids = new List<string>(progress.activeNodeGuids),
                nodeProgressCounts = progress.nodeProgressCounts
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new QuestNodeProgressSaveData
                    {
                        nodeGuid = pair.Key,
                        count = pair.Value
                    })
                    .ToList(),
                completedNodeGuids = new List<string>(progress.completedNodeGuids),
                completedGateInputs = new List<string>(progress.completedGateInputs)
            };

            if (!QuestSaveData.TryValidateAgainstDefinition(
                    saveData,
                    progress,
                    definition,
                    graphIndex,
                    out string error))
            {
                throw new InvalidOperationException(error);
            }

            return saveData;
        }

        /// <summary>스냅샷을 검증하고 새로운 런타임 진행 기록으로 복원합니다.</summary>
        public bool TryRestore(out QuestProgress progress, out string error)
        {
            progress = null;
            if (questId <= 0)
            {
                error = $"Quest 저장 데이터에 올바르지 않은 Quest ID {questId}가 있습니다.";
                return false;
            }

            if (definitionSchemaVersion <= 0
                || definitionSchemaVersion > GraphAssetMigrator.CurrentVersion)
            {
                error = $"Quest {questId}가 지원하지 않는 그래프 스키마 " +
                        $"{definitionSchemaVersion}을 참조합니다.";
                return false;
            }

            if (!Enum.IsDefined(typeof(QuestState), state))
            {
                error = $"Quest {questId}에 알 수 없는 상태 값 {(int)state}이 있습니다.";
                return false;
            }

            if (!TryValidateGuidList(activeNodeGuids, "활성 노드", out error)
                || !TryValidateGuidList(completedNodeGuids, "완료 노드", out error)
                || !TryValidateGuidList(completedGateInputs, "완료 Gate 입력", out error))
            {
                error = $"Quest {questId}: {error}";
                return false;
            }

            var counters = new Dictionary<string, int>();
            foreach (QuestNodeProgressSaveData entry in nodeProgressCounts
                         ?? Enumerable.Empty<QuestNodeProgressSaveData>())
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.nodeGuid))
                {
                    error = $"Quest {questId}에 빈 노드 진행 키가 있습니다.";
                    return false;
                }

                if (entry.count < 0)
                {
                    error = $"Quest {questId}의 노드 '{entry.nodeGuid}' 진행량이 음수입니다.";
                    return false;
                }

                if (!counters.TryAdd(entry.nodeGuid, entry.count))
                {
                    error = $"Quest {questId}에 중복된 노드 진행 키 '{entry.nodeGuid}'가 있습니다.";
                    return false;
                }
            }

            progress = new QuestProgress
            {
                questId = questId,
                state = state,
                activeNodeGuids = new List<string>(activeNodeGuids ?? Enumerable.Empty<string>()),
                nodeProgressCounts = counters,
                completedNodeGuids = new List<string>(completedNodeGuids ?? Enumerable.Empty<string>()),
                completedGateInputs = new List<string>(completedGateInputs ?? Enumerable.Empty<string>())
            };
            error = null;
            return true;
        }

        private static bool TryValidateGuidList(
            IEnumerable<string> values,
            string label,
            out string error)
        {
            var unique = new HashSet<string>();
            foreach (string value in values ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    error = $"빈 {label} 키가 있습니다.";
                    return false;
                }

                if (!unique.Add(value))
                {
                    error = $"중복된 {label} 키 '{value}'가 있습니다.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }

    /// <summary>
    /// 버전이 지정된 List 기반 Quest 스냅샷입니다. 게임의 저장 시스템은 런타임 Dictionary 구조를 몰라도
    /// 이 객체를 직접 저장하거나 제공되는 JsonUtility 보조 함수를 사용할 수 있습니다.
    /// </summary>
    [Serializable]
    public sealed class QuestSaveData
    {
        public const int CurrentSchemaVersion = 3;

        public int schemaVersion = CurrentSchemaVersion;
        public List<QuestProgressSaveData> quests = new();

        /// <summary>모든 진행 기록을 Quest ID 순서로 저장합니다.</summary>
        public static QuestSaveData Capture(IQuestController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller), "Quest 저장 데이터를 만들 Controller가 필요합니다.");
            }

            if (controller.QuestProgress == null)
            {
                throw new InvalidOperationException("IQuestController.QuestProgress가 null을 반환했습니다.");
            }

            return new QuestSaveData
            {
                schemaVersion = CurrentSchemaVersion,
                quests = controller.QuestProgress.Values
                    .Where(progress => progress != null)
                    .OrderBy(progress => progress.questId)
                    .Select(QuestProgressSaveData.Capture)
                    .ToList()
            };
        }

        /// <summary>
        /// Controller를 변경하기 전에 모든 기록을 검증하고, 성공하면 한 번에 교체하거나 병합합니다.
        /// </summary>
        public bool TryApplyTo(IQuestController controller, bool replaceExisting, out string error)
        {
            if (controller == null)
            {
                error = "대상 Quest Controller가 필요합니다.";
                return false;
            }

            if (controller.QuestProgress == null)
            {
                error = "IQuestController.QuestProgress가 null을 반환했습니다.";
                return false;
            }

            if (!QuestSaveMigrator.TryMigrate(this, out _, out error))
            {
                return false;
            }

            QuestDefinitionRegistry registry = QuestDefinitionRegistry.Instance;
            if (registry == null)
            {
                error = "Quest 저장 데이터를 적용하기 전에 QuestDefinitionRegistry.Initialize를 호출해야 합니다.";
                return false;
            }

            var restored = new Dictionary<int, QuestProgress>();
            foreach (QuestProgressSaveData saved in quests ?? Enumerable.Empty<QuestProgressSaveData>())
            {
                if (saved == null)
                {
                    error = "Quest 저장 데이터에 null 진행 기록이 있습니다.";
                    return false;
                }

                if (!saved.TryRestore(out QuestProgress progress, out error))
                {
                    return false;
                }

                if (!registry.TryGetQuestIndex(
                        progress.questId,
                        out QuestContainer definition,
                        out QuestGraphIndex graphIndex))
                {
                    error = $"Quest 저장 데이터가 등록되지 않았거나 읽을 수 없는 Quest ID {progress.questId}를 참조합니다.";
                    return false;
                }

                if (!TryValidateAgainstDefinition(saved, progress, definition, graphIndex, out error))
                {
                    return false;
                }

                if (!restored.TryAdd(progress.questId, progress))
                {
                    error = $"Quest 저장 데이터에 중복된 Quest ID {progress.questId}가 있습니다.";
                    return false;
                }
            }

            if (replaceExisting)
            {
                controller.QuestProgress.Clear();
            }

            foreach (KeyValuePair<int, QuestProgress> pair in restored)
            {
                controller.QuestProgress[pair.Key] = pair.Value;
            }

            error = null;
            return true;
        }

        internal static bool TryValidateAgainstDefinition(
            QuestProgressSaveData saved,
            QuestProgress progress,
            QuestContainer definition,
            QuestGraphIndex graphIndex,
            out string error)
        {
            if (saved.definitionSchemaVersion != definition.SchemaVersion)
            {
                error = $"Quest {progress.questId} 저장 데이터의 정의 스키마는 " +
                        $"{saved.definitionSchemaVersion}이지만 등록된 정의는 {definition.SchemaVersion}입니다.";
                return false;
            }

            if (progress.state == QuestState.InProgress && progress.activeNodeGuids.Count == 0)
            {
                error = $"Quest {progress.questId}가 InProgress이지만 활성 목표나 대기 노드가 없습니다.";
                return false;
            }

            if (progress.state != QuestState.InProgress && progress.activeNodeGuids.Count > 0)
            {
                error = $"Quest {progress.questId}의 상태는 {progress.state}이지만 활성 노드가 남아 있습니다.";
                return false;
            }

            if (progress.state == QuestState.NotStarted
                && (progress.nodeProgressCounts.Count > 0
                    || progress.completedNodeGuids.Count > 0
                    || progress.completedGateInputs.Count > 0))
            {
                error = $"Quest {progress.questId}가 NotStarted이지만 이전 진행 기록이 남아 있습니다.";
                return false;
            }

            foreach (string activeGuid in progress.activeNodeGuids)
            {
                if (!graphIndex.Nodes.TryGetValue(activeGuid, out NodeBaseData nodeData))
                {
                    error = $"Quest {progress.questId}의 활성 노드 '{activeGuid}'가 현재 정의에 없습니다.";
                    return false;
                }

                if (nodeData is not QuestObjectiveNodeData
                    && nodeData is not QuestWaitForQuestNodeData)
                {
                    error = $"Quest {progress.questId}의 활성 노드 '{activeGuid}' 타입 " +
                            $"'{nodeData.GetType().Name}'은 대기 가능한 노드가 아닙니다.";
                    return false;
                }

                if (progress.completedNodeGuids.Contains(activeGuid))
                {
                    error = $"Quest {progress.questId}의 노드 '{activeGuid}'가 활성 목록과 완료 목록에 모두 있습니다.";
                    return false;
                }
            }

            foreach (string completedGuid in progress.completedNodeGuids)
            {
                if (!graphIndex.Nodes.TryGetValue(completedGuid, out NodeBaseData completedNode))
                {
                    error = $"Quest {progress.questId}의 완료 노드 '{completedGuid}'가 현재 정의에 없습니다.";
                    return false;
                }

                if (!CanStoreCompletedNode(completedNode))
                {
                    error = $"Quest {progress.questId}의 완료 노드 '{completedGuid}' 타입 " +
                            $"'{completedNode.GetType().Name}'은 완료 기록을 남기는 노드가 아닙니다.";
                    return false;
                }
            }

            foreach (KeyValuePair<string, int> counter in progress.nodeProgressCounts)
            {
                if (!graphIndex.Nodes.TryGetValue(counter.Key, out NodeBaseData nodeData)
                    || nodeData is not QuestObjectiveNodeData objective)
                {
                    error = $"Quest {progress.questId}의 진행량 키 '{counter.Key}'가 현재 Objective 노드를 참조하지 않습니다.";
                    return false;
                }

                int requiredAmount = Math.Max(1, objective.RequiredAmount);
                if (counter.Value > requiredAmount)
                {
                    error = $"Quest {progress.questId}의 Objective '{counter.Key}' 진행량 {counter.Value}가 " +
                            $"필요량 {requiredAmount}보다 큽니다.";
                    return false;
                }

                if (progress.activeNodeGuids.Contains(counter.Key) && counter.Value >= requiredAmount)
                {
                    error = $"Quest {progress.questId}의 활성 Objective '{counter.Key}' 진행량이 " +
                            $"이미 필요량 {requiredAmount}에 도달했습니다.";
                    return false;
                }

                if (!progress.activeNodeGuids.Contains(counter.Key)
                    && !progress.completedNodeGuids.Contains(counter.Key))
                {
                    error = $"Quest {progress.questId}의 Objective '{counter.Key}' 진행량이 " +
                            "활성 또는 완료 기록과 연결되어 있지 않습니다.";
                    return false;
                }
            }

            foreach (string gateInput in progress.completedGateInputs)
            {
                int separatorIndex = gateInput.IndexOf('|');
                if (separatorIndex <= 0 || separatorIndex >= gateInput.Length - 1)
                {
                    error = $"Quest {progress.questId}의 완료 Gate 입력 '{gateInput}' 형식이 올바르지 않습니다.";
                    return false;
                }

                string gateGuid = gateInput.Substring(0, separatorIndex);
                string sourceGuid = gateInput.Substring(separatorIndex + 1);
                if (!graphIndex.Nodes.TryGetValue(gateGuid, out NodeBaseData gateData)
                    || gateData is not QuestAndGateNodeData
                    || !graphIndex.Nodes.ContainsKey(sourceGuid)
                    || !graphIndex.OutgoingLinks.TryGetValue(sourceGuid, out List<NodeLinkData> sourceLinks)
                    || !sourceLinks.Any(link => link.TargetNodeGuid == gateGuid))
                {
                    error = $"Quest {progress.questId}의 완료 Gate 입력 '{gateInput}'이 현재 연결 구조와 일치하지 않습니다.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool CanStoreCompletedNode(NodeBaseData nodeData)
        {
            return nodeData is QuestObjectiveNodeData
                   || nodeData is QuestAndGateNodeData
                   || nodeData is QuestStateChangeNodeData
                   || nodeData is QuestActionNodeData
                   || nodeData is QuestFailNodeData
                   || nodeData is QuestRewardNodeData
                   || nodeData is QuestWaitForQuestNodeData;
        }

        /// <summary>Unity 내장 JSON 형식으로 이 스냅샷을 직렬화합니다.</summary>
        public string ToJson(bool prettyPrint = false)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        /// <summary>Controller를 변경하지 않고 JSON을 읽어 스키마를 검사합니다.</summary>
        public static bool TryFromJson(string json, out QuestSaveData saveData, out string error)
        {
            saveData = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Quest 저장 JSON이 비어 있습니다.";
                return false;
            }

            try
            {
                QuestSaveVersionProbe versionProbe = JsonUtility.FromJson<QuestSaveVersionProbe>(json);
                saveData = JsonUtility.FromJson<QuestSaveData>(json);
                if (saveData != null)
                {
                    // JsonUtility는 필드가 없어도 초기값을 유지할 수 있으므로 버전을 별도로 확인합니다.
                    saveData.schemaVersion = versionProbe?.schemaVersion ?? 0;
                }
            }
            catch (Exception exception)
            {
                error = $"Quest 저장 JSON을 파싱하지 못했습니다: {exception.Message}";
                return false;
            }

            if (saveData == null)
            {
                error = "Quest 저장 JSON에서 데이터를 생성하지 못했습니다.";
                return false;
            }

            if (!QuestSaveMigrator.TryMigrate(saveData, out _, out error))
            {
                saveData = null;
                return false;
            }

            saveData.quests ??= new List<QuestProgressSaveData>();
            error = null;
            return true;
        }

        [Serializable]
        private sealed class QuestSaveVersionProbe
        {
            public int schemaVersion;

            public QuestSaveVersionProbe()
            {
                schemaVersion = 0;
            }
        }
    }
}
