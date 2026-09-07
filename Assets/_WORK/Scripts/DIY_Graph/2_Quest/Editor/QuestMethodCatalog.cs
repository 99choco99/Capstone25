using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;

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
            var playerAssemblies = new HashSet<string>();
            foreach (UnityEditor.Compilation.Assembly assembly in
                     CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                playerAssemblies.Add(assembly.name);
                foreach (string reference in assembly.compiledAssemblyReferences)
                {
                    playerAssemblies.Add(System.IO.Path.GetFileNameWithoutExtension(reference));
                }
            }

            var actionCandidates = new Dictionary<string, List<QuestMethodDescriptor>>();
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<QuestActionAttribute>())
            {
                QuestActionAttribute attribute = method.GetCustomAttribute<QuestActionAttribute>(false);
                if (attribute != null && IsPlayerMethod(method, playerAssemblies))
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
                if (attribute != null && IsPlayerMethod(method, playerAssemblies))
                {
                    AddCandidate(
                        method,
                        MethodKind.Condition,
                        attribute.Key,
                        attribute.Target,
                        conditionCandidates);
                }
            }

            FinalizeCandidates(actionCandidates, actions, actionByKey);
            FinalizeCandidates(conditionCandidates, conditions, conditionByKey);
        }

        private static bool IsPlayerMethod(
            MethodInfo method,
            ISet<string> playerAssemblies)
        {
            string assemblyName = method.DeclaringType?.Assembly.GetName().Name;
            return !string.IsNullOrWhiteSpace(assemblyName) && playerAssemblies.Contains(assemblyName);
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
                    out _))
            {
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
            List<QuestMethodDescriptor> list,
            IDictionary<string, QuestMethodDescriptor> listByKey)
        {
            foreach (KeyValuePair<string, List<QuestMethodDescriptor>> pair in candidatesByKey)
            {
                if (pair.Value.Count != 1)
                {
                    continue;
                }

                QuestMethodDescriptor descriptor = pair.Value[0];
                list.Add(descriptor);
                listByKey.Add(descriptor.Key, descriptor);
            }

            list.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
        }
    }
}
