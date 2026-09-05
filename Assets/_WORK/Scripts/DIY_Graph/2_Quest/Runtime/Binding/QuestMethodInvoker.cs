using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Attribute가 붙은 Quest Action과 Condition을 찾아 등록하고 호출합니다.</summary>
    public static class QuestMethodInvoker
    {
        private sealed class GeneratedRegistrationCollector : IQuestGeneratedMethodSink
        {
            public List<QuestGeneratedMethodRegistration> Registrations { get; } = new();

            /// <summary>어셈블리를 초기화하면서 Generator가 만든 등록 정보 하나를 수집합니다.</summary>
            public void Add(QuestGeneratedMethodRegistration registration)
            {
                if (registration != null)
                {
                    Registrations.Add(registration);
                }
            }
        }

        private static readonly Dictionary<string, QuestMethodDescriptor> Actions = new();
        private static readonly Dictionary<string, QuestMethodDescriptor> Conditions = new();
        private static readonly HashSet<string> InvalidActionKeys = new();
        private static readonly HashSet<string> InvalidConditionKeys = new();
        private static bool isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Actions.Clear();
            Conditions.Clear();
            InvalidActionKeys.Clear();
            InvalidConditionKeys.Clear();
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

            Actions.Clear();
            Conditions.Clear();
            InvalidActionKeys.Clear();
            InvalidConditionKeys.Clear();
#if UNITY_EDITOR
            // Editor 전용 어셈블리는 게임 메서드 검색에서 제외합니다.
            HashSet<string> editorAssemblies = new();
            foreach (UnityEditor.Compilation.Assembly editorAssembly in UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Editor))
            {
                if ((editorAssembly.flags & UnityEditor.Compilation.AssemblyFlags.EditorAssembly) != 0)
                {
                    editorAssemblies.Add(editorAssembly.name);
                }
            }
#endif
            string runtimeAssemblyName = typeof(QuestMethodInvoker).Assembly.GetName().Name;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
#if UNITY_EDITOR
                if (editorAssemblies.Contains(assembly.GetName().Name))
                {
                    continue;
                }
#endif
                if (!CanContainQuestHandlers(assembly, runtimeAssemblyName))
                {
                    continue;
                }

                if (!TryRegisterGeneratedAssembly(assembly))
                {
                    ScanAssembly(assembly);
                }
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

            Dictionary<string, QuestMethodDescriptor> methods = kind == MethodKind.Action ? Actions : Conditions;
            if (!methods.TryGetValue(key, out QuestMethodDescriptor descriptor))
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
                Debug.LogError($"[Quest] '{descriptor.Key}'의 인수를 변환하지 못했습니다: {error}");
                return false;
            }

            object target = ResolveTarget(descriptor, context?.Controller);
            if (!descriptor.IsStatic && target == null)
            {
                return false;
            }

            try
            {
                object methodResult = descriptor.GeneratedInvoker != null
                    ? descriptor.GeneratedInvoker(target, arguments)
                    : descriptor.MethodInfo.Invoke(target, arguments);

                if (kind == MethodKind.Condition)
                {
                    if (methodResult is not bool value)
                    {
                        Debug.LogError($"[Quest] Condition '{binding.Key}'가 bool 값을 반환하지 않았습니다.");
                        return false;
                    }

                    conditionResult = value;
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

        private static bool TryRegisterGeneratedAssembly(Assembly assembly)
        {
            object[] attributes;
            try
            {
                attributes = assembly.GetCustomAttributes(typeof(QuestGeneratedProviderAttribute), false);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[Quest] 어셈블리 '{assembly.GetName().Name}'에서 생성된 Provider를 읽지 못했습니다: " +
                    exception.Message);
                return false;
            }

            if (attributes.Length == 0)
            {
                return false;
            }

            var collector = new GeneratedRegistrationCollector();
            bool failed = false;
            foreach (object value in attributes)
            {
                Type providerType = (value as QuestGeneratedProviderAttribute)?.ProviderType;
                if (providerType == null
                    || providerType.Assembly != assembly
                    || !typeof(IQuestGeneratedMethodProvider).IsAssignableFrom(providerType))
                {
                    Debug.LogError(
                        $"[Quest] 어셈블리 '{assembly.GetName().Name}'에 올바르지 않은 생성 Provider가 선언되어 있습니다.");
                    failed = true;
                    continue;
                }

                try
                {
                    var provider = (IQuestGeneratedMethodProvider)Activator.CreateInstance(providerType, true);
                    provider.Collect(collector);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[Quest] 생성 Provider '{providerType.FullName}' 실행에 실패했습니다.\n{exception}");
                    failed = true;
                }
            }

            var descriptors = new List<QuestMethodDescriptor>();
            foreach (QuestGeneratedMethodRegistration registration in collector.Registrations)
            {
                if (QuestMethodDescriptorFactory.TryCreateGenerated(
                        assembly,
                        registration,
                        out QuestMethodDescriptor descriptor,
                        out string error))
                {
                    descriptors.Add(descriptor);
                }
                else
                {
                    Debug.LogError($"[Quest] 생성된 메서드를 등록하지 못했습니다: {error}");
                    failed = true;
                }
            }

            if (failed)
            {
                return false;
            }

            foreach (QuestMethodDescriptor descriptor in descriptors)
            {
                RegisterDescriptor(descriptor);
            }
            return true;
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
                        RegisterMethod(method, MethodKind.Action, action.Key, action.Target);
                    }

                    QuestConditionAttribute condition = method.GetCustomAttribute<QuestConditionAttribute>(false);
                    if (condition != null)
                    {
                        RegisterMethod(method, MethodKind.Condition, condition.Key, condition.Target);
                    }
                }
            }
        }

        /// <summary>Reflection으로 찾은 메서드의 설명서를 만들고 등록합니다.</summary>
        private static void RegisterMethod(
            MethodInfo method,
            MethodKind kind,
            string key,
            QuestMethodTarget target)
        {
            if (!QuestMethodDescriptorFactory.TryCreateFromReflection(method, kind, key, target, out QuestMethodDescriptor descriptor, out string error))
            {
                Debug.LogError($"[Quest] '{method.DeclaringType?.FullName}.{method.Name}'을 등록하지 못했습니다: {error}");
                return;
            }

            RegisterDescriptor(descriptor);
        }

        private static void RegisterDescriptor(QuestMethodDescriptor descriptor)
        {
            IDictionary<string, QuestMethodDescriptor> methodsByKey = descriptor.Kind == MethodKind.Action
                ? Actions
                : Conditions;
            ISet<string> invalidKeys = descriptor.Kind == MethodKind.Action
                ? InvalidActionKeys
                : InvalidConditionKeys;

            if (invalidKeys.Contains(descriptor.Key))
            {
                return;
            }

            if (methodsByKey.TryGetValue(descriptor.Key, out QuestMethodDescriptor duplicate))
            {
                methodsByKey.Remove(descriptor.Key);
                invalidKeys.Add(descriptor.Key);
                Debug.LogError(
                    $"[Quest] 중복된 {descriptor.Kind} 키 '{descriptor.Key}': " +
                    $"{duplicate.DeclaringType?.FullName}.{duplicate.MethodName}, " +
                    $"{descriptor.DeclaringType?.FullName}.{descriptor.MethodName}");
                return;
            }

            methodsByKey.Add(descriptor.Key, descriptor);
        }

        private static object ResolveTarget(QuestMethodDescriptor descriptor, IQuestController controller)
        {
            if (descriptor.Target == QuestMethodTarget.Global)
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
            if (assembly == null || assembly.IsDynamic)
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
