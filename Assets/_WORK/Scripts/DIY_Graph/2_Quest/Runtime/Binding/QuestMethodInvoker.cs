using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Attribute가 붙은 Quest Action과 Condition을 찾아 등록하고 호출합니다.</summary>
    public static class QuestMethodInvoker
    {
        private static readonly Dictionary<string, QuestMethodDescriptor> actionRegistry = new();
        private static readonly Dictionary<string, QuestMethodDescriptor> conditionRegistry = new();
        private static readonly HashSet<string> invalidActionKeys = new();
        private static readonly HashSet<string> invalidConditionKeys = new();
        private static bool isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            actionRegistry.Clear();
            conditionRegistry.Clear();
            invalidActionKeys.Clear();
            invalidConditionKeys.Clear();
            isInitialized = false;
        }

        /// <summary>게임에서 사용하기 전에 Quest Action과 Condition 등록부를 만듭니다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            actionRegistry.Clear();
            conditionRegistry.Clear();
            invalidActionKeys.Clear();
            invalidConditionKeys.Clear();
#if UNITY_EDITOR
            // Editor 전용 어셈블리는 게임 메서드 검색에서 제외합니다.
            HashSet<string> playerAssemblies = new();
            foreach (UnityEditor.Compilation.Assembly playerAssembly in UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player))
            {
                playerAssemblies.Add(playerAssembly.name);
            }
#endif
            string runtimeAssemblyName = typeof(QuestMethodInvoker).Assembly.GetName().Name;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
#if UNITY_EDITOR
                if (!playerAssemblies.Contains(assembly.GetName().Name))
                {
                    continue;
                }
#endif
                if (!CanContainQuestHandlers(assembly, runtimeAssemblyName))
                {
                    continue;
                }

                ScanAssembly(assembly);
            }

            isInitialized = true;
        }

        /// <summary>호출 키 조회, 인수 복원과 대상 결정을 마친 뒤 메서드를 실행합니다.</summary>
        /// <returns>호출 성공 여부. 조건의 참·거짓은 conditionResult로 전달하며 Action에서는 false입니다.</returns>
        public static bool TryInvokeMethod(
            MethodBindingData binding,
            QuestExecutionContext context,
            MethodKind kind,
            out bool conditionResult)
        {
            conditionResult = false;
            if (kind != MethodKind.Action && kind != MethodKind.Condition)
            {
                Debug.LogError("[Quest] 메서드 종류가 올바르지 않습니다.");
                return false;
            }

            Initialize();

            string key = binding?.Key?.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError($"[Quest] {kind} 키가 비어 있습니다.");
                return false;
            }

            Dictionary<string, QuestMethodDescriptor> registry = kind == MethodKind.Action ? actionRegistry : conditionRegistry;
            if (!registry.TryGetValue(key, out QuestMethodDescriptor descriptor))
            {
                Debug.LogError($"[Quest] {kind} '{key}'이 등록되지 않았습니다.");
                return false;
            }

            if (!MethodArgumentCodec.TryCreateQuestRuntimeArguments(
                    binding.Arguments,
                    descriptor,
                    context,
                    out object[] arguments,
                    out string error))
            {
                Debug.LogError($"[Quest] {error}");
                return false;
            }

            object target = GetTargetInstance(descriptor, context?.Controller);
            if (!descriptor.IsStatic && target == null)
            {
                return false;
            }

            try
            {
                object methodResult = descriptor.MethodInfo.Invoke(target, arguments);

                if (kind == MethodKind.Condition)
                {
                    conditionResult = (bool)methodResult;
                }

                return true;
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogError($"[Quest] 메서드 '{descriptor.Key}' 실행 중 예외가 발생했습니다.\n{exception.InnerException ?? exception}");
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Quest] 메서드 '{descriptor.Key}'를 호출하지 못했습니다.\n{exception}");
                return false;
            }
        }

        private static void ScanAssembly(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Quest] 어셈블리 '{assembly.GetName().Name}'을 검색하지 못했습니다: {exception.Message}");
                return;
            }

            foreach (Type type in types)
            {
                if (type == null)
                {
                    continue;
                }

                foreach (MethodInfo method in type.GetMethods(
                             BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                             BindingFlags.Public | BindingFlags.NonPublic))
                {
                    QuestActionAttribute action = method.GetCustomAttribute<QuestActionAttribute>(false);
                    if (action != null)
                    {
                        RegisterMethod(method, MethodKind.Action, action.Key, action.Owner);
                    }

                    QuestConditionAttribute condition = method.GetCustomAttribute<QuestConditionAttribute>(false);
                    if (condition != null)
                    {
                        RegisterMethod(method, MethodKind.Condition, condition.Key, condition.Owner);
                    }
                }
            }
        }

        /// <summary>Reflection으로 찾은 메서드의 설명서를 만들고 등록합니다.</summary>
        private static void RegisterMethod(
            MethodInfo method,
            MethodKind kind,
            string key,
            QuestMethodOwner owner)
        {
            if (!QuestMethodDescriptorFactory.TryCreateDescriptor(method, kind, key, owner, out QuestMethodDescriptor descriptor, out string error))
            {
                Debug.LogError($"[Quest] {error}");
                return;
            }

            RegisterDescriptor(descriptor);
        }

        private static void RegisterDescriptor(QuestMethodDescriptor descriptor)
        {
            IDictionary<string, QuestMethodDescriptor> registry = descriptor.Kind == MethodKind.Action
                ? actionRegistry
                : conditionRegistry;
            ISet<string> invalidKeys = descriptor.Kind == MethodKind.Action
                ? invalidActionKeys
                : invalidConditionKeys;

            if (invalidKeys.Contains(descriptor.Key))
            {
                return;
            }

            if (registry.TryGetValue(descriptor.Key, out QuestMethodDescriptor duplicate))
            {
                registry.Remove(descriptor.Key);
                invalidKeys.Add(descriptor.Key);
                Debug.LogError(
                    $"[Quest] 중복된 {descriptor.Kind} 키 '{descriptor.Key}': " +
                    $"{duplicate.DeclaringType?.FullName}.{duplicate.MethodName}, " +
                    $"{descriptor.DeclaringType?.FullName}.{descriptor.MethodName}");
                return;
            }

            registry.Add(descriptor.Key, descriptor);
        }

        private static object GetTargetInstance(QuestMethodDescriptor descriptor, IQuestController controller)
        {
            if (descriptor.Owner == QuestMethodOwner.Global)
            {
                return null;
            }

            if (controller != null && descriptor.DeclaringType.IsInstanceOfType(controller))
            {
                return controller;
            }

            Debug.LogError(
                $"[Quest] Controller {descriptor.Kind} '{descriptor.Key}'에는 '{descriptor.DeclaringType?.FullName}' 타입이 필요하지만, " +
                $"현재 Controller 타입은 '{controller?.GetType().FullName ?? "null"}'입니다.");
            return null;
        }


        private static bool CanContainQuestHandlers(Assembly assembly, string runtimeAssemblyName)
        {
            if (assembly.IsDynamic)
            {
                return false;
            }

            string name = assembly.GetName().Name;
            if (name == runtimeAssemblyName)
            {
                return true;
            }

            try
            {
                foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
                {
                    if (reference.Name == runtimeAssemblyName)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
