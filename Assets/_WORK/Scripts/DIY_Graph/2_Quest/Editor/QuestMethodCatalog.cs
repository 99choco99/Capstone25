using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniversalGraph.Quest.Editor
{
    /// <summary>그래프 작성에서 선택할 수 있는 유효한 Quest Attribute 메서드를 나열합니다.</summary>
    internal static class QuestMethodCatalog
    {
        private static readonly List<QuestMethodDescriptor> actions = new();
        private static readonly List<QuestMethodDescriptor> conditions = new();
        private static readonly Dictionary<string, QuestMethodDescriptor> actionByKey = new();
        private static readonly Dictionary<string, QuestMethodDescriptor> conditionByKey = new();

        static QuestMethodCatalog()
        {
            BuildRegistry();
        }

        /// <summary>특정 바인딩 종류의 유효하고 중복되지 않는 메서드를 반환합니다.</summary>
        public static IReadOnlyList<QuestMethodDescriptor> GetMethodList(MethodKind kind)
        {
            return kind == MethodKind.Action ? actions : conditions;
        }

        /// <summary>고정 키로 유효한 메서드 하나를 찾습니다.</summary>
        public static bool GetMethod(
            MethodKind kind,
            string key,
            out QuestMethodDescriptor descriptor)
        {
            descriptor = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return (kind == MethodKind.Action ? actionByKey : conditionByKey)
                .TryGetValue(key.Trim(), out descriptor);
        }

        /// <summary>플레이어 어셈블리를 검사하고 대상을 확정할 수 없는 중복 키를 제외합니다.</summary>
        private static void BuildRegistry()
        {
            actions.Clear();
            conditions.Clear();
            actionByKey.Clear();
            conditionByKey.Clear();

            var playerAssemblies = new HashSet<string>();
            foreach (UnityEditor.Compilation.Assembly assembly in
                     CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                playerAssemblies.Add(assembly.name);
            }

            var actionCandidates = new Dictionary<string, List<QuestMethodDescriptor>>();
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<QuestActionAttribute>())
            {
                QuestActionAttribute attribute = method.GetCustomAttribute<QuestActionAttribute>(false);
                if (attribute != null && IsPlayerMethod(method, playerAssemblies, "action"))
                {
                    AddCandidate(
                        method,
                        MethodKind.Action,
                        attribute.Key,
                        attribute.Target,
                        actionCandidates);
                }
            }

            var conditionCandidates = new Dictionary<string, List<QuestMethodDescriptor>>();
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<QuestConditionAttribute>())
            {
                QuestConditionAttribute attribute = method.GetCustomAttribute<QuestConditionAttribute>(false);
                if (attribute != null && IsPlayerMethod(method, playerAssemblies, "condition"))
                {
                    AddCandidate(
                        method,
                        MethodKind.Condition,
                        attribute.Key,
                        attribute.Target,
                        conditionCandidates);
                }
            }

            FinalizeCandidates(actionCandidates, actions, actionByKey, "action");
            FinalizeCandidates(conditionCandidates, conditions, conditionByKey, "condition");
        }

        private static bool IsPlayerMethod(
            MethodInfo method,
            ISet<string> playerAssemblies,
            string kind)
        {
            string assemblyName = method.DeclaringType?.Assembly.GetName().Name;
            if (!string.IsNullOrWhiteSpace(assemblyName) && playerAssemblies.Contains(assemblyName))
            {
                return true;
            }

            Debug.LogWarning(
                $"[Quest] Editor 전용 {kind} 메서드는 무시합니다: " +
                $"{method.DeclaringType?.FullName}.{method.Name}");
            return false;
        }

        private static void AddCandidate(
            MethodInfo method,
            MethodKind kind,
            string key,
            QuestMethodTarget target,
            IDictionary<string, List<QuestMethodDescriptor>> candidatesByKey)
        {
            if (!QuestMethodDescriptorFactory.TryCreateFromReflection(
                    method,
                    kind,
                    key,
                    target,
                    out QuestMethodDescriptor descriptor,
                    out string error))
            {
                Debug.LogError(
                    $"[Quest] {kind}을 등록하지 못했습니다: " +
                    $"'{method.DeclaringType?.FullName}.{method.Name}': {error}");
                return;
            }

            if (!candidatesByKey.TryGetValue(descriptor.Key, out List<QuestMethodDescriptor> candidates))
            {
                candidates = new List<QuestMethodDescriptor>();
                candidatesByKey.Add(descriptor.Key, candidates);
            }

            candidates.Add(descriptor);
        }

        private static void FinalizeCandidates(
            IReadOnlyDictionary<string, List<QuestMethodDescriptor>> candidatesByKey,
            ICollection<QuestMethodDescriptor> published,
            IDictionary<string, QuestMethodDescriptor> publishedByKey,
            string kind)
        {
            var sorted = new List<QuestMethodDescriptor>();
            foreach (KeyValuePair<string, List<QuestMethodDescriptor>> pair in candidatesByKey)
            {
                if (pair.Value.Count != 1)
                {
                    Debug.LogError(
                        $"[Quest] 중복된 {kind} 키 '{pair.Key}'는 그래프 메뉴에서 제외합니다.");
                    continue;
                }

                QuestMethodDescriptor descriptor = pair.Value[0];
                sorted.Add(descriptor);
                publishedByKey.Add(descriptor.Key, descriptor);
            }

            sorted.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
            foreach (QuestMethodDescriptor descriptor in sorted)
            {
                published.Add(descriptor);
            }
        }
    }
}
