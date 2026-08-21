using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UniversalGraph
{
	public static class DialogueEventRegistry
	{
		private sealed class GeneratedRegistrationCollector : IDialogueGeneratedMethodSink
		{
			public List<DialogueGeneratedMethodRegistration> Registrations { get; } = new List<DialogueGeneratedMethodRegistration>();


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

		private static readonly Dictionary<string, DialogueMethodDescriptor> actionRegistry = new Dictionary<string, DialogueMethodDescriptor>(StringComparer.Ordinal);

		private static readonly Dictionary<string, DialogueMethodDescriptor> conditionRegistry = new Dictionary<string, DialogueMethodDescriptor>(StringComparer.Ordinal);

		private static readonly HashSet<string> invalidActionKeys = new HashSet<string>(StringComparer.Ordinal);

		private static readonly HashSet<string> invalidConditionKeys = new HashSet<string>(StringComparer.Ordinal);

		private static bool isInitialized;

		[RuntimeInitializeOnLoadMethod]
		private static void ResetStaticState()
		{
			actionRegistry.Clear();
			conditionRegistry.Clear();
			invalidActionKeys.Clear();
			invalidConditionKeys.Clear();
			isInitialized = false;
		}

		[RuntimeInitializeOnLoadMethod]
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
			Debug.Log((object)("[DialogueEventRegistry] ?깅줉 ?꾨즺. " + $"(Action: {actionRegistry.Count}媛? Condition: {conditionRegistry.Count}媛?"));
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
				Debug.LogWarning((object)("[Dialogue] '" + assembly.GetName().Name + "'???앹꽦 Provider瑜??쎌? 紐삵뻽?듬땲?? " + ex.Message));
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
					Debug.LogError((object)("[Dialogue] '" + assembly.GetName().Name + "'???щ컮瑜댁? ?딆? ?앹꽦 Provider媛\u0080 ?덉뒿?덈떎."));
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
					Debug.LogError((object)("[Dialogue] ?앹꽦 Provider '" + type.FullName + "' ?섏쭛???ㅽ뙣?덉뒿?덈떎."));
					Debug.LogException(ex2);
					flag = true;
				}
			}
			List<DialogueMethodDescriptor> list = new List<DialogueMethodDescriptor>();
			foreach (DialogueGeneratedMethodRegistration registration in generatedRegistrationCollector.Registrations)
			{
				if (!DialogueMethodDescriptorFactory.TryCreateGenerated(assembly, registration, out var descriptor, out var error))
				{
					Debug.LogError((object)("[Dialogue] ?앹꽦 硫붿꽌???깅줉 ?ㅽ뙣: " + error));
					flag = true;
				}
				else
				{
					list.Add(descriptor);
				}
			}
			if (flag)
			{
				Debug.LogError((object)("[Dialogue] '" + assembly.GetName().Name + "' ?앹꽦 Provider媛\u0080 ?щ컮瑜댁? ?딆븘 ??assembly??Reflection ?명솚 寃쎈줈濡??ㅼ떆 寃\u0080?됲빀?덈떎."));
				return GeneratedProviderResult.Failed;
			}
			foreach (DialogueMethodDescriptor item in list)
			{
				if (item.Kind == DialogueMethodKind.Action)
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
				Debug.LogWarning((object)("[Dialogue] '" + assembly.GetName().Name + "' ?댁뀍釉붾━瑜?寃\u0080?됲븯吏\u0080 紐삵뻽?듬땲?? " + ex2.Message));
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
					Debug.LogWarning((object)("[Dialogue] '" + type.FullName + "' 硫붿꽌?쒕? 寃\u0080?됲븯吏\u0080 紐삵뻽?듬땲?? " + ex3.Message));
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
			if (string.Equals(name, dialogueAssemblyName, StringComparison.Ordinal))
			{
				return true;
			}
			if (string.Equals(name, "Assembly-CSharp-Editor", StringComparison.Ordinal) || name.EndsWith(".Editor", StringComparison.Ordinal) || name.StartsWith("UnityEditor", StringComparison.Ordinal))
			{
				return false;
			}
			try
			{
				AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
				AssemblyName[] array = referencedAssemblies;
				foreach (AssemblyName assemblyName in array)
				{
					if (string.Equals(assemblyName.Name, dialogueAssemblyName, StringComparison.Ordinal))
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
			if (!DialogueMethodDescriptorFactory.TryCreate(method, DialogueMethodKind.Action, attribute.Key, attribute.Target, out var descriptor, out var error))
			{
				Debug.LogError((object)("[Dialogue] Action ?깅줉 ?ㅽ뙣: " + error));
			}
			else
			{
				RegisterUnique(descriptor, actionRegistry, invalidActionKeys, "Action");
			}
		}

		private static void RegisterCondition(MethodInfo method, DialogueConditionAttribute attribute)
		{
			if (!DialogueMethodDescriptorFactory.TryCreate(method, DialogueMethodKind.Condition, attribute.Key, attribute.Target, out var descriptor, out var error))
			{
				Debug.LogError((object)("[Dialogue] Condition ?깅줉 ?ㅽ뙣: " + error));
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
					Debug.LogError((object)("[Dialogue] 以묐났 " + kind + " Key '" + key + "'瑜??ъ슜?????놁뒿?덈떎: " + value.QualifiedMethodName + ", " + descriptor.QualifiedMethodName));
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
				Debug.LogError((object)"[Dialogue] ?몄뒪?댁뒪 Action/Condition ?ㅽ뻾??DialogueContext媛\u0080 ?꾩슂?⑸땲??");
				return null;
			}
			GameObject val = ((descriptor.Target == DialogueTarget.Speaker) ? context.Speaker : context.Interactor);
			if ((object)val == (object)null)
			{
				Debug.LogWarning((object)$"[Dialogue] {descriptor.Target} ?\u0080?곸씠 議댁옱?섏? ?딆뒿?덈떎.");
				return null;
			}
			Component[] components = val.GetComponents(descriptor.DeclaringType);
			if (components.Length == 0)
			{
				Debug.LogWarning((object)("[Dialogue] '" + ((UnityEngine.Object)val).name + "'??'" + descriptor.DeclaringType?.Name + "' 而댄룷?뚰듃媛\u0080 ?놁뒿?덈떎."), (UnityEngine.Object)val);
				return null;
			}
			if (components.Length > 1)
			{
				Debug.LogError((object)("[Dialogue] '" + ((UnityEngine.Object)val).name + "'??'" + descriptor.DeclaringType?.Name + "' 而댄룷?뚰듃媛\u0080 " + $"{components.Length}媛??덉뼱 ?\u0080?곸쓣 ?섎굹濡?寃곗젙?????놁뒿?덈떎."), (UnityEngine.Object)val);
				return null;
			}
			return components[0];
		}

		public static bool ExecuteAction(string key, string parameter, DialogueContext context)
		{
			return ExecuteAction(key, null, parameter, context);
		}

		public static bool ExecuteAction(string key, IReadOnlyList<DialogueArgumentData> arguments, string legacyStringParameter, DialogueContext context)
		{
			if (string.IsNullOrWhiteSpace(key) || string.Equals(key, "None", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			EnsureInitialized();
			if (!actionRegistry.TryGetValue(key, out var value))
			{
				Debug.LogWarning((object)("[Dialogue] ?깅줉??Action???놁뒿?덈떎: " + key));
				return false;
			}
			if (!DialogueArgumentCodec.TryBuildInvocationArguments(arguments, legacyStringParameter, value, context, out var invocationArguments, out var error))
			{
				Debug.LogError((object)("[Dialogue] Action '" + key + "' ?몄옄瑜?蹂듭썝?섏? 紐삵뻽?듬땲?? " + error));
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

		public static bool TryEvaluateCondition(string key, string parameter, DialogueContext context, out bool result)
		{
			return TryEvaluateCondition(key, null, parameter, context, out result);
		}

		public static bool TryEvaluateCondition(string key, IReadOnlyList<DialogueArgumentData> arguments, string legacyStringParameter, DialogueContext context, out bool result)
		{
			result = false;
			if (string.IsNullOrWhiteSpace(key) || string.Equals(key, "None", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			EnsureInitialized();
			if (!conditionRegistry.TryGetValue(key, out var value))
			{
				Debug.LogWarning((object)("[Dialogue] ?깅줉??Condition???놁뒿?덈떎: " + key));
				return false;
			}
			if (!DialogueArgumentCodec.TryBuildInvocationArguments(arguments, legacyStringParameter, value, context, out var invocationArguments, out var error))
			{
				Debug.LogError((object)("[Dialogue] Condition '" + key + "' ?몄옄瑜?蹂듭썝?섏? 紐삵뻽?듬땲?? " + error));
				return false;
			}
			object targetInstance = GetTargetInstance(value, context);
			if (!value.IsStatic && targetInstance == null)
			{
				return false;
			}
			try
			{
				if (!(((value.GeneratedInvoker != null) ? value.GeneratedInvoker(targetInstance, invocationArguments) : value.Method.Invoke(targetInstance, invocationArguments)) is bool flag) || 1 == 0)
				{
					Debug.LogError((object)("[Dialogue] Condition '" + key + "' ?앹꽦 ?몄텧 寃곌낵媛\u0080 bool???꾨떃?덈떎."));
					return false;
				}
				result = flag;
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
			UnityEngine.Object val = instance as UnityEngine.Object;
			Debug.LogError((object)("[Dialogue] " + kind + " '" + key + "' ?ㅽ뻾 以??덉쇅媛\u0080 諛쒖깮?덉뒿?덈떎."), val);
			Debug.LogException(exception, val);
		}
	}
}






