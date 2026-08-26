using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>
	/// Dialogue와 Quest에서 편집 가능한 메서드 인수를 직렬화 형태로 변환하거나 원래 타입으로 복원합니다.
	/// </summary>
	public static class MethodArgumentCodec
	{
		private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

		/// <summary>메서드 인수 타입을 저장할 수 있는 종류로 변환해 반환합니다.</summary>
		public static bool TryGetKind(Type type, out MethodArgumentKind kind)
		{
			if (type == typeof(string))
			{
				kind = MethodArgumentKind.String;
				return true;
			}
			if (type == typeof(bool))
			{
				kind = MethodArgumentKind.Boolean;
				return true;
			}
			if (type != null && type.IsEnum)
			{
				kind = MethodArgumentKind.Enum;
				return true;
			}
			if (type != null && typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				kind = MethodArgumentKind.UnityObject;
				return true;
			}
			if (type == typeof(int))
			{
				kind = MethodArgumentKind.Integer;
				return true;
			}
			if (type == typeof(float))
			{
				kind = MethodArgumentKind.FloatingPoint;
				return true;
			}
			kind = MethodArgumentKind.String;
			return false;
		}

		/// <summary>메서드 설명 정보에 맞는 형태의 기본 인수 값을 만듭니다.</summary>
		public static List<MethodArgumentData> CreateDefaultArguments(MethodDescriptor descriptor)
		{
			List<MethodArgumentData> list = new List<MethodArgumentData>();
			if (descriptor == null)
			{
				return list;
			}
			foreach (MethodParameterDescriptor serializedParameter in descriptor.SerializedParameters)
			{
				list.Add(CreateDefaultArgument(serializedParameter));
			}
			return list;
		}

		/// <summary>메서드 시그니처 변경에 맞춰 인수를 다시 만들고, 호환되는 기존 값은 선택적으로 유지합니다.</summary>
		public static List<MethodArgumentData> RebuildArguments(IReadOnlyList<MethodArgumentData> existingArguments, MethodDescriptor descriptor, bool preserveCompatibleValues)
		{
			List<MethodArgumentData> list = new List<MethodArgumentData>();
			if (descriptor == null)
			{
				return list;
			}
			foreach (MethodParameterDescriptor serializedParameter in descriptor.SerializedParameters)
			{
				MethodArgumentData compatibleArgument = null;
				if (preserveCompatibleValues && existingArguments != null)
				{
					for (int i = 0; i < existingArguments.Count; i++)
					{
						MethodArgumentData candidate = existingArguments[i];
						if (TryDecode(candidate, serializedParameter, out _, out _))
						{
							compatibleArgument = candidate;
							break;
						}
					}
				}
				list.Add(compatibleArgument ?? CreateDefaultArgument(serializedParameter));
			}
			return list;
		}

		/// <summary>메서드를 실행하지 않고 저장된 인수를 해당 설명 정보로 호출할 수 있는지 확인합니다.</summary>
		public static bool TryValidateArguments(IReadOnlyList<MethodArgumentData> arguments, MethodDescriptor descriptor, out string error)
		{
			object[] invocationArguments;
			return TryBuildInvocationArguments(arguments, descriptor, null, null, validateOnly: true, out invocationArguments, out error);
		}

		/// <summary>그래프 데이터를 메서드 호출 순서에 맞는 인수 배열로 복원합니다.</summary>
		public static bool TryBuildInvocationArguments(IReadOnlyList<MethodArgumentData> arguments, DialogueMethodDescriptor descriptor, DialogueContext context, out object[] invocationArguments, out string error)
		{
			return TryBuildInvocationArguments(arguments, descriptor, context, null, validateOnly: false, out invocationArguments, out error);
		}

		/// <summary>그래프 인수를 복원하고 현재 Quest 실행 문맥을 주입합니다.</summary>
		public static bool TryBuildQuestInvocationArguments(
			IReadOnlyList<MethodArgumentData> arguments,
			QuestMethodDescriptor descriptor,
			QuestExecutionContext context,
			out object[] invocationArguments,
			out string error)
		{
			return TryBuildInvocationArguments(
				arguments,
				descriptor,
				null,
				context,
				validateOnly: false,
				out invocationArguments,
				out error);
		}

		/// <summary>인수 식별자와 값 타입을 확인하면서 저장된 인수 하나를 복원합니다.</summary>
		public static bool TryDecode(MethodArgumentData data, MethodParameterDescriptor parameter, out object value, out string error)
		{
			value = null;
			if (!IsShapeCompatible(data, parameter))
			{
				error = $"저장된 인수 '{parameter?.ParameterId}'가 현재 메서드 시그니처와 일치하지 않습니다.";
				return false;
			}
			Type parameterType = parameter.ParameterType;
			string text = data.SerializedValue ?? string.Empty;
			switch (parameter.Kind)
			{
			case MethodArgumentKind.String:
				value = text;
				error = null;
				return true;
			case MethodArgumentKind.Boolean:
			{
				if (bool.TryParse(text, out var result))
				{
					value = result;
					error = null;
					return true;
				}
				break;
			}
			case MethodArgumentKind.Integer:
				if (parameterType == typeof(int)
					&& int.TryParse(text, NumberStyles.Integer, Invariant, out int integerValue))
				{
					value = integerValue;
					error = null;
					return true;
				}
				break;
			case MethodArgumentKind.FloatingPoint:
			{
				if (parameterType == typeof(float) && float.TryParse(text, NumberStyles.Float, Invariant, out var result2) && !float.IsNaN(result2) && !float.IsInfinity(result2))
				{
					value = result2;
					error = null;
					return true;
				}
				break;
			}
			case MethodArgumentKind.Enum:
				if (TryParseEnum(text, parameterType, out value))
				{
					error = null;
					return true;
				}
				break;
			case MethodArgumentKind.UnityObject:
				if (data.ObjectValue == (object)null || parameterType.IsInstanceOfType(data.ObjectValue))
				{
					value = data.ObjectValue;
					error = null;
					return true;
				}
				break;
			}
			error = $"인수 '{parameter.ParameterId}'를 {parameterType.Name} 타입으로 변환할 수 없습니다.";
			return false;
		}

		/// <summary>Unity 객체가 아닌 값을 문화권에 영향받지 않는 형식으로 직렬화합니다.</summary>
		public static string SerializeScalar(object value, Type type, MethodArgumentKind kind)
		{
			if (value == null)
			{
				return string.Empty;
			}
			if (kind == MethodArgumentKind.Enum)
			{
				Type underlyingType = Enum.GetUnderlyingType(type);
				object underlyingValue = Convert.ChangeType(value, underlyingType, Invariant);
				return Convert.ToString(underlyingValue, Invariant);
			}
			if (value is bool booleanValue)
			{
				return booleanValue ? "true" : "false";
			}
			return Convert.ToString(value, Invariant) ?? string.Empty;
		}

		private static bool TryBuildInvocationArguments(
			IReadOnlyList<MethodArgumentData> arguments,
			MethodDescriptor descriptor,
			DialogueContext dialogueContext,
			QuestExecutionContext questContext,
			bool validateOnly,
			out object[] invocationArguments,
			out string error)
		{
			invocationArguments = null;
			if (descriptor == null)
			{
				error = "메서드 설명 정보가 null입니다.";
				return false;
			}
			int argumentCount = arguments?.Count ?? 0;
			if (argumentCount != descriptor.SerializedParameters.Count)
			{
				error = $"'{descriptor.Key}'에는 직렬화된 인수가 {descriptor.SerializedParameters.Count}개 필요하지만 {argumentCount}개 발견되었습니다.";
				return false;
			}
			var argumentsById = new Dictionary<string, MethodArgumentData>();
			if (arguments != null)
			{
				foreach (MethodArgumentData argument in arguments)
				{
					if (argument == null
						|| string.IsNullOrWhiteSpace(argument.ParameterId)
						|| !argumentsById.TryAdd(argument.ParameterId, argument))
					{
						error = $"'{descriptor.Key}'에 null, 빈 값 또는 중복된 파라미터 ID가 있습니다.";
						return false;
					}
				}
			}
			invocationArguments = new object[descriptor.Parameters.Count];
			foreach (MethodParameterDescriptor parameter in descriptor.Parameters)
			{
				if (parameter.Source == MethodParameterSource.DialogueContext)
				{
					if (!validateOnly && dialogueContext == null)
					{
						error = $"'{descriptor.Key}'를 실행하려면 DialogueContext가 필요합니다.";
						return false;
					}
					invocationArguments[parameter.MethodIndex] = dialogueContext;
					continue;
				}
				if (parameter.Source == MethodParameterSource.QuestContext)
				{
					if (!validateOnly && questContext == null)
					{
						error = $"'{descriptor.Key}'를 실행하려면 QuestExecutionContext가 필요합니다.";
						return false;
					}
					invocationArguments[parameter.MethodIndex] = questContext;
					continue;
				}
				if (!argumentsById.TryGetValue(parameter.ParameterId, out MethodArgumentData savedArgument))
				{
					error = $"'{descriptor.Key}'에 인수 '{parameter.ParameterId}'가 없습니다.";
					return false;
				}
				if (!TryDecode(savedArgument, parameter, out object decodedValue, out error))
				{
					error = "'" + descriptor.Key + "' " + error;
					return false;
				}
				invocationArguments[parameter.MethodIndex] = decodedValue;
			}
			error = null;
			return true;
		}

		private static MethodArgumentData CreateDefaultArgument(MethodParameterDescriptor parameter)
		{
			return new MethodArgumentData
			{
				ParameterId = parameter.ParameterId,
				DeclaredTypeId = parameter.DeclaredTypeId,
				Kind = parameter.Kind,
				SerializedValue = GetDefaultSerializedValue(parameter),
				ObjectValue = null
			};
		}

		private static string GetDefaultSerializedValue(MethodParameterDescriptor parameter)
		{
			switch (parameter.Kind)
			{
			case MethodArgumentKind.Boolean:
				return "false";
			case MethodArgumentKind.Integer:
			case MethodArgumentKind.FloatingPoint:
				return "0";
			case MethodArgumentKind.Enum:
			{
				Array values = Enum.GetValues(parameter.ParameterType);
				object value = ((values.Length > 0) ? values.GetValue(0) : Activator.CreateInstance(parameter.ParameterType));
				return SerializeScalar(value, parameter.ParameterType, MethodArgumentKind.Enum);
			}
			default:
				return string.Empty;
			}
		}

		private static bool IsShapeCompatible(MethodArgumentData data, MethodParameterDescriptor parameter)
		{
			return data != null && parameter != null && data.ParameterId == parameter.ParameterId && data.DeclaredTypeId == parameter.DeclaredTypeId && data.Kind == parameter.Kind;
		}

		private static bool TryParseEnum(string text, Type enumType, out object value)
		{
			return Enum.TryParse(enumType, text, out value);
		}
	}
}



