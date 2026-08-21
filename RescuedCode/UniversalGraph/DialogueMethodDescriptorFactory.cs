using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UniversalGraph
{
	public static class DialogueMethodDescriptorFactory
	{
		internal static bool TryCreateGenerated(Assembly sourceAssembly, DialogueGeneratedMethodRegistration registration, out DialogueMethodDescriptor descriptor, out string error)
		{
			descriptor = null;
			if (sourceAssembly == null || registration == null)
			{
				error = "Generated method??assembly ?먮뒗 registration???놁뒿?덈떎.";
				return false;
			}
			string key = registration.Key;
			if (string.IsNullOrWhiteSpace(key) || string.Equals(key, "None", StringComparison.OrdinalIgnoreCase))
			{
				error = "Generated method Key媛\u0080 鍮꾩뼱 ?덇굅???덉빟??None?낅땲??";
				return false;
			}
			if (string.IsNullOrWhiteSpace(registration.MethodMetadataName))
			{
				error = "'" + key + "'??method metadata ?대쫫??鍮꾩뼱 ?덉뒿?덈떎.";
				return false;
			}
			if (registration.Kind != 0 && registration.Kind != DialogueMethodKind.Condition)
			{
				error = "'" + key + "'??method kind媛\u0080 ?щ컮瑜댁? ?딆뒿?덈떎.";
				return false;
			}
			if (registration.Target != 0 && registration.Target != DialogueTarget.Interactor && registration.Target != DialogueTarget.Global)
			{
				error = "'" + key + "'??target???щ컮瑜댁? ?딆뒿?덈떎.";
				return false;
			}
			if (!TryResolveGeneratedType(sourceAssembly, registration.DeclaringTypeMetadataName, sourceAssembly.GetName().Name, out var type))
			{
				error = "'" + key + "'???좎뼵 ?\u0080??'" + registration.DeclaringTypeMetadataName + "'??李얠쓣 ???놁뒿?덈떎.";
				return false;
			}
			if (type.ContainsGenericParameters)
			{
				error = "'" + key + "'???좎뼵 ?\u0080?낆? open generic?????놁뒿?덈떎.";
				return false;
			}
			if (registration.Target == DialogueTarget.Global)
			{
				if (!registration.IsStatic)
				{
					error = "Global ?\u0080??'" + key + "'??static 硫붿꽌?쒖뿬???⑸땲??";
					return false;
				}
			}
			else if (registration.IsStatic || !typeof(Component).IsAssignableFrom(type))
			{
				error = $"{registration.Target} ?\u0080??'{key}'??Component??" + "?몄뒪?댁뒪 硫붿꽌?쒖뿬???⑸땲??";
				return false;
			}
			DialogueGeneratedParameterRegistration[] array = registration.Parameters ?? Array.Empty<DialogueGeneratedParameterRegistration>();
			DialogueParameterDescriptor[] array2 = new DialogueParameterDescriptor[array.Length];
			List<DialogueParameterDescriptor> list = new List<DialogueParameterDescriptor>();
			Type[] array3 = new Type[array.Length];
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			bool flag = false;
			for (int i = 0; i < array.Length; i++)
			{
				DialogueGeneratedParameterRegistration dialogueGeneratedParameterRegistration = array[i];
				if (dialogueGeneratedParameterRegistration == null || !TryResolveGeneratedType(sourceAssembly, dialogueGeneratedParameterRegistration.TypeMetadataName, dialogueGeneratedParameterRegistration.TypeAssemblyName, out var type2))
				{
					error = $"'{key}'??{i}踰??뚮씪誘명꽣 ?\u0080?낆쓣 李얠쓣 ???놁뒿?덈떎.";
					return false;
				}
				array3[i] = type2;
				string text = (string.IsNullOrWhiteSpace(dialogueGeneratedParameterRegistration.DisplayName) ? $"arg{i}" : dialogueGeneratedParameterRegistration.DisplayName);
				if (type2 == typeof(DialogueContext))
				{
					if (flag)
					{
						error = "'" + key + "'??DialogueContext瑜???踰?諛쏆쓣 ???놁뒿?덈떎.";
						return false;
					}
					flag = true;
					array2[i] = new DialogueParameterDescriptor(i, text, text, type2, DialogueParameterSource.DialogueContext, DialogueArgumentKind.String, GetStableTypeId(type2));
					continue;
				}
				if (!DialogueArgumentCodec.TryGetKind(type2, out var kind))
				{
					error = "'" + key + "'??'" + text + "' ?\u0080??'" + type2.FullName + "'?\u0080 洹몃옒?꾩뿉???\u0080?ν븷 ???놁뒿?덈떎.";
					return false;
				}
				string parameterId = dialogueGeneratedParameterRegistration.ParameterId;
				if (string.IsNullOrWhiteSpace(parameterId) || !hashSet.Add(parameterId))
				{
					error = "'" + key + "'??鍮?媛??먮뒗 以묐났 Parameter ID '" + parameterId + "'媛\u0080 ?덉뒿?덈떎.";
					return false;
				}
				list.Add(array2[i] = new DialogueParameterDescriptor(i, parameterId, text, type2, DialogueParameterSource.Serialized, kind, GetStableTypeId(type2)));
			}
			MethodInfo methodInfo = null;
			if (registration.DirectInvoker == null)
			{
				methodInfo = type.GetMethod(registration.MethodMetadataName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, array3, null);
				if (methodInfo == null)
				{
					error = "'" + key + "'??蹂댁〈??Reflection 硫붿꽌?쒕? 李얠쓣 ???놁뒿?덈떎: " + type.FullName + "." + registration.MethodMetadataName;
					return false;
				}
				Type type3 = ((registration.Kind == DialogueMethodKind.Action) ? typeof(void) : typeof(bool));
				if (methodInfo.IsStatic != registration.IsStatic || methodInfo.ReturnType != type3 || methodInfo.IsAbstract || methodInfo.IsGenericMethodDefinition || methodInfo.ContainsGenericParameters)
				{
					error = "'" + key + "'??Reflection 硫붿꽌???쒓렇?덉쿂媛\u0080 ?앹꽦 ?뺣낫?\u0080 ?ㅻ쫭?덈떎.";
					return false;
				}
			}
			descriptor = new DialogueMethodDescriptor(key, registration.Kind, registration.Target, type, registration.MethodMetadataName, registration.IsStatic, methodInfo, array2, list.ToArray(), registration.DirectInvoker);
			error = null;
			return true;
		}

		public static bool TryCreate(MethodInfo method, DialogueMethodKind kind, string key, DialogueTarget target, out DialogueMethodDescriptor descriptor, out string error)
		{
			descriptor = null;
			if (method == null)
			{
				error = "MethodInfo媛\u0080 null?낅땲??";
				return false;
			}
			string text = method.DeclaringType?.FullName + "." + method.Name;
			if (string.IsNullOrWhiteSpace(key) || string.Equals(key, "None", StringComparison.OrdinalIgnoreCase))
			{
				error = "'" + text + "'??Key媛\u0080 鍮꾩뼱 ?덇굅???덉빟??None?낅땲??";
				return false;
			}
			Type type = ((kind == DialogueMethodKind.Action) ? typeof(void) : typeof(bool));
			if (method.ReturnType != type)
			{
				error = "'" + key + "' (" + text + ")??諛섑솚?뺤? " + type.Name + "?댁뼱???⑸땲??";
				return false;
			}
			if (method.IsAbstract || method.IsSpecialName || method.IsGenericMethodDefinition || method.ContainsGenericParameters || method.DeclaringType == null || method.DeclaringType.ContainsGenericParameters)
			{
				error = "'" + key + "' (" + text + ")??鍮꾩텛?겶룸퉬?쒕꽕由??쇰컲 硫붿꽌?쒖뿬???⑸땲??";
				return false;
			}
			if ((method.CallingConvention & CallingConventions.VarArgs) != 0 || method.IsDefined(typeof(ExtensionAttribute), inherit: false))
			{
				error = "'" + key + "' (" + text + ")??varargs/extension 硫붿꽌?쒖씪 ???놁뒿?덈떎.";
				return false;
			}
			if (method.GetCustomAttribute<AsyncStateMachineAttribute>(inherit: false) != null)
			{
				error = "'" + key + "' (" + text + ")??async 硫붿꽌?쒖씪 ???놁뒿?덈떎.";
				return false;
			}
			if (target == DialogueTarget.Global)
			{
				if (!method.IsStatic)
				{
					error = "Global ?\u0080??'" + key + "' (" + text + ")??static 硫붿꽌?쒖뿬???⑸땲??";
					return false;
				}
			}
			else if (method.IsStatic || !typeof(Component).IsAssignableFrom(method.DeclaringType))
			{
				error = $"{target} ?\u0080??'{key}' ({text})??Component???몄뒪?댁뒪 硫붿꽌?쒖뿬???⑸땲??";
				return false;
			}
			ParameterInfo[] parameters = method.GetParameters();
			DialogueParameterDescriptor[] array = new DialogueParameterDescriptor[parameters.Length];
			List<DialogueParameterDescriptor> list = new List<DialogueParameterDescriptor>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			bool flag = false;
			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterInfo parameterInfo = parameters[i];
				Type parameterType = parameterInfo.ParameterType;
				string text2 = parameterInfo.Name ?? $"arg{i}";
				if (parameterType.IsByRef || parameterInfo.IsOut || parameterInfo.IsIn)
				{
					error = "'" + key + "' (" + text + ")??'" + text2 + "'?\u0080 ref/out/in?????놁뒿?덈떎.";
					return false;
				}
				if (parameterInfo.IsOptional || parameterInfo.GetCustomAttribute<ParamArrayAttribute>(inherit: false) != null)
				{
					error = "'" + key + "' (" + text + ")??'" + text2 + "'?\u0080 optional/params?????놁뒿?덈떎.";
					return false;
				}
				if (parameterType == typeof(DialogueContext))
				{
					if (flag)
					{
						error = "'" + key + "' (" + text + ")??DialogueContext瑜???踰?諛쏆쓣 ???놁뒿?덈떎.";
						return false;
					}
					flag = true;
					array[i] = new DialogueParameterDescriptor(i, text2, text2, parameterType, DialogueParameterSource.DialogueContext, DialogueArgumentKind.String, GetStableTypeId(parameterType));
					continue;
				}
				if (!DialogueArgumentCodec.TryGetKind(parameterType, out var kind2))
				{
					error = "'" + key + "' (" + text + ")??'" + text2 + "' ?\u0080??'" + parameterType.FullName + "'?\u0080 ?꾩쭅 洹몃옒?꾩뿉???\u0080?ν븷 ???놁뒿?덈떎.";
					return false;
				}
				string text3 = parameterInfo.GetCustomAttribute<DialogueParameterAttribute>(inherit: false)?.Id ?? text2;
				if (string.IsNullOrWhiteSpace(text3))
				{
					error = "'" + key + "' (" + text + ")??'" + text2 + "' Parameter ID媛\u0080 鍮꾩뼱 ?덉뒿?덈떎.";
					return false;
				}
				if (!hashSet.Add(text3))
				{
					error = "'" + key + "' (" + text + ")??以묐났 Parameter ID '" + text3 + "'媛\u0080 ?덉뒿?덈떎.";
					return false;
				}
				list.Add(array[i] = new DialogueParameterDescriptor(i, text3, text2, parameterType, DialogueParameterSource.Serialized, kind2, GetStableTypeId(parameterType)));
			}
			descriptor = new DialogueMethodDescriptor(key, kind, target, method, array, list.ToArray());
			error = null;
			return true;
		}

		public static string GetStableTypeId(Type type)
		{
			if (type == null)
			{
				return string.Empty;
			}
			return type.FullName + ", " + type.Assembly.GetName().Name;
		}

		private static bool TryResolveGeneratedType(Assembly sourceAssembly, string typeMetadataName, string typeAssemblyName, out Type type)
		{
			type = null;
			if (string.IsNullOrWhiteSpace(typeMetadataName) || string.IsNullOrWhiteSpace(typeAssemblyName))
			{
				return false;
			}
			if (string.Equals(sourceAssembly.GetName().Name, typeAssemblyName, StringComparison.Ordinal))
			{
				type = sourceAssembly.GetType(typeMetadataName, throwOnError: false, ignoreCase: false);
				return type != null;
			}
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Assembly[] array = assemblies;
			foreach (Assembly assembly in array)
			{
				if (string.Equals(assembly.GetName().Name, typeAssemblyName, StringComparison.Ordinal))
				{
					type = assembly.GetType(typeMetadataName, throwOnError: false, ignoreCase: false);
					if (type != null)
					{
						return true;
					}
				}
			}
			type = Type.GetType(typeMetadataName + ", " + typeAssemblyName, throwOnError: false, ignoreCase: false);
			return type != null;
		}
	}
}
