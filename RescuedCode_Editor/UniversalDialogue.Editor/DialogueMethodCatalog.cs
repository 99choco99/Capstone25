using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Scripting;

namespace UniversalDialogue.Editor
{
	[Preserve]
	internal static class DialogueMethodCatalog
	{
		private static readonly List<DialogueMethodDescriptor> actions;

		private static readonly List<DialogueMethodDescriptor> conditions;

		private static readonly Dictionary<string, DialogueMethodDescriptor> actionByKey;

		private static readonly Dictionary<string, DialogueMethodDescriptor> conditionByKey;

		public static IReadOnlyList<DialogueMethodDescriptor> Actions => actions;

		public static IReadOnlyList<DialogueMethodDescriptor> Conditions => conditions;

		static DialogueMethodCatalog()
		{
			actions = new List<DialogueMethodDescriptor>();
			conditions = new List<DialogueMethodDescriptor>();
			actionByKey = new Dictionary<string, DialogueMethodDescriptor>(StringComparer.Ordinal);
			conditionByKey = new Dictionary<string, DialogueMethodDescriptor>(StringComparer.Ordinal);
			BuildRegistry();
		}

		public static bool TryGetAction(string key, out DialogueMethodDescriptor descriptor)
		{
			return TryGet(actionByKey, key, out descriptor);
		}

		public static bool TryGetCondition(string key, out DialogueMethodDescriptor descriptor)
		{
			return TryGet(conditionByKey, key, out descriptor);
		}

		public static IReadOnlyList<DialogueMethodDescriptor> GetMethods(DialogueMethodKind kind)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return ((int)kind == 0) ? Actions : Conditions;
		}

		public static bool TryGetMethod(DialogueMethodKind kind, string key, out DialogueMethodDescriptor descriptor)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return ((int)kind == 0) ? TryGetAction(key, out descriptor) : TryGetCondition(key, out descriptor);
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

		private static void BuildRegistry()
		{
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Unknown result type (might be due to invalid IL or missing references)
			actions.Clear();
			conditions.Clear();
			actionByKey.Clear();
			conditionByKey.Clear();
			Dictionary<string, List<DialogueMethodDescriptor>> dictionary = new Dictionary<string, List<DialogueMethodDescriptor>>(StringComparer.Ordinal);
			Dictionary<string, List<DialogueMethodDescriptor>> dictionary2 = new Dictionary<string, List<DialogueMethodDescriptor>>(StringComparer.Ordinal);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			Assembly[] assemblies = CompilationPipeline.GetAssemblies((AssembliesType)1);
			foreach (Assembly val in assemblies)
			{
				hashSet.Add(val.name);
			}
			MethodCollection methodsWithAttribute = TypeCache.GetMethodsWithAttribute<DialogueActionAttribute>();
			Enumerator enumerator = ((MethodCollection)(ref methodsWithAttribute)).GetEnumerator();
			try
			{
				while (((Enumerator)(ref enumerator)).MoveNext())
				{
					MethodInfo current = ((Enumerator)(ref enumerator)).Current;
					DialogueActionAttribute customAttribute = ((MemberInfo)current).GetCustomAttribute<DialogueActionAttribute>(inherit: false);
					if (customAttribute != null && IsPlayerMethod(current, hashSet, "Action"))
					{
						AddCandidate(current, (DialogueMethodKind)0, customAttribute.Key, customAttribute.Target, dictionary);
					}
				}
			}
			finally
			{
				((IDisposable)(Enumerator)(ref enumerator)).Dispose();
			}
			methodsWithAttribute = TypeCache.GetMethodsWithAttribute<DialogueConditionAttribute>();
			Enumerator enumerator2 = ((MethodCollection)(ref methodsWithAttribute)).GetEnumerator();
			try
			{
				while (((Enumerator)(ref enumerator2)).MoveNext())
				{
					MethodInfo current2 = ((Enumerator)(ref enumerator2)).Current;
					DialogueConditionAttribute customAttribute2 = ((MemberInfo)current2).GetCustomAttribute<DialogueConditionAttribute>(inherit: false);
					if (customAttribute2 != null && IsPlayerMethod(current2, hashSet, "Condition"))
					{
						AddCandidate(current2, (DialogueMethodKind)1, customAttribute2.Key, customAttribute2.Target, dictionary2);
					}
				}
			}
			finally
			{
				((IDisposable)(Enumerator)(ref enumerator2)).Dispose();
			}
			PublishUnique(dictionary, actions, actionByKey, "Action");
			PublishUnique(dictionary2, conditions, conditionByKey, "Condition");
		}

		private static bool IsPlayerMethod(MethodInfo method, HashSet<string> playerAssemblyNames, string kind)
		{
			string text = method.DeclaringType?.Assembly.GetName().Name;
			if (!string.IsNullOrEmpty(text) && playerAssemblyNames.Contains(text))
			{
				return true;
			}
			Debug.LogWarning((object)("[Dialogue] Editor ?꾩슜 " + kind + "?\u0080 洹몃옒?꾩뿉???ъ슜?????놁뒿?덈떎: " + method.DeclaringType?.FullName + "." + method.Name));
			return false;
		}

		private static void AddCandidate(MethodInfo method, DialogueMethodKind kind, string key, DialogueTarget target, Dictionary<string, List<DialogueMethodDescriptor>> entriesByKey)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			DialogueMethodDescriptor val = default(DialogueMethodDescriptor);
			string arg = default(string);
			if (!DialogueMethodDescriptorFactory.TryCreate(method, kind, key, target, ref val, ref arg))
			{
				Debug.LogError((object)$"[Dialogue] {kind} ?깅줉 ?ㅽ뙣: {arg}");
				return;
			}
			if (!entriesByKey.TryGetValue(val.Key, out var value))
			{
				value = new List<DialogueMethodDescriptor>();
				entriesByKey.Add(val.Key, value);
			}
			value.Add(val);
		}

		private static void PublishUnique(Dictionary<string, List<DialogueMethodDescriptor>> candidates, List<DialogueMethodDescriptor> published, Dictionary<string, DialogueMethodDescriptor> publishedByKey, string kind)
		{
			foreach (KeyValuePair<string, List<DialogueMethodDescriptor>> candidate in candidates)
			{
				if (candidate.Value.Count != 1)
				{
					Debug.LogError((object)("[Dialogue] 以묐났 " + kind + " Key '" + candidate.Key + "'??洹몃옒?꾩뿉???좏깮?????놁뒿?덈떎."));
				}
				else
				{
					DialogueMethodDescriptor val = candidate.Value[0];
					published.Add(val);
					publishedByKey.Add(val.Key, val);
				}
			}
			published.Sort((DialogueMethodDescriptor left, DialogueMethodDescriptor right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
		}
	}
}
