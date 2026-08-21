using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace UniversalGraph
{
	public static class DialogueArgumentCodec
	{
		private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

		public static bool TryGetKind(Type type, out DialogueArgumentKind kind)
		{
			if (type == typeof(string))
			{
				kind = DialogueArgumentKind.String;
				return true;
			}
			if (type == typeof(bool))
			{
				kind = DialogueArgumentKind.Boolean;
				return true;
			}
			if (type != null && type.IsEnum)
			{
				kind = DialogueArgumentKind.Enum;
				return true;
			}
			if (type != null && typeof(Object).IsAssignableFrom(type))
			{
				kind = DialogueArgumentKind.UnityObject;
				return true;
			}
			if (IsInteger(type))
			{
				kind = DialogueArgumentKind.Integer;
				return true;
			}
			if (type == typeof(float) || type == typeof(double))
			{
				kind = DialogueArgumentKind.FloatingPoint;
				return true;
			}
			kind = DialogueArgumentKind.String;
			return false;
		}

		public static List<DialogueArgumentData> CreateDefaultArguments(DialogueMethodDescriptor descriptor)
		{
			List<DialogueArgumentData> list = new List<DialogueArgumentData>();
			if (descriptor == null)
			{
				return list;
			}
			foreach (DialogueParameterDescriptor serializedParameter in descriptor.SerializedParameters)
			{
				list.Add(CreateDefaultArgument(serializedParameter));
			}
			return list;
		}

		public static List<DialogueArgumentData> RebuildArguments(IReadOnlyList<DialogueArgumentData> existingArguments, DialogueMethodDescriptor descriptor, bool preserveCompatibleValues)
		{
			List<DialogueArgumentData> list = new List<DialogueArgumentData>();
			if (descriptor == null)
			{
				return list;
			}
			foreach (DialogueParameterDescriptor serializedParameter in descriptor.SerializedParameters)
			{
				DialogueArgumentData dialogueArgumentData = null;
				if (preserveCompatibleValues && existingArguments != null)
				{
					for (int i = 0; i < existingArguments.Count; i++)
					{
						DialogueArgumentData dialogueArgumentData2 = existingArguments[i];
						if (IsShapeCompatible(dialogueArgumentData2, serializedParameter) && TryDecode(dialogueArgumentData2, serializedParameter, out var _, out var _))
						{
							dialogueArgumentData = dialogueArgumentData2;
							break;
						}
					}
				}
				list.Add(dialogueArgumentData ?? CreateDefaultArgument(serializedParameter));
			}
			return list;
		}

		public static bool TryValidateArguments(IReadOnlyList<DialogueArgumentData> arguments, string legacyStringParameter, DialogueMethodDescriptor descriptor, out string error)
		{
			object[] invocationArguments;
			return TryBuildInvocationArguments(arguments, legacyStringParameter, descriptor, null, validateOnly: true, out invocationArguments, out error);
		}

		public static bool TryBuildInvocationArguments(IReadOnlyList<DialogueArgumentData> arguments, string legacyStringParameter, DialogueMethodDescriptor descriptor, DialogueContext context, out object[] invocationArguments, out string error)
		{
			return TryBuildInvocationArguments(arguments, legacyStringParameter, descriptor, context, validateOnly: false, out invocationArguments, out error);
		}

		public static bool TryDecode(DialogueArgumentData data, DialogueParameterDescriptor parameter, out object value, out string error)
		{
			value = null;
			if (!IsShapeCompatible(data, parameter))
			{
				error = "'" + parameter?.ParameterId + "' ?뚮씪誘명꽣???\u0080???\u0080?낆씠 ?꾩옱 硫붿꽌???쒓렇?덉쿂?\u0080 ?ㅻ쫭?덈떎.";
				return false;
			}
			Type parameterType = parameter.ParameterType;
			string text = data.SerializedValue ?? string.Empty;
			switch (parameter.Kind)
			{
			case DialogueArgumentKind.String:
				value = text;
				error = null;
				return true;
			case DialogueArgumentKind.Boolean:
			{
				if (bool.TryParse(text, out var result))
				{
					value = result;
					error = null;
					return true;
				}
				break;
			}
			case DialogueArgumentKind.Integer:
				if (TryParseInteger(text, parameterType, out value))
				{
					error = null;
					return true;
				}
				break;
			case DialogueArgumentKind.FloatingPoint:
			{
				if (parameterType == typeof(float) && float.TryParse(text, NumberStyles.Float, Invariant, out var result2) && !float.IsNaN(result2) && !float.IsInfinity(result2))
				{
					value = result2;
					error = null;
					return true;
				}
				if (parameterType == typeof(double) && double.TryParse(text, NumberStyles.Float, Invariant, out var result3) && !double.IsNaN(result3) && !double.IsInfinity(result3))
				{
					value = result3;
					error = null;
					return true;
				}
				break;
			}
			case DialogueArgumentKind.Enum:
				if (TryParseEnum(text, parameterType, out value))
				{
					error = null;
					return true;
				}
				break;
			case DialogueArgumentKind.UnityObject:
				if (data.ObjectValue == (Object)null || parameterType.IsInstanceOfType(data.ObjectValue))
				{
					value = data.ObjectValue;
					error = null;
					return true;
				}
				break;
			}
			error = "'" + parameter.ParameterId + "' 媛믪쓣 " + parameterType.Name + " ?\u0080?낆쑝濡?蹂듭썝?????놁뒿?덈떎.";
			return false;
		}

		public static string SerializeScalar(object value, Type type, DialogueArgumentKind kind)
		{
			if (value == null)
			{
				return string.Empty;
			}
			if (kind == DialogueArgumentKind.Enum)
			{
				Type underlyingType = Enum.GetUnderlyingType(type);
				object obj = Convert.ChangeType(value, underlyingType, Invariant);
				return Convert.ToString(obj, Invariant);
			}
			if (value is bool flag)
			{
				return flag ? "true" : "false";
			}
			return Convert.ToString(value, Invariant) ?? string.Empty;
		}

		private static bool TryBuildInvocationArguments(IReadOnlyList<DialogueArgumentData> arguments, string legacyStringParameter, DialogueMethodDescriptor descriptor, DialogueContext context, bool validateOnly, out object[] invocationArguments, out string error)
		{
			invocationArguments = null;
			if (descriptor == null)
			{
				error = "MethodDescriptor媛\u0080 ?놁뒿?덈떎.";
				return false;
			}
			bool flag = (arguments == null || arguments.Count == 0) && descriptor.SerializedParameters.Count == 1 && descriptor.SerializedParameters[0].ParameterType == typeof(string);
			int num = arguments?.Count ?? 0;
			if (!flag && num != descriptor.SerializedParameters.Count)
			{
				error = "'" + descriptor.Key + "'???\u0080???뚮씪誘명꽣 媛쒖닔媛\u0080 ?ㅻ쫭?덈떎. " + $"?꾩슂: {descriptor.SerializedParameters.Count}, ?\u0080?? {num}";
				return false;
			}
			Dictionary<string, DialogueArgumentData> dictionary = new Dictionary<string, DialogueArgumentData>(StringComparer.Ordinal);
			if (!flag && arguments != null)
			{
				foreach (DialogueArgumentData argument in arguments)
				{
					if (argument == null || string.IsNullOrWhiteSpace(argument.ParameterId) || !dictionary.TryAdd(argument.ParameterId, argument))
					{
						error = "'" + descriptor.Key + "'??null/鍮?媛?以묐났 Parameter ID媛\u0080 ?덉뒿?덈떎.";
						return false;
					}
				}
			}
			invocationArguments = new object[descriptor.Parameters.Count];
			foreach (DialogueParameterDescriptor parameter in descriptor.Parameters)
			{
				if (parameter.Source == DialogueParameterSource.DialogueContext)
				{
					if (!validateOnly && context == null)
					{
						error = "'" + descriptor.Key + "' ?ㅽ뻾??DialogueContext媛\u0080 ?꾩슂?⑸땲??";
						return false;
					}
					invocationArguments[parameter.MethodIndex] = context;
					continue;
				}
				if (flag)
				{
					invocationArguments[parameter.MethodIndex] = legacyStringParameter ?? string.Empty;
					continue;
				}
				if (!dictionary.TryGetValue(parameter.ParameterId, out var value))
				{
					error = "'" + descriptor.Key + "'??'" + parameter.ParameterId + "' 媛믪씠 ?놁뒿?덈떎.";
					return false;
				}
				if (!TryDecode(value, parameter, out var value2, out error))
				{
					error = "'" + descriptor.Key + "' " + error;
					return false;
				}
				invocationArguments[parameter.MethodIndex] = value2;
			}
			error = null;
			return true;
		}

		private static DialogueArgumentData CreateDefaultArgument(DialogueParameterDescriptor parameter)
		{
			return new DialogueArgumentData
			{
				ParameterId = parameter.ParameterId,
				DeclaredTypeId = parameter.DeclaredTypeId,
				Kind = parameter.Kind,
				SerializedValue = GetDefaultSerializedValue(parameter),
				ObjectValue = null
			};
		}

		private static string GetDefaultSerializedValue(DialogueParameterDescriptor parameter)
		{
			switch (parameter.Kind)
			{
			case DialogueArgumentKind.Boolean:
				return "false";
			case DialogueArgumentKind.Integer:
			case DialogueArgumentKind.FloatingPoint:
				return "0";
			case DialogueArgumentKind.Enum:
			{
				Array values = Enum.GetValues(parameter.ParameterType);
				object value = ((values.Length > 0) ? values.GetValue(0) : Activator.CreateInstance(parameter.ParameterType));
				return SerializeScalar(value, parameter.ParameterType, DialogueArgumentKind.Enum);
			}
			default:
				return string.Empty;
			}
		}

		private static bool IsShapeCompatible(DialogueArgumentData data, DialogueParameterDescriptor parameter)
		{
			return data != null && parameter != null && string.Equals(data.ParameterId, parameter.ParameterId, StringComparison.Ordinal) && string.Equals(data.DeclaredTypeId, parameter.DeclaredTypeId, StringComparison.Ordinal) && data.Kind == parameter.Kind;
		}

		private static bool IsInteger(Type type)
		{
			return type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
		}

		private static bool TryParseInteger(string text, Type type, out object value)
		{
			NumberStyles style = NumberStyles.Integer;
			byte result2;
			short result3;
			ushort result4;
			int result5;
			uint result6;
			long result7;
			if (type == typeof(sbyte) && sbyte.TryParse(text, style, Invariant, out var result))
			{
				value = result;
			}
			else if (type == typeof(byte) && byte.TryParse(text, style, Invariant, out result2))
			{
				value = result2;
			}
			else if (type == typeof(short) && short.TryParse(text, style, Invariant, out result3))
			{
				value = result3;
			}
			else if (type == typeof(ushort) && ushort.TryParse(text, style, Invariant, out result4))
			{
				value = result4;
			}
			else if (type == typeof(int) && int.TryParse(text, style, Invariant, out result5))
			{
				value = result5;
			}
			else if (type == typeof(uint) && uint.TryParse(text, style, Invariant, out result6))
			{
				value = result6;
			}
			else if (type == typeof(long) && long.TryParse(text, style, Invariant, out result7))
			{
				value = result7;
			}
			else
			{
				if (!(type == typeof(ulong)) || !ulong.TryParse(text, style, Invariant, out var result8))
				{
					value = null;
					return false;
				}
				value = result8;
			}
			return true;
		}

		private static bool TryParseEnum(string text, Type enumType, out object value)
		{
			Type underlyingType = Enum.GetUnderlyingType(enumType);
			if (!TryParseInteger(text, underlyingType, out var value2))
			{
				value = null;
				return false;
			}
			value = Enum.ToObject(enumType, value2);
			return true;
		}
	}
}
