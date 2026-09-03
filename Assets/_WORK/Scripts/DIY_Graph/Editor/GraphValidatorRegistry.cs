using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UniversalGraph.Editor
{
    /// <summary>도메인 검증기를 찾아 공통 구조 검사 결과와 합칩니다.</summary>
    public static class GraphValidatorRegistry
    {
        private static readonly List<IGraphValidator> Validators = new();
        private static bool isInitialized;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Validators.Clear();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IGraphValidator>()
                         .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
                         .OrderBy(type => type.AssemblyQualifiedName, StringComparer.Ordinal))
            {
                try
                {
                    if (Activator.CreateInstance(type) is IGraphValidator validator
                        && validator.ContainerType != null
                        && typeof(GraphContainer).IsAssignableFrom(validator.ContainerType))
                    {
                        Validators.Add(validator);
                    }
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogError(
                        $"[Flow Graph] Validator '{type.FullName}'를 초기화하지 못했습니다.\n{exception}");
                }
            }

            isInitialized = true;
        }

        private static void EnsureInitialized()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>그래프 에셋을 수정하지 않고 공통 규칙과 도메인 규칙을 실행합니다.</summary>
        public static IReadOnlyList<GraphValidationIssue> Validate(GraphContainer container)
        {
            if (container == null)
            {
                return CreateNullGraphIssue();
            }

            EnsureInitialized();
            var issues = new List<GraphValidationIssue>();
            GraphStructureValidator.Validate(container, issues);
            var index = new GraphValidationIndex(container);

            Type containerType = container.GetType();
            foreach (IGraphValidator validator in Validators
                         .Where(item => item.ContainerType.IsAssignableFrom(containerType)))
            {
                try
                {
                    validator.Validate(index, issues);
                }
                catch (Exception exception)
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        "VALIDATOR_EXCEPTION",
                        $"Validator '{validator.GetType().Name}' 실행에 실패했습니다: {exception.Message}"));
                }
            }

            return issues
                .OrderByDescending(issue => issue.Severity)
                .ThenBy(issue => issue.NodeGuid ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(issue => issue.Code, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// 노드 타입별 규칙은 제외하고, 모든 그래프에 공통인 직렬화 구조만 검사합니다.
        /// Serializer처럼 손상된 데이터를 읽기 전에 확인해야 하는 에디터 코드에서 사용합니다.
        /// </summary>
        internal static IReadOnlyList<GraphValidationIssue> ValidateStructure(GraphContainer container)
        {
            if (container == null)
            {
                return CreateNullGraphIssue();
            }

            var issues = new List<GraphValidationIssue>();
            GraphStructureValidator.Validate(container, issues);
            return issues;
        }

        /// <summary>선택한 노드 집합 안에서 단방향 순환에 포함된 노드를 찾습니다.</summary>
        public static HashSet<string> FindCycleNodes(
            GraphValidationIndex index,
            Func<NodeBaseData, bool> includeNode)
        {
            var visited = new HashSet<string>();
            var active = new HashSet<string>();
            var stack = new List<string>();
            var cycleNodes = new HashSet<string>();

            foreach (NodeBaseData node in index.Nodes.Where(node => node != null && includeNode(node)))
            {
                Visit(node.Guid);
            }

            return cycleNodes;

            void Visit(string guid)
            {
                if (active.Contains(guid))
                {
                    int cycleStart = stack.FindIndex(item => item == guid);
                    if (cycleStart >= 0)
                    {
                        cycleNodes.UnionWith(stack.Skip(cycleStart));
                    }
                    return;
                }

                if (!visited.Add(guid))
                {
                    return;
                }

                active.Add(guid);
                stack.Add(guid);
                foreach (NodeLinkData link in index.GetOutgoing(guid))
                {
                    if (index.TryGetNode(link.TargetNodeGuid, out NodeBaseData target)
                        && includeNode(target))
                    {
                        Visit(target.Guid);
                    }
                }

                active.Remove(guid);
                stack.RemoveAt(stack.Count - 1);
            }
        }

        private static IReadOnlyList<GraphValidationIssue> CreateNullGraphIssue()
        {
            return new[]
            {
                new GraphValidationIssue(
                    GraphValidationSeverity.Error,
                    "GRAPH_NULL",
                    "불러온 그래프 에셋이 없습니다.")
            };
        }

    }
}
