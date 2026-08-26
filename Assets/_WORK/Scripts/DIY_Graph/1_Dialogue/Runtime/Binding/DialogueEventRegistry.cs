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
	public static class DialogueEventRegistry
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
			Assembly assembly = typeof(DialogueActionAttribute).Assembly;
			string name = assembly.GetName().Name;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Assembly[] array = assemblies;
			foreach (Assembly assembly2 in array)
			{
				if (CanContainDialogueHandlers(assembly2, name))
				{
					GeneratedProviderResult generatedProviderResult = TryRegisterGeneratedAssembly(assembly2);
					if (generatedProviderResult != GeneratedProviderResult.Success)
					{
						ScanAssemblyByReflection(assembly2);
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
			GeneratedRegistrationCollector generatedRegistrationCollector = new GeneratedRegistrationCollector();
			bool flag = false;
			object[] array = customAttributes;
			object[] array2 = array;
			foreach (object obj in array2)
			{
				Type type = ((obj is DialogueGeneratedProviderAttribute dialogueGeneratedProviderAttribute) ? dialogueGeneratedProviderAttribute.ProviderType : null);
				if (type == null || type.Assembly != assembly || !typeof(IDialogueGeneratedMethodProvider).IsAssignableFrom(type))
				{
					Debug.LogError($"[Dialogue] 어셈블리 '{assembly.GetName().Name}'에 올바르지 않은 생성 Provider가 선언되어 있습니다.");
					flag = true;
					continue;
				}
				try
				{
					IDialogueGeneratedMethodProvider dialogueGeneratedMethodProvider = (IDialogueGeneratedMethodProvider)Activator.CreateInstance(type, nonPublic: true);
					dialogueGeneratedMethodProvider.Collect(generatedRegistrationCollector);
				}
				catch (Exception ex2)
				{
					Debug.LogError($"[Dialogue] 생성 Provider '{type.FullName}'가 메서드를 수집하는 중 실패했습니다.");
					Debug.LogException(ex2);
					flag = true;
				}
			}
			List<DialogueMethodDescriptor> list = new List<DialogueMethodDescriptor>();
			foreach (DialogueGeneratedMethodRegistration registration in generatedRegistrationCollector.Registrations)
			{
				if (!DialogueMethodDescriptorFactory.TryCreateGenerated(assembly, registration, out var descriptor, out var error))
				{
					Debug.LogError($"[Dialogue] 생성된 메서드를 등록하지 못했습니다: {error}");
					flag = true;
				}
				else
				{
					list.Add(descriptor);
				}
			}
			if (flag)
			{
				Debug.LogError($"[Dialogue] '{assembly.GetName().Name}'의 생성 메타데이터가 올바르지 않아 Reflection 검색으로 대체합니다.");
				return GeneratedProviderResult.Failed;
			}
			foreach (DialogueMethodDescriptor item in list)
			{
				if (item.Kind == MethodKind.Action)
				{
					RegisterUnique(item, actionRegistry, invalidActionKeys, "Action");
				}
				else
				{
					RegisterUnique(item, conditionRegistry, invalidConditionKeys, "Condition");
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
			catch (Exception ex2)
			{
				Debug.LogWarning($"[Dialogue] 어셈블리 '{assembly.GetName().Name}'을 검색하지 못했습니다: {ex2.Message}");
				return;
			}
			Type[] array = types;
			Type[] array2 = array;
			foreach (Type type in array2)
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
				catch (Exception ex3)
				{
					Debug.LogWarning($"[Dialogue] '{type.FullName}'의 메서드를 검사하지 못했습니다: {ex3.Message}");
					continue;
				}
				MethodInfo[] array3 = methods;
				MethodInfo[] array4 = array3;
				foreach (MethodInfo methodInfo in array4)
				{
					DialogueActionAttribute customAttribute = ((MemberInfo)methodInfo).GetCustomAttribute<DialogueActionAttribute>(inherit: false);
					if (customAttribute != null)
					{
						RegisterAction(methodInfo, customAttribute);
					}
					DialogueConditionAttribute customAttribute2 = ((MemberInfo)methodInfo).GetCustomAttribute<DialogueConditionAttribute>(inherit: false);
					if (customAttribute2 != null)
					{
						RegisterCondition(methodInfo, customAttribute2);
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
			string name = assembly.GetName().Name;
			if (name == dialogueAssemblyName)
			{
				return true;
			}
			if (name == "Assembly-CSharp-Editor" || name.EndsWith(".Editor", StringComparison.Ordinal) || name.StartsWith("UnityEditor", StringComparison.Ordinal))
			{
				return false;
			}
			try
			{
				AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
				AssemblyName[] array = referencedAssemblies;
				foreach (AssemblyName assemblyName in array)
				{
					if (assemblyName.Name == dialogueAssemblyName)
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
			if (!DialogueMethodDescriptorFactory.TryCreate(method, MethodKind.Action, attribute.Key, attribute.Target, out var descriptor, out var error))
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
			if (!DialogueMethodDescriptorFactory.TryCreate(method, MethodKind.Condition, attribute.Key, attribute.Target, out var descriptor, out var error))
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
			if (!invalidKeys.Contains(key))
			{
				if (registry.TryGetValue(key, out var value))
				{
					registry.Remove(key);
					invalidKeys.Add(key);
					Debug.LogError($"[Dialogue] 중복된 {kind} 키 '{key}': {value.QualifiedMethodName}, {descriptor.QualifiedMethodName}");
				}
				else
				{
					registry.Add(key, descriptor);
				}
			}
		}

		private static object GetTargetInstance(DialogueMethodDescriptor descriptor, DialogueContext context)
		{
			if (descriptor.IsStatic)
			{
				return null;
			}
			if (context == null)
			{
				Debug.LogError("[Dialogue] 인스턴스 Action과 Condition 대상을 사용하려면 DialogueContext가 필요합니다.");
				return null;
			}
			GameObject target = descriptor.Target == DialogueTarget.Speaker ? context.Speaker : context.Interactor;
			if (target == null)
			{
				Debug.LogWarning($"[Dialogue] Context의 대상 '{descriptor.Target}'이 null입니다.");
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
		public static bool ExecuteAction(MethodCallData methodCall, DialogueContext context)
		{
			string key = methodCall?.Key;
			if (string.IsNullOrWhiteSpace(key) || key == "None")
			{
				return false;
			}
			EnsureInitialized();
			if (!actionRegistry.TryGetValue(key, out var value))
			{
				Debug.LogWarning($"[Dialogue] 키 '{key}'에 등록된 Action이 없습니다.");
				return false;
			}
			if (!MethodArgumentCodec.TryBuildInvocationArguments(methodCall.Arguments, value, context, out var invocationArguments, out var error))
			{
				Debug.LogError($"[Dialogue] Action '{key}'의 인수를 변환하지 못했습니다: {error}");
				return false;
			}
			object targetInstance = GetTargetInstance(value, context);
			if (!value.IsStatic && targetInstance == null)
			{
				return false;
			}
			try
			{
				if (value.GeneratedInvoker != null)
				{
					value.GeneratedInvoker(targetInstance, invocationArguments);
				}
				else
				{
					value.Method.Invoke(targetInstance, invocationArguments);
				}
				return true;
			}
			catch (TargetInvocationException ex)
			{
				LogInvocationException("Action", key, ex.InnerException ?? ex, targetInstance);
				return false;
			}
			catch (Exception exception)
			{
				LogInvocationException("Action", key, exception, targetInstance);
				return false;
			}
		}

		/// <summary>등록된 Condition 하나의 인수를 복원하고 평가합니다.</summary>
		public static bool TryEvaluateCondition(MethodCallData methodCall, DialogueContext context, out bool result)
		{
			result = false;
			string key = methodCall?.Key;
			if (string.IsNullOrWhiteSpace(key) || key == "None")
			{
				return false;
			}
			EnsureInitialized();
			if (!conditionRegistry.TryGetValue(key, out var value))
			{
				Debug.LogWarning($"[Dialogue] 키 '{key}'에 등록된 Condition이 없습니다.");
				return false;
			}
			if (!MethodArgumentCodec.TryBuildInvocationArguments(methodCall.Arguments, value, context, out var invocationArguments, out var error))
			{
				Debug.LogError($"[Dialogue] Condition '{key}'의 인수를 변환하지 못했습니다: {error}");
				return false;
			}
			object targetInstance = GetTargetInstance(value, context);
			if (!value.IsStatic && targetInstance == null)
			{
				return false;
			}
			try
			{
				object invocationResult = value.GeneratedInvoker != null
					? value.GeneratedInvoker(targetInstance, invocationArguments)
					: value.Method.Invoke(targetInstance, invocationArguments);
				if (invocationResult is not bool conditionResult)
				{
					Debug.LogError($"[Dialogue] 생성된 Condition '{key}'가 bool 값을 반환하지 않았습니다.");
					return false;
				}
				result = conditionResult;
				return true;
			}
			catch (TargetInvocationException ex)
			{
				LogInvocationException("Condition", key, ex.InnerException ?? ex, targetInstance);
				return false;
			}
			catch (Exception exception)
			{
				LogInvocationException("Condition", key, exception, targetInstance);
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






