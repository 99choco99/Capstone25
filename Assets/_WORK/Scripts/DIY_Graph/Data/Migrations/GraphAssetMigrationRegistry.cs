using System;
using System.Collections.Generic;

namespace UniversalGraph
{
    /// <summary>스키마 버전과 컨테이너 타입에 맞는 마이그레이션 단계를 보관합니다.</summary>
    internal sealed class GraphAssetMigrationRegistry
    {
        private readonly Dictionary<int, List<MigrationRegistration>> registrationsByVersion = new();

        /// <summary>특정 버전에서 해당 컨테이너 타입에만 적용할 단계를 등록합니다.</summary>
        internal void Register<TContainer>(int fromVersion, Func<TContainer, string> migrate)
            where TContainer : GraphContainer
        {
            if (fromVersion < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fromVersion), "마이그레이션 시작 버전은 0 이상이어야 합니다.");
            }

            if (migrate == null)
            {
                throw new ArgumentNullException(nameof(migrate), "마이그레이션 함수가 필요합니다.");
            }

            if (!registrationsByVersion.TryGetValue(fromVersion, out List<MigrationRegistration> registrations))
            {
                registrations = new List<MigrationRegistration>();
                registrationsByVersion.Add(fromVersion, registrations);
            }

            Type containerType = typeof(TContainer);
            if (registrations.Exists(registration => registration.ContainerType == containerType))
            {
                throw new InvalidOperationException(
                    $"{containerType.Name}에 대한 마이그레이션 {fromVersion} -> {fromVersion + 1}이 이미 등록되어 있습니다.");
            }

            registrations.Add(new MigrationRegistration<TContainer>(migrate));
        }

        /// <summary>등록 순서대로 공통 단계와 현재 컨테이너에 맞는 도메인 단계를 실행합니다.</summary>
        internal string Migrate(int fromVersion, GraphContainer container)
        {
            foreach (MigrationRegistration registration in registrationsByVersion[fromVersion])
            {
                if (!registration.AppliesTo(container))
                {
                    continue;
                }

                string error = registration.Migrate(container);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    return $"{registration.ContainerType.Name}: {error}";
                }
            }

            return null;
        }

        /// <summary>현재 버전까지 중간 단계가 빠짐없이 등록되었는지 시작 시 검증합니다.</summary>
        internal void EnsureComplete(int currentVersion)
        {
            for (int fromVersion = 0; fromVersion < currentVersion; fromVersion++)
            {
                if (!registrationsByVersion.TryGetValue(fromVersion, out List<MigrationRegistration> registrations)
                    || registrations.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"그래프 마이그레이션 {fromVersion} -> {fromVersion + 1}이 등록되어 있지 않습니다.");
                }
            }
        }

        private abstract class MigrationRegistration
        {
            protected MigrationRegistration(Type containerType)
            {
                ContainerType = containerType;
            }

            internal Type ContainerType { get; }
            internal abstract bool AppliesTo(GraphContainer container);
            internal abstract string Migrate(GraphContainer container);
        }

        private sealed class MigrationRegistration<TContainer> : MigrationRegistration
            where TContainer : GraphContainer
        {
            private readonly Func<TContainer, string> migrate;

            internal MigrationRegistration(Func<TContainer, string> migrate)
                : base(typeof(TContainer))
            {
                this.migrate = migrate;
            }

            internal override bool AppliesTo(GraphContainer container)
            {
                return container is TContainer;
            }

            internal override string Migrate(GraphContainer container)
            {
                return migrate((TContainer)container);
            }
        }
    }
}
