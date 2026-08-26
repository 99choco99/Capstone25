using System;
using System.Collections.Generic;

namespace UniversalGraph
{
    /// <summary>Quest 저장 데이터를 순서대로 마이그레이션한 결과입니다.</summary>
    public readonly struct QuestSaveMigrationResult
    {
        internal QuestSaveMigrationResult(int fromVersion, int toVersion, bool changed)
        {
            FromVersion = fromVersion;
            ToVersion = toVersion;
            Changed = changed;
        }

        public int FromVersion { get; }
        public int ToVersion { get; }
        public bool Changed { get; }
    }

    /// <summary>
    /// 구형 QuestSaveData를 배포된 스키마 순서대로 한 단계씩 업그레이드합니다. 각 단계는 여러 번 실행해도
    /// 결과가 같으며, 여러 게임 버전을 건너뛴 저장 데이터도 처리할 수 있도록 계속 보존합니다.
    /// </summary>
    public static class QuestSaveMigrator
    {
        private static readonly IReadOnlyDictionary<int, Func<QuestSaveData, string>> Steps =
            new Dictionary<int, Func<QuestSaveData, string>>
            {
                [0] = MigrateVersion0To1,
                [1] = MigrateVersion1To2
            };

        /// <summary>구형 스냅샷을 현재 스키마로 변경하고, 알 수 없는 미래 버전 데이터는 거부합니다.</summary>
        public static bool TryMigrate(
            QuestSaveData saveData,
            out QuestSaveMigrationResult result,
            out string error)
        {
            result = default;
            if (saveData == null)
            {
                error = "Quest 저장 데이터가 null입니다.";
                return false;
            }

            int fromVersion = saveData.schemaVersion;
            if (fromVersion < 0)
            {
                error = $"Quest 저장 데이터의 스키마 버전 {fromVersion}이 올바르지 않습니다.";
                return false;
            }

            if (fromVersion > QuestSaveData.CurrentSchemaVersion)
            {
                error = $"Quest 저장 데이터의 스키마 버전 {fromVersion}이 지원 버전 " +
                        $"{QuestSaveData.CurrentSchemaVersion}보다 높습니다.";
                return false;
            }

            try
            {
                while (saveData.schemaVersion < QuestSaveData.CurrentSchemaVersion)
                {
                    int stepVersion = saveData.schemaVersion;
                    if (!Steps.TryGetValue(stepVersion, out Func<QuestSaveData, string> migrate))
                    {
                        error = $"Quest 저장 마이그레이션 {stepVersion} -> {stepVersion + 1} " +
                                "단계가 없습니다.";
                        return false;
                    }

                    string stepError = migrate(saveData);
                    if (!string.IsNullOrWhiteSpace(stepError))
                    {
                        error = $"Quest 저장 마이그레이션 {stepVersion} -> {stepVersion + 1}에 " +
                                $"실패했습니다: {stepError}";
                        return false;
                    }

                    saveData.schemaVersion = stepVersion + 1;
                }
            }
            catch (Exception exception)
            {
                error = $"Quest 저장 마이그레이션 중 {exception.GetType().Name} 예외가 발생했습니다: {exception.Message}";
                return false;
            }

            result = new QuestSaveMigrationResult(
                fromVersion,
                saveData.schemaVersion,
                fromVersion != saveData.schemaVersion);
            error = null;
            return true;
        }

        private static string MigrateVersion0To1(QuestSaveData saveData)
        {
            NormalizeCollections(saveData);
            return null;
        }

        private static string MigrateVersion1To2(QuestSaveData saveData)
        {
            NormalizeCollections(saveData);
            foreach (QuestProgressSaveData progress in saveData.quests)
            {
                if (progress != null && progress.definitionSchemaVersion <= 0)
                {
                    progress.definitionSchemaVersion = GraphAssetMigrator.CurrentVersion;
                }
            }
            return null;
        }

        private static void NormalizeCollections(QuestSaveData saveData)
        {
            saveData.quests ??= new List<QuestProgressSaveData>();
            foreach (QuestProgressSaveData progress in saveData.quests)
            {
                if (progress == null)
                {
                    continue;
                }

                progress.activeNodeGuids ??= new List<string>();
                progress.nodeProgressCounts ??= new List<QuestNodeProgressSaveData>();
                progress.completedNodeGuids ??= new List<string>();
                progress.completedGateInputs ??= new List<string>();
            }
        }
    }
}
