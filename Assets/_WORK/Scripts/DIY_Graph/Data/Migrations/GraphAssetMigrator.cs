using System;

namespace UniversalGraph
{
    /// <summary>그래프 에셋 하나를 검사하거나 업그레이드한 결과입니다.</summary>
    public readonly struct GraphAssetMigrationResult
    {
        internal GraphAssetMigrationResult(int fromVersion, int toVersion, bool changed)
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
    /// 그래프 스키마를 정해진 순서로, 여러 번 실행해도 결과가 같도록 업그레이드합니다.
    /// 새 버전마다 단계를 추가하고 이미 배포한 단계는 구형 에셋 호환을 위해 수정하지 않습니다.
    /// </summary>
    public static class GraphAssetMigrator
    {
        public const int CurrentVersion = 2;

        private static readonly GraphAssetMigrationRegistry Registry = CreateRegistry();

        /// <summary>그래프 하나를 메모리에서 업그레이드하고, 실패하면 안전하게 읽을 수 없는 이유를 반환합니다.</summary>
        public static bool TryMigrate(
            GraphContainer container,
            out GraphAssetMigrationResult result,
            out string error)
        {
            result = default;
            if (container == null)
            {
                error = "그래프 에셋이 필요합니다.";
                return false;
            }

            int fromVersion = container.SchemaVersion;
            if (fromVersion < 0)
            {
                error = $"그래프 '{container.name}'의 스키마 버전 {fromVersion}이 올바르지 않습니다.";
                return false;
            }

            if (fromVersion > CurrentVersion)
            {
                error = $"그래프 '{container.name}'은 미래 스키마 버전 {fromVersion}을 사용합니다. " +
                        $"현재 패키지는 {CurrentVersion} 버전까지 지원합니다.";
                return false;
            }

            try
            {
                while (container.SchemaVersion < CurrentVersion)
                {
                    int stepVersion = container.SchemaVersion;
                    string stepError = Registry.Migrate(stepVersion, container);
                    if (!string.IsNullOrWhiteSpace(stepError))
                    {
                        error = $"그래프 마이그레이션 {stepVersion} -> {stepVersion + 1}에 실패했습니다: {stepError}";
                        return false;
                    }

                    container.SetSchemaVersion(stepVersion + 1);
                }
            }
            catch (Exception exception)
            {
                error = $"그래프 마이그레이션 중 {exception.GetType().Name} 예외가 발생했습니다: {exception.Message}";
                return false;
            }

            result = new GraphAssetMigrationResult(
                fromVersion,
                container.SchemaVersion,
                fromVersion != container.SchemaVersion);
            error = null;
            return true;
        }

        /// <summary>지원 범위보다 새 그래프이거나 필수 마이그레이션이 실패하면 예외를 발생시킵니다.</summary>
        public static void EnsureCurrent(GraphContainer container)
        {
            if (!TryMigrate(container, out _, out string error))
            {
                throw new InvalidOperationException(error);
            }
        }

        /// <summary>공통 및 각 도메인의 마이그레이션 단계를 실행 순서대로 등록합니다.</summary>
        private static GraphAssetMigrationRegistry CreateRegistry()
        {
            var registry = new GraphAssetMigrationRegistry();
            CommonGraphMigrations.Register(registry);
            DialogueGraphMigrations.Register(registry);
            QuestGraphMigrations.Register(registry);
            registry.EnsureComplete(CurrentVersion);
            return registry;
        }
    }
}
