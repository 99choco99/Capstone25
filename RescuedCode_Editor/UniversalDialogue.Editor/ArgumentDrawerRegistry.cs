using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	public static class ArgumentDrawerRegistry
	{
		private static readonly Dictionary<string, DrawerFactory> factories;

		static ArgumentDrawerRegistry()
		{
			factories = new Dictionary<string, DrawerFactory>(StringComparer.Ordinal);
			Register("String", CreateStringField);
			Register("Int", CreateIntField);
			Register("Float", CreateFloatField);
			Register("Bool", CreateBoolField);
		}

		public static void Register(string codecId, DrawerFactory factory)
		{
			factories[codecId] = factory;
		}

		public static bool TryGet(string codecId, out DrawerFactory factory)
		{
			if (factories.TryGetValue(codecId, out factory))
			{
				return true;
			}
			if (codecId.StartsWith("Enum<") && codecId.EndsWith(">"))
			{
				string typeName = codecId.Substring(5, codecId.Length - 6);
				Type type = FindType(typeName);
				if (type != null && type.IsEnum)
				{
					MethodInfo methodInfo = typeof(ArgumentDrawerRegistry).GetMethod("RegisterEnumDrawer").MakeGenericMethod(type);
					methodInfo.Invoke(null, new object[1] { codecId });
					return factories.TryGetValue(codecId, out factory);
				}
			}
			if (codecId.StartsWith("Object<") && codecId.EndsWith(">"))
			{
				string typeName2 = codecId.Substring(7, codecId.Length - 8);
				Type type2 = FindType(typeName2);
				if (type2 != null && typeof(Object).IsAssignableFrom(type2))
				{
					MethodInfo methodInfo2 = typeof(ArgumentDrawerRegistry).GetMethod("RegisterObjectDrawer").MakeGenericMethod(type2);
					methodInfo2.Invoke(null, new object[1] { codecId });
					return factories.TryGetValue(codecId, out factory);
				}
			}
			return false;
		}

		private static Type FindType(string typeName)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				Type type = assembly.GetType(typeName);
				if (type != null)
				{
					return type;
				}
			}
			return null;
		}

		private static VisualElement CreateStringField(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Expected O, but got Unknown
			//IL_0048: Expected O, but got Unknown
			TextField val = new TextField(parameter.DisplayName);
			((BaseField<string>)val).value = argument.SerializedValue ?? string.Empty;
			TextField val2 = val;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)val2, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
			{
				context.ApplyEdit(parameter.DisplayName + " 변경", (Action)delegate
				{
					argument.SerializedValue = evt.newValue;
					argument.ObjectValue = null;
				});
			});
			return (VisualElement)(object)val2;
		}

		private static VisualElement CreateIntField(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
		{
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Expected O, but got Unknown
			//IL_0057: Expected O, but got Unknown
			int result = 0;
			if (!int.TryParse(argument.SerializedValue, out result))
			{
				result = 0;
			}
			IntegerField val = new IntegerField(parameter.DisplayName, 1000);
			((BaseField<int>)val).value = result;
			IntegerField val2 = val;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<int>((INotifyValueChanged<int>)(object)val2, (EventCallback<ChangeEvent<int>>)delegate(ChangeEvent<int> evt)
			{
				context.ApplyEdit(parameter.DisplayName + " 변경", (Action)delegate
				{
					argument.SerializedValue = DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, (DialogueArgumentKind)2);
					argument.ObjectValue = null;
				});
			});
			return (VisualElement)(object)val2;
		}

		private static VisualElement CreateFloatField(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
		{
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Expected O, but got Unknown
			//IL_005f: Expected O, but got Unknown
			float result = 0f;
			if (!float.TryParse(argument.SerializedValue, out result))
			{
				result = 0f;
			}
			FloatField val = new FloatField(parameter.DisplayName, 1000);
			((BaseField<float>)val).value = result;
			FloatField val2 = val;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<float>((INotifyValueChanged<float>)(object)val2, (EventCallback<ChangeEvent<float>>)delegate(ChangeEvent<float> evt)
			{
				context.ApplyEdit(parameter.DisplayName + " 변경", (Action)delegate
				{
					argument.SerializedValue = DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, (DialogueArgumentKind)3);
					argument.ObjectValue = null;
				});
			});
			return (VisualElement)(object)val2;
		}

		private static VisualElement CreateBoolField(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
		{
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Expected O, but got Unknown
			//IL_0052: Expected O, but got Unknown
			bool result = false;
			if (!bool.TryParse(argument.SerializedValue, out result))
			{
				result = false;
			}
			Toggle val = new Toggle(parameter.DisplayName);
			((BaseField<bool>)val).value = result;
			Toggle val2 = val;
			INotifyValueChangedExtensions.RegisterValueChangedCallback<bool>((INotifyValueChanged<bool>)(object)val2, (EventCallback<ChangeEvent<bool>>)delegate(ChangeEvent<bool> evt)
			{
				context.ApplyEdit(parameter.DisplayName + " 변경", (Action)delegate
				{
					argument.SerializedValue = DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, (DialogueArgumentKind)1);
					argument.ObjectValue = null;
				});
			});
			return (VisualElement)(object)val2;
		}

		public static void RegisterEnumDrawer<TEnum>(string codecId) where TEnum : Enum
		{
			Register(codecId, delegate(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
			{
				//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
				//IL_00a7: Expected O, but got Unknown
				//IL_0072: Unknown result type (might be due to invalid IL or missing references)
				//IL_0079: Expected O, but got Unknown
				Enum @enum = (Enum)Enum.Parse(typeof(TEnum), argument.SerializedValue ?? string.Empty);
				if (typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false))
				{
					EnumFlagsField val = new EnumFlagsField(parameter.DisplayName, @enum);
					INotifyValueChangedExtensions.RegisterValueChangedCallback<Enum>((INotifyValueChanged<Enum>)(object)val, (EventCallback<ChangeEvent<Enum>>)delegate(ChangeEvent<Enum> evt)
					{
						context.ApplyEdit(parameter.DisplayName + " 변경", (Action)delegate
						{
							argument.SerializedValue = DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, (DialogueArgumentKind)4);
							argument.ObjectValue = null;
						});
					});
					return (VisualElement)(object)val;
				}
				EnumField val2 = new EnumField(parameter.DisplayName, @enum);
				INotifyValueChangedExtensions.RegisterValueChangedCallback<Enum>((INotifyValueChanged<Enum>)(object)val2, (EventCallback<ChangeEvent<Enum>>)delegate(ChangeEvent<Enum> evt)
				{
					context.ApplyEdit(parameter.DisplayName + " 변경", (Action)delegate
					{
						argument.SerializedValue = DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, (DialogueArgumentKind)4);
						argument.ObjectValue = null;
					});
				});
				return (VisualElement)(object)val2;
			});
		}

		public static void RegisterObjectDrawer<TObj>(string codecId) where TObj : Object
		{
			Register(codecId, delegate(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
			{
				//IL_0027: Unknown result type (might be due to invalid IL or missing references)
				//IL_002d: Expected O, but got Unknown
				ObjectField val = new ObjectField(parameter.DisplayName);
				val.objectType = typeof(TObj);
				val.allowSceneObjects = false;
				Object objectValue = argument.ObjectValue;
				((BaseField<Object>)(object)val).value = (Object)(object)(TObj)(object)((objectValue is TObj) ? objectValue : null);
				ObjectField val2 = val;
				INotifyValueChangedExtensions.RegisterValueChangedCallback<Object>((INotifyValueChanged<Object>)(object)val2, (EventCallback<ChangeEvent<Object>>)delegate(ChangeEvent<Object> evt)
				{
					context.ApplyEdit(parameter.DisplayName + " 변경", (Action)delegate
					{
						argument.SerializedValue = string.Empty;
						argument.ObjectValue = evt.newValue;
					});
				});
				return (VisualElement)(object)val2;
			});
		}
	}
}
