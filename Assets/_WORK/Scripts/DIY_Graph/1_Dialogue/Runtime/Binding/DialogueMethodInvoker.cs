using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>
	/// Dialogue Attribute 메서드를 찾아 키 중복을 검증하고 문맥 대상을 결정한 뒤 Action 또는 Condition을 호출합니다.
	/// Generator가 만든 등록 정보를 우선 사용하고 Reflection은 안전한 대체 경로로 사용합니다.
	/// </summary>
	public static class DialogueMethodInvoker
	{
		private sealed class GeneratedRegistrationCollector : IDialogueGeneratedMethodSink
		{
			public List<DialogueGeneratedMethodRegistration> Registrations { get; } = new List<DialogueGeneratedMethodRegistration>();

			/// <summary>어셈블리를 초기화하면서 Generator가 만든 등록 정보 하나를 수집합니다.</summary>
			public void Add(DialogueGeneratedMethodRegistration registration)
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

		private static readonly Dictionary<string, DialogueMethodDescriptor> actionRegistry = new Dictionary<string, DialogueMethodDescriptor>();

		private static readonly Dictionary<string, DialogueMethodDescriptor> conditionRegistry = new Dictionary<string, DialogueMethodDescriptor>();

		private static readonly HashSet<string> invalidActionKeys = new HashSet<string>();

		private static readonly HashSet<string> invalidConditionKeys = new HashSet<string>();

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

		/// <summary>메서드 등록을 미리 초기화합니다. 여러 번 호출해도 결과는 같습니다.</summary>
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
			actionRegistry.Clear();
			conditionRegistry.Clear();
			invalidActionKeys.Clear();
			invalidConditionKeys.Clear();
			Assembly runtimeAssembly = typeof(DialogueActionAttribute).Assembly;
			string runtimeAssemblyName = runtimeAssembly.GetName().Name;
			foreach (Assembly candidateAssembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (CanContainDialogueHandlers(candidateAssembly, runtimeAssemblyName))
				{
					GeneratedProviderResult providerResult = TryRegisterGeneratedAssembly(candidateAssembly);
					if (providerResult != GeneratedProviderResult.Success)
					{
						ScanAssemblyByReflection(candidateAssembly);
					}
				}
			}
			isInitialized = true;
			Debug.Log($"[Dialogue] Action {actionRegistry.Count}개와 Condition {conditionRegistry.Count}개를 등록했습니다.");
		}

		private static GeneratedProviderResult TryRegisterGeneratedAssembly(Assembly assembly)
		{
			object[] customAttributes;
			try
			{
				customAttributes = assembly.GetCustomAttributes(typeof(DialogueGeneratedProviderAttribute), inherit: false);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[Dialogue] 어셈블리 '{assembly.GetName().Name}'에서 생성된 Provider를 읽지 못했습니다: {ex.Message}");
				return GeneratedProviderResult.None;
			}
			if (customAttributes.Length == 0)
			{
				return GeneratedProviderResult.None;
			}
			var collector = new GeneratedRegistrationCollector();
			bool hasErrors = false;
			foreach (object customAttribute in customAttributes)
			{
				Type providerType = customAttribute is DialogueGeneratedProviderAttribute providerAttribute
					? providerAttribute.ProviderType
					: null;
				if (providerType == null
					|| providerType.Assembly != assembly
					|| !typeof(IDialogueGeneratedMethodProvider).IsAssignableFrom(providerType))
				{
					Debug.LogError($"[Dialogue] 어셈블리 '{assembly.GetName().Name}'에 올바르지 않은 생성 Provider가 선언되어 있습니다.");
					hasErrors = true;
					continue;
				}
				try
				{
					var provider = (IDialogueGeneratedMethodProvider)Activator.CreateInstance(providerType, nonPublic: true);
					provider.Collect(collector);
				}
				catch (Exception exception)
				{
					Debug.LogError($"[Dialogue] 생성 Provider '{providerType.FullName}'가 메서드를 수집하는 중 실패했습니다.");
					Debug.LogException(exception);
					hasErrors = true;
				}
			}
			var descriptors = new List<DialogueMethodDescriptor>();
			foreach (DialogueGeneratedMethodRegistration registration in collector.Registrations)
			{
				if (!DialogueMethodDescriptorFactory.TryCreateGenerated(
						assembly,
						registration,
						out DialogueMethodDescriptor descriptor,
						out string error))
				{
					Debug.LogError($"[Dialogue] 생성된 메서드를 등록하지 못했습니다: {error}");
					hasErrors = true;
				}
				else
				{
					descriptors.Add(descriptor);
				}
			}
			if (hasErrors)
			{
				Debug.LogError($"[Dialogue] '{assembly.GetName().Name}'의 생성 메타데이터가 올바르지 않아 Reflection 검색으로 대체합니다.");
				return GeneratedProviderResult.Failed;
			}
			foreach (DialogueMethodDescriptor descriptor in descriptors)
			{
				if (descriptor.Kind == MethodKind.Action)
				{
					RegisterUnique(descriptor, actionRegistry, invalidActionKeys, "Action");
				}
				else
				{
					RegisterUnique(descriptor, conditionRegistry, invalidConditionKeys, "Condition");
				}
			}
			return GeneratedProviderResult.Success;
		}

		private static void ScanAssemblyByReflection(Assembly assembly)
		{
			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				types = ex.Types;
			}
			catch (Exception exception)
			{
				Debug.LogWarning($"[Dialogue] 어셈블리 '{assembly.GetName().Name}'을 검색하지 못했습니다: {exception.Message}");
				return;
			}
			foreach (Type type in types)
			{
				if (type == null)
				{
					continue;
				}
				MethodInfo[] methods;
				try
				{
					methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				}
				catch (Exception exception)
				{
					Debug.LogWarning($"[Dialogue] '{type.FullName}'의 메서드를 검사하지 못했습니다: {exception.Message}");
					continue;
				}
				foreach (MethodInfo method in methods)
				{
					DialogueActionAttribute actionAttribute = method.GetCustomAttribute<DialogueActionAttribute>(inherit: false);
					if (actionAttribute != null)
					{
						RegisterAction(method, actionAttribute);
					}
					DialogueConditionAttribute conditionAttribute = method.GetCustomAttribute<DialogueConditionAttribute>(inherit: false);
					if (conditionAttribute != null)
					{
						RegisterCondition(method, conditionAttribute);
					}
				}
			}
		}

		private static bool CanContainDialogueHandlers(Assembly assembly, string dialogueAssemblyName)
		{
			if (assembly == null || assembly.IsDynamic)
			{
				return false;
			}
			string assemblyName = assembly.GetName().Name;
			if (assemblyName == dialogueAssemblyName)
			{
				return true;
			}
			if (assemblyName == "Assembly-CSharp-Editor"
				|| assemblyName.EndsWith(".Editor", StringComparison.Ordinal)
				|| assemblyName.StartsWith("UnityEditor", StringComparison.Ordinal))
			{
				return false;
			}
			try
			{
				foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
				{
					if (reference.Name == dialogueAssemblyName)
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

		private static void RegisterAction(MethodInfo method, DialogueActionAttribute attribute)
		{
			if (!DialogueMethodDescriptorFactory.TryCreateFromReflection(
					method,
					MethodKind.Action,
					attribute.Key,
					attribute.Owner,
					out DialogueMethodDescriptor descriptor,
					out string error))
			{
				Debug.LogError($"[Dialogue] Action을 등록하지 못했습니다: {error}");
			}
			else
			{
				RegisterUnique(descriptor, actionRegistry, invalidActionKeys, "Action");
			}
		}

		private static void RegisterCondition(MethodInfo method, DialogueConditionAttribute attribute)
		{
			if (!DialogueMethodDescriptorFactory.TryCreateFromReflection(
					method,
					MethodKind.Condition,
					attribute.Key,
					attribute.Owner,
					out DialogueMethodDescriptor descriptor,
					out string error))
			{
				Debug.LogError($"[Dialogue] Condition을 등록하지 못했습니다: {error}");
			}
			else
			{
				RegisterUnique(descriptor, conditionRegistry, invalidConditionKeys, "Condition");
			}
		}

		private static void RegisterUnique(DialogueMethodDescriptor descriptor, Dictionary<string, DialogueMethodDescriptor> registry, HashSet<string> invalidKeys, string kind)
		{
			string key = descriptor.Key;
			if (invalidKeys.Contains(key))
			{
				return;
			}
			if (registry.TryGetValue(key, out DialogueMethodDescriptor existingDescriptor))
			{
				registry.Remove(key);
				invalidKeys.Add(key);
				Debug.LogError($"[Dialogue] 중복된 {kind} 키 '{key}': {existingDescriptor.QualifiedMethodName}, {descriptor.QualifiedMethodName}");
			}
			else
			{
				registry.Add(key, descriptor);
			}
		}

		private static object GetTargetInstance(DialogueMethodDescriptor descriptor, DialogueExecutionContext context)
		{
			if (descriptor.IsStatic)
			{
				return null;
			}
			if (context == null)
			{
				Debug.LogError("[Dialogue] 인스턴스 Action과 Condition 대상을 사용하려면 DialogueExecutionContext가 필요합니다.");
				return null;
			}
			GameObject target = descriptor.Owner == DialogueMethodOwner.Speaker ? context.Speaker : context.Interactor;
			if (target == null)
			{
				Debug.LogWarning($"[Dialogue] DialogueExecutionContext의 대상 '{descriptor.Owner}'이 null입니다.");
				return null;
			}
			Component[] components = target.GetComponents(descriptor.DeclaringType);
			if (components.Length == 0)
			{
				Debug.LogWarning($"[Dialogue] '{target.name}'에 '{descriptor.DeclaringType?.Name}' 컴포넌트가 없습니다.", target);
				return null;
			}
			if (components.Length > 1)
			{
				Debug.LogError($"[Dialogue] '{target.name}'에 '{descriptor.DeclaringType?.Name}' 컴포넌트가 {components.Length}개 있어 호출 대상을 결정할 수 없습니다.", target);
				return null;
			}
			return components[0];
		}

		/// <summary>등록된 Action 하나의 인수를 복원하고 실행합니다.</summary>
		public static bool TryExecuteAction(MethodCallData methodCall, DialogueExecutionContext context)
		{
			return TryInvokeMethod(methodCall, context, MethodKind.Action, out _);
		}

		/// <summary>등록된 Condition 하나의 인수를 복원하고 평가합니다.</summary>
		public static bool TryEvaluateCondition(MethodCallData methodCall, DialogueExecutionContext context, out bool result)
		{
			result = false;
			if (!TryInvokeMethod(methodCall, context, MethodKind.Condition, out object methodResult))
			{
				return false;
			}
			if (methodResult is not bool conditionResult)
			{
				Debug.LogError($"[Dialogue] 생성된 Condition '{methodCall.Key}'가 bool 값을 반환하지 않았습니다.");
				return false;
			}
			result = conditionResult;
			return true;
		}

		/// <summary>메서드 종류에 맞는 등록 정보를 찾아 인수를 복원하고 호출합니다.</summary>
		private static bool TryInvokeMethod(MethodCallData methodCall, DialogueExecutionContext context, MethodKind kind, out object methodResult)
		{
			methodResult = null;
			string key = methodCall?.Key;
			if (string.IsNullOrWhiteSpace(key) || key == "None")
			{
				return false;
			}

			EnsureInitialized();
			Dictionary<string, DialogueMethodDescriptor> registry = kind == MethodKind.Action ? actionRegistry : conditionRegistry;
			string kindName = kind.ToString();
			if (!registry.TryGetValue(key, out DialogueMethodDescriptor descriptor))
			{
				Debug.LogWarning($"[Dialogue] 키 '{key}'에 등록된 {kindName}이 없습니다.");
				return false;
			}
			if (!MethodArgumentCodec.TryCreateDialogueRuntimeArguments(methodCall.Arguments, descriptor, context, out object[] arguments, out string error))
			{
				Debug.LogError($"[Dialogue] {kindName} '{key}'의 인수를 변환하지 못했습니다: {error}");
				return false;
			}

			object target = GetTargetInstance(descriptor, context);
			if (!descriptor.IsStatic && target == null)
			{
				return false;
			}
			try
			{
				methodResult = descriptor.GeneratedInvoker != null
					? descriptor.GeneratedInvoker(target, arguments)
					: descriptor.MethodInfo.Invoke(target, arguments);
				return true;
			}
			catch (TargetInvocationException ex)
			{
				LogInvocationException(kindName, key, ex.InnerException ?? ex, target);
				return false;
			}
			catch (Exception exception)
			{
				LogInvocationException(kindName, key, exception, target);
				return false;
			}
		}

		private static void LogInvocationException(string kind, string key, Exception exception, object instance)
		{
			UnityEngine.Object unityContext = instance as UnityEngine.Object;
			Debug.LogError($"[Dialogue] {kind} '{key}' 실행 중 예외가 발생했습니다.", unityContext);
			Debug.LogException(exception, unityContext);
		}
	}
}






