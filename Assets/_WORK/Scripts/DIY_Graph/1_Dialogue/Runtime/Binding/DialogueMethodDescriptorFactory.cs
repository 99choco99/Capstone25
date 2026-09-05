using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>이미 발견된 Dialogue Attribute 메서드가 사용 가능한지 검사하고<para>
	/// </para> DialogueMethodDescriptor로 변환하는 클래스</summary>
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
			if (registration.Owner != DialogueMethodOwner.Speaker
				&& registration.Owner != DialogueMethodOwner.Interactor
				&& registration.Owner != DialogueMethodOwner.Global)
			{
				error = $"'{key}'의 호출 대상이 올바르지 않습니다.";
				return false;
			}
			if (!TryResolveGeneratedType(
					sourceAssembly,
					registration.DeclaringTypeMetadataName,
					sourceAssembly.GetName().Name,
					out Type declaringType))
			{
				error = $"'{key}'의 선언 타입 '{registration.DeclaringTypeMetadataName}'을 찾을 수 없습니다.";
				return false;
			}
			if (declaringType.ContainsGenericParameters)
			{
				error = $"'{key}'는 닫히지 않은 제네릭 타입에 선언할 수 없습니다.";
				return false;
			}
			if (registration.Owner == DialogueMethodOwner.Global)
			{
				if (!registration.IsStatic)
				{
					error = $"Global 대상 '{key}'는 static 메서드여야 합니다.";
					return false;
				}
			}
			else if (registration.IsStatic || !typeof(Component).IsAssignableFrom(declaringType))
			{
				error = $"{registration.Owner} 대상 '{key}'는 Component의 인스턴스 메서드여야 합니다.";
				return false;
			}
			GeneratedParameterRegistration[] generatedParameters =
				registration.Parameters ?? Array.Empty<GeneratedParameterRegistration>();
			var parameters = new MethodParameterDescriptor[generatedParameters.Length];
			var signatureTypes = new Type[generatedParameters.Length];
			var parameterIds = new HashSet<string>();
			bool hasContext = false;
			for (int index = 0; index < generatedParameters.Length; index++)
			{
				GeneratedParameterRegistration generated = generatedParameters[index];
				if (generated == null
					|| !TryResolveGeneratedType(
						sourceAssembly,
						generated.TypeMetadataName,
						generated.TypeAssemblyName,
						out Type parameterType))
				{
					error = $"'{key}'의 {index}번 파라미터 타입을 찾을 수 없습니다.";
					return false;
				}
				signatureTypes[index] = parameterType;
				string displayName = string.IsNullOrWhiteSpace(generated.DisplayName)
					? $"arg{index}"
					: generated.DisplayName;
				if (parameterType == typeof(DialogueExecutionContext))
				{
					if (hasContext)
					{
						error = $"'{key}'는 DialogueExecutionContext를 한 번만 받을 수 있습니다.";
						return false;
					}
					hasContext = true;
					parameters[index] = new MethodParameterDescriptor(
						index,
						displayName,
						displayName,
						parameterType,
						MethodParameterSource.DialogueExecutionContext,
						MethodArgumentKind.String);
					continue;
				}
				if (!MethodArgumentCodec.TryGetArgumentKind(parameterType, out MethodArgumentKind argumentKind))
				{
					error = $"'{key}'의 파라미터 '{displayName}' 타입 '{parameterType.FullName}'은 그래프 코덱에서 지원하지 않습니다.";
					return false;
				}
				string parameterId = generated.ParameterId;
				if (string.IsNullOrWhiteSpace(parameterId) || !parameterIds.Add(parameterId))
				{
					error = $"'{key}'에 비어 있거나 중복된 파라미터 ID '{parameterId}'가 있습니다.";
					return false;
				}
				var parameter = new MethodParameterDescriptor(
					index,
					parameterId,
					displayName,
					parameterType,
					MethodParameterSource.Serialized,
					argumentKind);
				parameters[index] = parameter;
			}
			MethodInfo method = null;
			if (registration.DirectInvoker == null)
			{
				method = declaringType.GetMethod(
					registration.MethodMetadataName,
					genericParameterCount: 0,
					BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
					BindingFlags.Public | BindingFlags.NonPublic,
					null,
					signatureTypes,
					null);
				if (method == null)
				{
					error = $"'{key}'의 Reflection 대체 메서드를 찾을 수 없습니다: {declaringType.FullName}.{registration.MethodMetadataName}.";
					return false;
				}
				Type expectedReturn = registration.Kind == MethodKind.Action ? typeof(void) : typeof(bool);
				if (method.IsStatic != registration.IsStatic
					|| method.ReturnType != expectedReturn
					|| method.IsAbstract
					|| method.IsGenericMethodDefinition
					|| method.ContainsGenericParameters)
				{
					error = $"'{key}'의 Reflection 시그니처가 생성된 등록 메타데이터와 일치하지 않습니다.";
					return false;
				}
			}
			descriptor = new DialogueMethodDescriptor(
				key,
				registration.Kind,
				registration.Owner,
				declaringType,
				registration.MethodMetadataName,
				registration.IsStatic,
				method,
				parameters,
				registration.DirectInvoker);
			error = null;
			return true;
		}


        /// <summary>
        /// ㄴㅁㅇㄴㅁㄴ
        /// </summary>
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
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
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



		//===================================== Reflection 방식 ==============================================


        /// <summary>Reflection으로 찾은 Attribute 메서드 하나에 대해서 Descriptor 제작</summary>
        public static bool TryCreateFromReflection(MethodInfo method, MethodKind kind, string key, DialogueMethodOwner owner, out DialogueMethodDescriptor descriptor, out string error)
		{
			descriptor = null;
			if (method == null)
			{
				error = "MethodInfo가 null입니다.";
				return false;
			}
			if (kind != MethodKind.Action && kind != MethodKind.Condition)
			{
				error = "메서드 종류가 올바르지 않습니다.";
				return false;
			}
			if (owner != DialogueMethodOwner.Speaker && owner != DialogueMethodOwner.Interactor && owner != DialogueMethodOwner.Global)
			{
				error = $"'{key}'의 호출 대상이 올바르지 않습니다.";
				return false;
			}

			//메서드 이름 설정(오류검출용)
			string name = method.DeclaringType?.FullName + "." + method.Name;
			if (string.IsNullOrWhiteSpace(key) || key == "None")
			{
				error = $"'{name}'에 빈 키가 있거나 예약 값 'None'을 사용했습니다.";
				return false;
			}

			//action 메서드인가 or condition 메서드인가?
			Type expectedReturn = kind == MethodKind.Action ? typeof(void) : typeof(bool);
			if (method.ReturnType != expectedReturn)
			{
				error = $"'{key}' ({name})는 {expectedReturn.Name} 타입을 반환해야 합니다.";
				return false;
			}
			if (method.IsAbstract || method.IsSpecialName || method.IsGenericMethodDefinition || method.ContainsGenericParameters || method.DeclaringType == null || method.DeclaringType.ContainsGenericParameters)
			{
				error = $"'{key}' ({name})는 구체적인 타입에 선언된 제네릭이 아닌 구체적인 메서드여야 합니다.";
				return false;
			}
			if ((method.CallingConvention & CallingConventions.VarArgs) != 0 || method.IsDefined(typeof(ExtensionAttribute), inherit: false))
			{
				error = $"'{key}' ({name})는 가변 인수 또는 확장 메서드일 수 없습니다.";
				return false;
			}
			if (method.GetCustomAttribute<AsyncStateMachineAttribute>(inherit: false) != null)
			{
				error = $"'{key}' ({name})는 async 메서드일 수 없습니다.";
				return false;
			}
			if (owner == DialogueMethodOwner.Global)
			{
				if (!method.IsStatic)
				{
					error = $"Global 대상 '{key}' ({name})는 static 메서드여야 합니다.";
					return false;
				}
			}
			else if (method.IsStatic || !typeof(Component).IsAssignableFrom(method.DeclaringType))
			{
				error = $"{owner} 대상 '{key}' ({name})는 Component의 인스턴스 메서드여야 합니다.";
				return false;
			}


			//모든 파라미터 가져오기
			ParameterInfo[] methodParameters = method.GetParameters();
			var parameters = new MethodParameterDescriptor[methodParameters.Length];

			int serializedParameterCount = 0;
			bool hasContext = false;
			for (int index = 0; index < methodParameters.Length; index++)
			{
				ParameterInfo parameter = methodParameters[index];

				Type parameterType = parameter.ParameterType;
				string displayName = parameter.Name ?? $"arg{index}";

				if (parameterType.IsByRef || parameter.IsOut || parameter.IsIn)
				{
					error = $"'{key}' ({name})의 파라미터 '{displayName}'에는 ref, out, in을 사용할 수 없습니다.";
					return false;
				}
				if (parameter.IsOptional || parameter.GetCustomAttribute<ParamArrayAttribute>(inherit: false) != null)
				{
					error = $"'{key}' ({name})의 파라미터 '{displayName}'는 선택적 파라미터 또는 params일 수 없습니다.";
					return false;
				}

				//context가 있을 때
				if (parameterType == typeof(DialogueExecutionContext))
				{
					if (hasContext)
					{
						error = $"'{key}' ({name})는 DialogueExecutionContext를 한 번만 받을 수 있습니다.";
						return false;
					}
					hasContext = true;
					parameters[index] = new MethodParameterDescriptor(
						index,
						displayName,
						displayName,
						parameterType,
						MethodParameterSource.DialogueExecutionContext,
						MethodArgumentKind.String);
					continue;
				}

				//인수의 타입을 결정
				if (!MethodArgumentCodec.TryGetArgumentKind(parameterType, out MethodArgumentKind argumentKind))
				{
					error = $"'{key}' ({name})의 파라미터 '{displayName}' 타입 '{parameterType.FullName}'은 그래프 코덱에서 지원하지 않습니다.";
					return false;
				}

				//파라미터 설명서 완성
				string parameterId = $"arg{serializedParameterCount++}";
                MethodParameterDescriptor descriptorParameter = new (
					index,
					parameterId,
					displayName,
					parameterType,
					MethodParameterSource.Serialized,
					argumentKind);

				parameters[index] = descriptorParameter;
			}

			descriptor = new DialogueMethodDescriptor(key, kind, owner, method, parameters);

			error = null;
			return true;
		}
	}
}
