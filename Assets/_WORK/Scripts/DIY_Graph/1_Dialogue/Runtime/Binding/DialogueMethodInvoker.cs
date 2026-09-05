using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>
	/// Dialogue Attribute 메서드를 찾아 키 중복을 검증하고 메서드를 호출<para></para>
	/// Generator가 만든 등록 정보를 우선 사용하고 Reflection은 안전한 대체 경로로 사용
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

		private static readonly Dictionary<string, DialogueMethodDescriptor> actionRegistry = new();

		private static readonly Dictionary<string, DialogueMethodDescriptor> conditionRegistry = new();

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


		/// <summary>메서드 등록을 미리 초기화</summary>
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
            // 테스트용 DialogueAction이 게임 메서드로 등록되지 않도록 에디터 전용 어셈블리를 선별
            HashSet<string> editorAssemblies = new();
			foreach (UnityEditor.Compilation.Assembly editorAssembly in UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Editor))
			{
				if ((editorAssembly.flags & UnityEditor.Compilation.AssemblyFlags.EditorAssembly) != 0)
				{
					editorAssemblies.Add(editorAssembly.name);
				}
			}
#endif
			//로드된 어셈블리 하나씩 꺼내서 분류
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
#if UNITY_EDITOR
				if (editorAssemblies.Contains(assembly.GetName().Name))
				{
					continue;
				}
#endif
				if (!CanUseDialogueAttributes(assembly, typeof(DialogueActionAttribute).Assembly.GetName().Name))
				{
					continue;
				}
				if (!TryRegisterGeneratedAssembly(assembly))
				{
					ScanAssemblyByReflection(assembly);
				}
			}
			isInitialized = true;
		}


        private static bool CanUseDialogueAttributes(Assembly assembly, string dialogueAssemblyName)
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
            try
            {
                // 혹시 dialogue 어셈블리들을 참조하는지
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

        //=========================== 메서드를 가져오기 (generator or reflection)===============================================

        private static bool TryRegisterGeneratedAssembly(Assembly assembly)
		{
			object[] customAttributes;
			try
			{
				customAttributes = assembly.GetCustomAttributes(typeof(DialogueGeneratedProviderAttribute), inherit: false);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[Dialogue] 어셈블리 '{assembly.GetName().Name}'에서 생성된 Provider를 읽지 못했습니다: {ex.Message}");
				return false;
			}
			if (customAttributes.Length == 0)
			{
				return false;
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
					Debug.LogError($"[Dialogue] 생성 Provider '{providerType.FullName}'가 메서드를 수집하는 중 실패했습니다.\n{exception}");
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
				return false;
			}
			foreach (DialogueMethodDescriptor descriptor in descriptors)
			{
				RegisterDescriptor(descriptor);
			}
			return true;
		}


		/// <summary>
		/// 리플렉션으로 메서드 가져오기(Assembly -> Type -> method -> attribute순)
		/// </summary>
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
						RegisterMethod(method, MethodKind.Action, actionAttribute.Key, actionAttribute.Owner);
					}
					DialogueConditionAttribute conditionAttribute = method.GetCustomAttribute<DialogueConditionAttribute>(inherit: false);
					if (conditionAttribute != null)
					{
						RegisterMethod(method, MethodKind.Condition, conditionAttribute.Key, conditionAttribute.Owner);
					}
				}
			}
		}

		//=========================== 메서드를 가져온 이후 ===============================================

		/// <summary>Reflection으로 찾은 메서드의 설명서를 만들고 등록</summary>
		private static void RegisterMethod(MethodInfo method, MethodKind kind, string key, DialogueMethodOwner owner)
		{
			if (!DialogueMethodDescriptorFactory.TryCreateFromReflection(method, kind, key, owner, out DialogueMethodDescriptor descriptor, out string error))
			{
				Debug.LogError($"[Dialogue] {kind}을 등록하지 못했습니다: {error}");
				return;
			}
			RegisterDescriptor(descriptor);
		}

		/// <summary>종류에 맞는 등록부에 설명서를 추가하고, 중복된 키는 등록에서 제외</summary>
		private static void RegisterDescriptor(DialogueMethodDescriptor descriptor)
		{
			Dictionary<string, DialogueMethodDescriptor> registry = descriptor.Kind == MethodKind.Action ? actionRegistry : conditionRegistry;
			HashSet<string> invalidKeys = descriptor.Kind == MethodKind.Action ? invalidActionKeys : invalidConditionKeys;

			string key = descriptor.Key;
			if (invalidKeys.Contains(key))
			{
				return;
			}
			if (registry.TryGetValue(key, out DialogueMethodDescriptor existingDescriptor))
			{
				registry.Remove(key);
				invalidKeys.Add(key);
				Debug.LogError($"[Dialogue] 중복된 {descriptor.Kind} 키 '{key}': {existingDescriptor.QualifiedMethodName}, {descriptor.QualifiedMethodName}");
			}
			else
			{
				registry.Add(key, descriptor);
			}
		}

		/// <summary>메서드 종류에 맞는 등록 정보를 찾아 인수를 복원하고 호출</summary>
		public static bool TryInvokeMethod(MethodBindingData binding, DialogueExecutionContext context, MethodKind kind, out bool conditionResult)
		{
			conditionResult = false;
			if (kind != MethodKind.Action && kind != MethodKind.Condition)
			{
				Debug.LogError("[Dialogue] 메서드 종류가 올바르지 않습니다.");
				return false;
			}
			string key = binding?.Key;
			if (string.IsNullOrWhiteSpace(key) || key == "None")
			{
				Debug.LogError($"[Dialogue] {kind} 키가 비어 있습니다.");
				return false;
			}

			Initialize();
			Dictionary<string, DialogueMethodDescriptor> registry = kind == MethodKind.Action ? actionRegistry : conditionRegistry;

			//descriptor 찾기
			if (!registry.TryGetValue(key, out DialogueMethodDescriptor descriptor))
			{
				Debug.LogWarning($"[Dialogue] 키 '{key}'에 등록된 {kind}이 없습니다.");
				return false;
			}
			//argument 복원
			if (!MethodArgumentCodec.TryCreateDialogueRuntimeArguments(binding.Arguments, descriptor, context, out object[] arguments, out string error))
			{
				Debug.LogError($"[Dialogue] {kind} '{key}'의 인수를 변환하지 못했습니다: {error}");
				return false;
			}

			//메서드 타겟 가져오기
			object target = GetTargetInstance(descriptor, context);
			if (!descriptor.IsStatic && target == null)
			{
				return false;
			}
			try
			{
				//메서드 실행 후 결과 반환
				object methodResult = descriptor.GeneratedInvoker != null ? descriptor.GeneratedInvoker(target, arguments) : descriptor.MethodInfo.Invoke(target, arguments);
				if (kind == MethodKind.Condition)
				{
					if (methodResult is not bool result)
					{
						Debug.LogError($"[Dialogue] 생성된 Condition '{binding.Key}'가 bool 값을 반환하지 않았습니다.");
						return false;
					}
					conditionResult = result;
				}
				return true;
			}
			catch (TargetInvocationException ex)
			{
				Debug.LogError($"[Dialogue] {kind} '{key}' 실행 중 예외가 발생했습니다.\n{ex.InnerException ?? ex}", target as UnityEngine.Object);
				return false;
			}
			catch (Exception exception)
			{
				Debug.LogError($"[Dialogue] {kind} '{key}' 실행 중 예외가 발생했습니다.\n{exception}", target as UnityEngine.Object);
				return false;
			}
		}


		/// <summary>
		/// 주어진 설명서 에 해당하는 메서드를 가지고 있는 Instance를 가져오는 함수
		/// </summary>
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
			//어떤 object가 해당 메서드를 가지고 있는지
            GameObject target = descriptor.Owner == DialogueMethodOwner.Speaker ? context.Speaker : context.Interactor;
            if (target == null)
            {
                Debug.LogWarning($"[Dialogue] DialogueExecutionContext의 대상 '{descriptor.Owner}'이 null입니다.");
                return null;
            }

			//실제 컴포넌트 찾기
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

	}
}






