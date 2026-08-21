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
    /// Builds the editor-only list of methods that can be selected by dialogue nodes.
    /// Runtime invocation remains the responsibility of <see cref="DialogueEventRegistry"/>.
    /// </summary>
    [Preserve]
    internal static class DialogueMethodCatalog
    {
        private static readonly List<DialogueMethodDescriptor> actions = new List<DialogueMethodDescriptor>();
        private static readonly List<DialogueMethodDescriptor> conditions = new List<DialogueMethodDescriptor>();
        private static readonly Dictionary<string, DialogueMethodDescriptor> actionByKey = new Dictionary<string, DialogueMethodDescriptor>(StringComparer.Ordinal);
        private static readonly Dictionary<string, DialogueMethodDescriptor> conditionByKey = new Dictionary<string, DialogueMethodDescriptor>(StringComparer.Ordinal);

        /// <summary>Gets selectable action methods, ordered by key.</summary>
        public static IReadOnlyList<DialogueMethodDescriptor> Actions => actions;

        /// <summary>Gets selectable condition methods, ordered by key.</summary>
        public static IReadOnlyList<DialogueMethodDescriptor> Conditions => conditions;

        static DialogueMethodCatalog()
        {
            BuildRegistry();
        }

        /// <summary>Looks up an action by its stable dialogue key.</summary>
        public static bool TryGetAction(string key, out DialogueMethodDescriptor descriptor)
        {
            return TryGet(actionByKey, key, out descriptor);
        }

        /// <summary>Looks up a condition by its stable dialogue key.</summary>
        public static bool TryGetCondition(string key, out DialogueMethodDescriptor descriptor)
        {
            return TryGet(conditionByKey, key, out descriptor);
        }

        /// <summary>Returns the methods available for a binding kind.</summary>
        public static IReadOnlyList<DialogueMethodDescriptor> GetMethods(DialogueMethodKind kind)
        {
            return kind == DialogueMethodKind.Action ? Actions : Conditions;
        }

        /// <summary>Looks up a method by kind and key.</summary>
        public static bool TryGetMethod(DialogueMethodKind kind, string key, out DialogueMethodDescriptor descriptor)
        {
            return kind == DialogueMethodKind.Action
                ? TryGetAction(key, out descriptor)
                : TryGetCondition(key, out descriptor);
        }

        private static bool TryGet(Dictionary<string, DialogueMethodDescriptor> registry, string key, out DialogueMethodDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                descriptor = null;
                return false;
            }

            return registry.TryGetValue(key, out descriptor);
        }

        /// <summary>
        /// Scans player assemblies only, so editor helper methods never leak into the authoring menu.
        /// Duplicate keys are intentionally excluded: choosing either candidate would make assets ambiguous.
        /// </summary>
        private static void BuildRegistry()
        {
            actions.Clear();
            conditions.Clear();
            actionByKey.Clear();
            conditionByKey.Clear();

            var playerAssemblyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (UnityEditor.Compilation.Assembly assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                playerAssemblyNames.Add(assembly.name);
            }

            var actionCandidates = new Dictionary<string, List<DialogueMethodDescriptor>>(StringComparer.Ordinal);
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<DialogueActionAttribute>())
            {
                var attribute = method.GetCustomAttribute<DialogueActionAttribute>(inherit: false);
                if (attribute != null && IsPlayerMethod(method, playerAssemblyNames, "action"))
                {
                    AddCandidate(method, DialogueMethodKind.Action, attribute.Key, attribute.Target, actionCandidates);
                }
            }

            var conditionCandidates = new Dictionary<string, List<DialogueMethodDescriptor>>(StringComparer.Ordinal);
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<DialogueConditionAttribute>())
            {
                var attribute = method.GetCustomAttribute<DialogueConditionAttribute>(inherit: false);
                if (attribute != null && IsPlayerMethod(method, playerAssemblyNames, "condition"))
                {
                    AddCandidate(method, DialogueMethodKind.Condition, attribute.Key, attribute.Target, conditionCandidates);
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

            Debug.LogWarning($"[Dialogue] Editor-only {kind} method is ignored: {method.DeclaringType?.FullName}.{method.Name}");
            return false;
        }

        private static void AddCandidate(
            MethodInfo method,
            DialogueMethodKind kind,
            string key,
            DialogueTarget target,
            Dictionary<string, List<DialogueMethodDescriptor>> candidatesByKey)
        {
            if (!DialogueMethodDescriptorFactory.TryCreate(method, kind, key, target, out DialogueMethodDescriptor descriptor, out string error))
            {
                Debug.LogError($"[Dialogue] Could not register {kind} '{method.DeclaringType?.FullName}.{method.Name}': {error}");
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
                    Debug.LogError($"[Dialogue] Duplicate {kind} key '{pair.Key}' is excluded from the graph menu.");
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
