using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>
    /// Dialogue 노드에서 선택할 수 있는 메서드의 드롭다운을 만들기
    /// </summary>
    internal static class DialogueMethodCatalog
    {
        //메서드들 보관
        private static readonly List<DialogueMethodDescriptor> actions = new();
        private static readonly List<DialogueMethodDescriptor> conditions = new();
        private static readonly Dictionary<string, DialogueMethodDescriptor> actionByKey = new();
        private static readonly Dictionary<string, DialogueMethodDescriptor> conditionByKey = new();

        static DialogueMethodCatalog()
        {
            BuildCatalog();
        }

        /// <summary>바인딩 종류에 사용할 수 있는 메서드를 반환</summary>
        public static IReadOnlyList<DialogueMethodDescriptor> GetMethodList(MethodKind kind)
        {
            return kind == MethodKind.Action ? actions : conditions;
        }

        /// <summary>kind랑 key로 메서드 가져오기</summary>
        public static bool GetMethod(MethodKind kind, string key, out DialogueMethodDescriptor descriptor)
        {
            descriptor = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            Dictionary<string, DialogueMethodDescriptor> methodsByKey = kind == MethodKind.Action ? actionByKey : conditionByKey;
            return methodsByKey.TryGetValue(key, out descriptor);
        }

        /// <summary>
        /// 플레이어 어셈블리만 검사해서 드롭다운에 띄우기위해서 Catalog를 만듦
        /// </summary>
        private static void BuildCatalog()
        {
            actions.Clear();
            conditions.Clear();
            actionByKey.Clear();
            conditionByKey.Clear();

            HashSet<string> playerAssemblyNames = new();
            foreach (UnityEditor.Compilation.Assembly assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                playerAssemblyNames.Add(assembly.name);
            }

            //action 함수들
            Dictionary<string, List<DialogueMethodDescriptor>> actionCandidates = new ();
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<DialogueActionAttribute>())
            {
                var attribute = method.GetCustomAttribute<DialogueActionAttribute>(inherit: false);
                if (attribute != null && IsPlayerMethod(method, playerAssemblyNames, "action"))
                {
                    AddCandidate(method, MethodKind.Action, attribute.Key, attribute.Owner, actionCandidates);
                }
            }

            //condition 함수들
            Dictionary<string, List<DialogueMethodDescriptor>> conditionCandidates = new ();
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<DialogueConditionAttribute>())
            {
                var attribute = method.GetCustomAttribute<DialogueConditionAttribute>(inherit: false);
                if (attribute != null && IsPlayerMethod(method, playerAssemblyNames, "condition"))
                {
                    AddCandidate(method, MethodKind.Condition, attribute.Key, attribute.Owner, conditionCandidates);
                }
            }

            FinalizeCandidates(actionCandidates, actions, actionByKey, "action");
            FinalizeCandidates(conditionCandidates, conditions, conditionByKey, "condition");
        }

        /// <summary>
        /// 플레이어 어셈블리인지 ?
        /// </summary>
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

        /// <summary>
        /// 메서드 후보를 목록에 추가
        /// </summary>
        private static void AddCandidate(MethodInfo method, MethodKind kind, string key, DialogueMethodOwner owner, Dictionary<string, List<DialogueMethodDescriptor>> candidatesByKey)
        {
            if (!DialogueMethodDescriptorFactory.TryCreateFromReflection(method, kind, key, owner, out DialogueMethodDescriptor descriptor, out string error))
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

        /// <summary>
        /// 후보 메서드들을 검증 후 확정
        /// </summary>
        private static void FinalizeCandidates(
            Dictionary<string, List<DialogueMethodDescriptor>> candidatesByKey,
            List<DialogueMethodDescriptor> list,
            Dictionary<string, DialogueMethodDescriptor> listByKey,
            string kind)
        {
            foreach (KeyValuePair<string, List<DialogueMethodDescriptor>> pair in candidatesByKey)
            {
                if (pair.Value.Count != 1)
                {
                    Debug.LogError($"[Dialogue] 중복된 {kind} 키 '{pair.Key}'는 그래프 메뉴에서 제외합니다.");
                    continue;
                }
                //유일한것만 담기
                DialogueMethodDescriptor descriptor = pair.Value[0];
                list.Add(descriptor);
                listByKey.Add(descriptor.Key, descriptor);
            }

            list.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
        }
    }
}
