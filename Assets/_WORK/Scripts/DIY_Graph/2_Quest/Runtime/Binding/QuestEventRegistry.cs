using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>Attribute가 붙은 Quest Action과 Condition을 찾아 등록하고 호출합니다.</summary>
    public static class QuestEventRegistry
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

        private enum GeneratedProviderResult
        {
            None,
            Success,
            Failed
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
            EnsureInitialized();
        }

        private static void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            Actions.Clear();
            Conditions.Clear();
            InvalidActionKeys.Clear();
            InvalidConditionKeys.Clear();
            string runtimeAssemblyName = typeof(QuestActionAttribute).Assembly.GetName().Name;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!CanContainQuestHandlers(assembly, runtimeAssemblyName))
                {
                    continue;
                }

                GeneratedProviderResult generated = TryRegisterGeneratedAssembly(assembly);
                if (generated != GeneratedProviderResult.Success)
                {
                    ScanAssembly(assembly);
                }
            }

            isInitialized = true;
            Debug.Log($"[Quest] Attribute Action {Actions.Count}개와 Condition {Conditions.Count}개를 등록했습니다.");
        }

        /// <summary>등록된 Action 하나를 실행하고 해당 키가 이 등록부의 키였는지 함께 반환합니다.</summary>
        public static bool TryExecuteAction(
            MethodCallData methodCall,
            IQuestController controller,
            QuestExecutionContext context,
            out bool registered)
        {
            EnsureInitialized();
            QuestMethodDescriptor descriptor = null;
            registered = !string.IsNullOrWhiteSpace(methodCall?.Key) && Actions.TryGetValue(methodCall.Key, out descriptor);
            if (!registered)
            {
                return false;
            }

            if (!TryBuildArguments(descriptor, methodCall.Arguments, context, out object[] invocationArguments))
            {
                return false;
            }

            object target = ResolveTarget(descriptor, controller);
            if (!descriptor.IsStatic && target == null)
            {
                return false;
            }

            return TryInvoke(descriptor, target, invocationArguments, out _);
        }

        /// <summary>등록된 Condition 하나를 평가하고 해당 키가 이 등록부의 키였는지 함께 반환합니다.</summary>
        public static bool TryEvaluateCondition(
            MethodCallData methodCall,
            IQuestController controller,
            QuestExecutionContext context,
            out bool result,
            out bool registered)
        {
            result = false;
            EnsureInitialized();
            QuestMethodDescriptor descriptor = null;
            registered = !string.IsNullOrWhiteSpace(methodCall?.Key) && Conditions.TryGetValue(methodCall.Key, out descriptor);
            if (!registered)
            {
                return false;
            }

            if (!TryBuildArguments(descriptor, methodCall.Arguments, context, out object[] invocationArguments))
            {
                return false;
            }

            object target = ResolveTarget(descriptor, controller);
            if (!descriptor.IsStatic && target == null)
            {
                return false;
            }

            if (!TryInvoke(descriptor, target, invocationArguments, out object invocationResult)
                || invocationResult is not bool value)
            {
                return false;
            }

            result = value;
            return true;
        }

        private static GeneratedProviderResult TryRegisterGeneratedAssembly(Assembly assembly)
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
                return GeneratedProviderResult.None;
            }

            if (attributes.Length == 0)
            {
                return GeneratedProviderResult.None;
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
                    Debug.LogError($"[Quest] 생성 Provider '{providerType.FullName}' 실행에 실패했습니다.");
                    Debug.LogException(exception);
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
                return GeneratedProviderResult.Failed;
            }

            foreach (QuestMethodDescriptor descriptor in descriptors)
            {
                RegisterDescriptor(descriptor);
            }
            return GeneratedProviderResult.Success;
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
                        Register(method, MethodKind.Action, action.Key, action.Target);
                    }

                    QuestConditionAttribute condition = method.GetCustomAttribute<QuestConditionAttribute>(false);
                    if (condition != null)
                    {
                        Register(method, MethodKind.Condition, condition.Key, condition.Target);
                    }
                }
            }
        }

        private static void Register(
            MethodInfo method,
            MethodKind kind,
            string key,
            QuestMethodTarget target)
        {
            if (!QuestMethodDescriptorFactory.TryCreate(method, kind, key, target, out QuestMethodDescriptor descriptor, out string error))
            {
                Debug.LogError($"[Quest] '{method.DeclaringType?.FullName}.{method.Name}'을 등록하지 못했습니다: {error}");
                return;
            }

            RegisterDescriptor(descriptor);
        }

        private static void RegisterDescriptor(QuestMethodDescriptor descriptor)
        {
            IDictionary<string, QuestMethodDescriptor> registry = descriptor.Kind == MethodKind.Action
                ? Actions
                : Conditions;
            ISet<string> invalidKeys = descriptor.Kind == MethodKind.Action
                ? InvalidActionKeys
                : InvalidConditionKeys;

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

        private static bool TryBuildArguments(
            QuestMethodDescriptor descriptor,
            IReadOnlyList<MethodArgumentData> arguments,
            QuestExecutionContext context,
            out object[] invocationArguments)
        {
            if (MethodArgumentCodec.TryBuildQuestInvocationArguments(
                    arguments,
                    descriptor,
                    context,
                    out invocationArguments,
                    out string error))
            {
                return true;
            }

            Debug.LogError($"[Quest] '{descriptor.Key}'의 인수를 변환하지 못했습니다: {error}");
            return false;
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
                $"[Quest] Controller Action '{descriptor.Key}'에는 '{descriptor.DeclaringType?.FullName}' 타입이 필요하지만, " +
                $"현재 Controller 타입은 '{controller?.GetType().FullName ?? "null"}'입니다.");
            return null;
        }

        private static bool TryInvoke(
            QuestMethodDescriptor descriptor,
            object target,
            object[] arguments,
            out object result)
        {
            result = null;
            try
            {
                result = descriptor.GeneratedInvoker != null
                    ? descriptor.GeneratedInvoker(target, arguments)
                    : descriptor.Method.Invoke(target, arguments);
                return true;
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogError($"[Quest] 메서드 '{descriptor.Key}' 실행 중 예외가 발생했습니다.");
                Debug.LogException(exception.InnerException ?? exception);
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Quest] 메서드 '{descriptor.Key}'를 호출하지 못했습니다.");
                Debug.LogException(exception);
                return false;
            }
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

            if (name.EndsWith(".Editor", StringComparison.Ordinal)
                || name.StartsWith("UnityEditor", StringComparison.Ordinal))
            {
                return false;
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
