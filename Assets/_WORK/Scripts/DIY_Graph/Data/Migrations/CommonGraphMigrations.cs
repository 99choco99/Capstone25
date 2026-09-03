using System.Collections.Generic;

namespace UniversalGraph
{
    /// <summary>모든 그래프 컨테이너에 공통으로 적용되는 스키마 변경을 정의합니다.</summary>
    internal static class CommonGraphMigrations
    {
        /// <summary>공통 그래프 마이그레이션 단계를 등록합니다.</summary>
        internal static void Register(GraphAssetMigrationRegistry registry)
        {
            registry.Register<GraphContainer>(0, MigrateVersion0To1);
            registry.Register<GraphContainer>(1, MigrateVersion1To2);
        }

        /// <summary>기본 노드·연결 컬렉션과 구형 연결의 도착 포트를 복구합니다.</summary>
        private static string MigrateVersion0To1(GraphContainer container)
        {
            container.Nodes ??= new List<NodeBaseData>();
            container.NodeLinks ??= new List<NodeLinkData>();

            foreach (NodeLinkData link in container.NodeLinks)
            {
                if (link != null && string.IsNullOrWhiteSpace(link.TargetPortName))
                {
                    link.TargetPortName = "Input";
                }
            }

            return null;
        }

        /// <summary>두 번째 스키마에서 공통 데이터 변경은 없으며 도메인별 변환만 실행합니다.</summary>
        private static string MigrateVersion1To2(GraphContainer container)
        {
            return null;
        }
    }
}
