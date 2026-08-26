using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>Attribute 메서드의 시그니처를 검증하고 런타임 설명 정보로 변환합니다.</summary>
	public static class DialogueMethodDescriptorFactory
	{
		/// <summary>생성된 메타데이터로 런타임 설명 정보를 만들고 필요하면 Reflection 대체 경로를 찾습니다.</summary>
		internal static bool TryCreateGenerated(Assembly sourceAssembly, DialogueGeneratedMethodRegistration registration, out DialogueMethodDescriptor descriptor, out string error)
		{
			descriptor = null;
			if (sourceAssembly == null || registration == null)
			{
				error = "생성된 메서드의 어셈블리 또는 등록 정보가 null입니다.";
				return false;
			}
			string key = registration.Key;
			if (string.IsNullOrWhiteSpace(key) || key == "None")
			{
				error = "생성된 메서드 키가 비어 있거나 예약 값 'None'입니다.";
				return false;
			}
			if (string.IsNullOrWhiteSpace(registration.MethodMetadataName))
			{
				error = $"'{key}'에 메서드 메타데이터 이름이 없습니다.";
				return false;
			}
			if (registration.Kind != MethodKind.Action && registration.Kind != MethodKind.Condition)
			{
				error = $"'{key}'의 메서드 종류가 올바르지 않습니다.";
				return false;
			}
			if (registration.Target != DialogueTarget.Speaker
				&& registration.Target != DialogueTarget.Interactor
				&& registration.Target != DialogueTarget.Global)
			{
				error = $"'{key}'의 호출 대상이 올바르지 않습니다.";
				return false;
			}
			if (!TryResolveGeneratedType(sourceAssembly, registration.DeclaringTypeMetadataName, sourceAssembly.GetName().Name, out var type))
			{
				error = $"'{key}'의 선언 타입 '{registration.DeclaringTypeMetadataName}'을 찾을 수 없습니다.";
				return false;
			}
			if (type.ContainsGenericParameters)
			{
				error = $"'{key}'는 닫히지 않은 제네릭 타입에 선언할 수 없습니다.";
				return false;
			}
			if (registration.Target == DialogueTarget.Global)
			{
				if (!registration.IsStatic)
				{
					error = $"Global 대상 '{key}'는 static 메서드여야 합니다.";
					return false;
				}
			}
			else if (registration.IsStatic || !typeof(Component).IsAssignableFrom(type))
			{
				error = $"{registration.Target} 대상 '{key}'는 Component의 인스턴스 메서드여야 합니다.";
				return false;
			}
			GeneratedParameterRegistration[] array = registration.Parameters ?? Array.Empty<GeneratedParameterRegistration>();
			MethodParameterDescriptor[] array2 = new MethodParameterDescriptor[array.Length];
			List<MethodParameterDescriptor> list = new List<MethodParameterDescriptor>();
			Type[] array3 = new Type[array.Length];
			HashSet<string> hashSet = new HashSet<string>();
			bool flag = false;
			for (int i = 0; i < array.Length; i++)
			{
				GeneratedParameterRegistration generatedParameter = array[i];
				if (generatedParameter == null || !TryResolveGeneratedType(sourceAssembly, generatedParameter.TypeMetadataName, generatedParameter.TypeAssemblyName, out var type2))
				{
					error = $"'{key}'의 {i}번 파라미터 타입을 찾을 수 없습니다.";
					return false;
				}
				array3[i] = type2;
				string text = (string.IsNullOrWhiteSpace(generatedParameter.DisplayName) ? $"arg{i}" : generatedParameter.DisplayName);
				if (type2 == typeof(DialogueContext))
				{
					if (flag)
					{
						error = $"'{key}'는 DialogueContext를 한 번만 받을 수 있습니다.";
						return false;
					}
					flag = true;
					array2[i] = new MethodParameterDescriptor(i, text, text, type2, MethodParameterSource.DialogueContext, MethodArgumentKind.String, MethodTypeUtility.GetStableTypeId(type2));
					continue;
				}
				if (!MethodArgumentCodec.TryGetKind(type2, out var kind))
				{
					error = $"'{key}'의 파라미터 '{text}' 타입 '{type2.FullName}'은 그래프 코덱에서 지원하지 않습니다.";
					return false;
				}
				string parameterId = generatedParameter.ParameterId;
				if (string.IsNullOrWhiteSpace(parameterId) || !hashSet.Add(parameterId))
				{
					error = $"'{key}'에 비어 있거나 중복된 파라미터 ID '{parameterId}'가 있습니다.";
					return false;
				}
				list.Add(array2[i] = new MethodParameterDescriptor(i, parameterId, text, type2, MethodParameterSource.Serialized, kind, MethodTypeUtility.GetStableTypeId(type2)));
			}
			MethodInfo methodInfo = null;
			if (registration.DirectInvoker == null)
			{
				methodInfo = type.GetMethod(registration.MethodMetadataName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, array3, null);
				if (methodInfo == null)
				{
					error = $"'{key}'의 Reflection 대체 메서드를 찾을 수 없습니다: {type.FullName}.{registration.MethodMetadataName}.";
					return false;
				}
				Type type3 = ((registration.Kind == MethodKind.Action) ? typeof(void) : typeof(bool));
				if (methodInfo.IsStatic != registration.IsStatic || methodInfo.ReturnType != type3 || methodInfo.IsAbstract || methodInfo.IsGenericMethodDefinition || methodInfo.ContainsGenericParameters)
				{
					error = $"'{key}'의 Reflection 시그니처가 생성된 등록 메타데이터와 일치하지 않습니다.";
					return false;
				}
			}
			descriptor = new DialogueMethodDescriptor(key, registration.Kind, registration.Target, type, registration.MethodMetadataName, registration.IsStatic, methodInfo, array2, list.ToArray(), registration.DirectInvoker);
			error = null;
			return true;
		}

		/// <summary>Reflection으로 찾은 Attribute 메서드 하나에서 설명 정보를 만듭니다.</summary>
		public static bool TryCreate(MethodInfo method, MethodKind kind, string key, DialogueTarget target, out DialogueMethodDescriptor descriptor, out string error)
		{
			descriptor = null;
			if (method == null)
			{
				error = "MethodInfo가 null입니다.";
				return false;
			}
			string text = method.DeclaringType?.FullName + "." + method.Name;
			if (string.IsNullOrWhiteSpace(key) || key == "None")
			{
				error = $"'{text}'에 빈 키가 있거나 예약 값 'None'을 사용했습니다.";
				return false;
			}
			Type type = ((kind == MethodKind.Action) ? typeof(void) : typeof(bool));
			if (method.ReturnType != type)
			{
				error = $"'{key}' ({text})는 {type.Name} 타입을 반환해야 합니다.";
				return false;
			}
			if (method.IsAbstract || method.IsSpecialName || method.IsGenericMethodDefinition || method.ContainsGenericParameters || method.DeclaringType == null || method.DeclaringType.ContainsGenericParameters)
			{
				error = $"'{key}' ({text})는 구체적인 타입에 선언된 제네릭이 아닌 구체적인 메서드여야 합니다.";
				return false;
			}
			if ((method.CallingConvention & CallingConventions.VarArgs) != 0 || method.IsDefined(typeof(ExtensionAttribute), inherit: false))
			{
				error = $"'{key}' ({text})는 가변 인수 또는 확장 메서드일 수 없습니다.";
				return false;
			}
			if (method.GetCustomAttribute<AsyncStateMachineAttribute>(inherit: false) != null)
			{
				error = $"'{key}' ({text})는 async 메서드일 수 없습니다.";
				return false;
			}
			if (target == DialogueTarget.Global)
			{
				if (!method.IsStatic)
				{
					error = $"Global 대상 '{key}' ({text})는 static 메서드여야 합니다.";
					return false;
				}
			}
			else if (method.IsStatic || !typeof(Component).IsAssignableFrom(method.DeclaringType))
			{
				error = $"{target} 대상 '{key}' ({text})는 Component의 인스턴스 메서드여야 합니다.";
				return false;
			}
			ParameterInfo[] parameters = method.GetParameters();
			MethodParameterDescriptor[] array = new MethodParameterDescriptor[parameters.Length];
			List<MethodParameterDescriptor> list = new List<MethodParameterDescriptor>();
			HashSet<string> hashSet = new HashSet<string>();
			bool flag = false;
			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterInfo parameterInfo = parameters[i];
				Type parameterType = parameterInfo.ParameterType;
				string text2 = parameterInfo.Name ?? $"arg{i}";
				if (parameterType.IsByRef || parameterInfo.IsOut || parameterInfo.IsIn)
				{
					error = $"'{key}' ({text})의 파라미터 '{text2}'에는 ref, out, in을 사용할 수 없습니다.";
					return false;
				}
				if (parameterInfo.IsOptional || parameterInfo.GetCustomAttribute<ParamArrayAttribute>(inherit: false) != null)
				{
					error = $"'{key}' ({text})의 파라미터 '{text2}'는 선택적 파라미터 또는 params일 수 없습니다.";
					return false;
				}
				if (parameterType == typeof(DialogueContext))
				{
					if (flag)
					{
						error = $"'{key}' ({text})는 DialogueContext를 한 번만 받을 수 있습니다.";
						return false;
					}
					flag = true;
					array[i] = new MethodParameterDescriptor(i, text2, text2, parameterType, MethodParameterSource.DialogueContext, MethodArgumentKind.String, MethodTypeUtility.GetStableTypeId(parameterType));
					continue;
				}
				if (!MethodArgumentCodec.TryGetKind(parameterType, out var kind2))
				{
					error = $"'{key}' ({text})의 파라미터 '{text2}' 타입 '{parameterType.FullName}'은 그래프 코덱에서 지원하지 않습니다.";
					return false;
				}
				string text3 = parameterInfo.GetCustomAttribute<DialogueParameterAttribute>(inherit: false)?.Id ?? text2;
				if (string.IsNullOrWhiteSpace(text3))
				{
					error = $"'{key}' ({text})의 파라미터 '{text2}'에 파라미터 ID가 없습니다.";
					return false;
				}
				if (!hashSet.Add(text3))
				{
					error = $"'{key}' ({text})에 중복된 파라미터 ID '{text3}'가 있습니다.";
					return false;
				}
				list.Add(array[i] = new MethodParameterDescriptor(i, text3, text2, parameterType, MethodParameterSource.Serialized, kind2, MethodTypeUtility.GetStableTypeId(parameterType)));
			}
			descriptor = new DialogueMethodDescriptor(key, kind, target, method, array, list.ToArray());
			error = null;
			return true;
		}

		private static bool TryResolveGeneratedType(Assembly sourceAssembly, string typeMetadataName, string typeAssemblyName, out Type type)
		{
			type = null;
			if (string.IsNullOrWhiteSpace(typeMetadataName) || string.IsNullOrWhiteSpace(typeAssemblyName))
			{
				return false;
			}
			if (sourceAssembly.GetName().Name == typeAssemblyName)
			{
				type = sourceAssembly.GetType(typeMetadataName, throwOnError: false, ignoreCase: false);
				return type != null;
			}
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Assembly[] array = assemblies;
			foreach (Assembly assembly in array)
			{
				if (assembly.GetName().Name == typeAssemblyName)
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
