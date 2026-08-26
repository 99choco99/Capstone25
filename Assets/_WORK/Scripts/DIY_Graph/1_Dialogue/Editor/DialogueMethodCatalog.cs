using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Scripting;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>
    /// Dialogue 노드에서 선택할 수 있는 메서드의 에디터 전용 목록을 만듭니다.
    /// 런타임 호출은 <see cref="DialogueEventRegistry"/>가 담당합니다.
    /// </summary>
    [Preserve]
    internal static class DialogueMethodCatalog
    {
        private static readonly List<DialogueMethodDescriptor> actions = new List<DialogueMethodDescriptor>();
        private static readonly List<DialogueMethodDescriptor> conditions = new List<DialogueMethodDescriptor>();
        private static readonly Dictionary<string, DialogueMethodDescriptor> actionByKey = new Dictionary<string, DialogueMethodDescriptor>();
        private static readonly Dictionary<string, DialogueMethodDescriptor> conditionByKey = new Dictionary<string, DialogueMethodDescriptor>();

        static DialogueMethodCatalog()
        {
            BuildRegistry();
        }

        /// <summary>바인딩 종류에 사용할 수 있는 메서드를 반환합니다.</summary>
        public static IReadOnlyList<DialogueMethodDescriptor> GetMethods(MethodKind kind)
        {
            return kind == MethodKind.Action ? actions : conditions;
        }

        /// <summary>종류와 키로 메서드를 찾습니다.</summary>
        public static bool TryGetMethod(MethodKind kind, string key, out DialogueMethodDescriptor descriptor)
        {
            descriptor = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            Dictionary<string, DialogueMethodDescriptor> methodsByKey = kind == MethodKind.Action
                ? actionByKey
                : conditionByKey;
            return methodsByKey.TryGetValue(key, out descriptor);
        }

        /// <summary>
        /// 플레이어 어셈블리만 검사하여 에디터 보조 메서드가 작성 메뉴에 나타나지 않게 합니다.
        /// 중복 키는 어느 메서드인지 확정할 수 없으므로 의도적으로 목록에서 제외합니다.
        /// </summary>
        private static void BuildRegistry()
        {
            actions.Clear();
            conditions.Clear();
            actionByKey.Clear();
            conditionByKey.Clear();

            var playerAssemblyNames = new HashSet<string>();
            foreach (UnityEditor.Compilation.Assembly assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                playerAssemblyNames.Add(assembly.name);
            }

            var actionCandidates = new Dictionary<string, List<DialogueMethodDescriptor>>();
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<DialogueActionAttribute>())
            {
                var attribute = method.GetCustomAttribute<DialogueActionAttribute>(inherit: false);
                if (attribute != null && IsPlayerMethod(method, playerAssemblyNames, "action"))
                {
                    AddCandidate(method, MethodKind.Action, attribute.Key, attribute.Target, actionCandidates);
                }
            }

            var conditionCandidates = new Dictionary<string, List<DialogueMethodDescriptor>>();
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<DialogueConditionAttribute>())
            {
                var attribute = method.GetCustomAttribute<DialogueConditionAttribute>(inherit: false);
                if (attribute != null && IsPlayerMethod(method, playerAssemblyNames, "condition"))
                {
                    AddCandidate(method, MethodKind.Condition, attribute.Key, attribute.Target, conditionCandidates);
                }
            }

            PublishUnique(actionCandidates, actions, actionByKey, "action");
            PublishUnique(conditionCandidates, conditions, conditionByKey, "condition");
        }

        private static bool IsPlayerMethod(MethodInfo method, HashSet<string> playerAssemblyNames, string kind)
        {
            string assemblyName = method.DeclaringType?.Assembly.GetName().Name;
            if (!string.IsNullOrEmpty(assemblyName) && playerAssemblyNames.Contains(assemblyName))
            {
                return true;
            }

            Debug.LogWarning($"[Dialogue] Editor 전용 {kind} 메서드는 무시합니다: {method.DeclaringType?.FullName}.{method.Name}");
            return false;
        }

        private static void AddCandidate(
            MethodInfo method,
            MethodKind kind,
            string key,
            DialogueTarget target,
            Dictionary<string, List<DialogueMethodDescriptor>> candidatesByKey)
        {
            if (!DialogueMethodDescriptorFactory.TryCreate(method, kind, key, target, out DialogueMethodDescriptor descriptor, out string error))
            {
                Debug.LogError($"[Dialogue] {kind} '{method.DeclaringType?.FullName}.{method.Name}'을 등록하지 못했습니다: {error}");
                return;
            }

            if (!candidatesByKey.TryGetValue(descriptor.Key, out List<DialogueMethodDescriptor> candidates))
            {
                candidates = new List<DialogueMethodDescriptor>();
                candidatesByKey.Add(descriptor.Key, candidates);
            }

            candidates.Add(descriptor);
        }

        private static void PublishUnique(
            Dictionary<string, List<DialogueMethodDescriptor>> candidatesByKey,
            List<DialogueMethodDescriptor> published,
            Dictionary<string, DialogueMethodDescriptor> publishedByKey,
            string kind)
        {
            foreach (KeyValuePair<string, List<DialogueMethodDescriptor>> pair in candidatesByKey)
            {
                if (pair.Value.Count != 1)
                {
                    Debug.LogError($"[Dialogue] 중복된 {kind} 키 '{pair.Key}'는 그래프 메뉴에서 제외합니다.");
                    continue;
                }

                DialogueMethodDescriptor descriptor = pair.Value[0];
                published.Add(descriptor);
                publishedByKey.Add(descriptor.Key, descriptor);
            }

            published.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
        }
    }
}
