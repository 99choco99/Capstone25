using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalDialogue.Editor
{
	internal static class DialogueMethodBindingEditor
	{
		public static VisualElement Create(NodeInspectorContext context, string title, DialogueMethodKind kind, DialogueMethodBindingAccessor binding)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Expected O, but got Unknown
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Expected O, but got Unknown
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0169: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Expected O, but got Unknown
			VisualElement val = new VisualElement();
			val.AddToClassList("action-editor");
			Label val2 = new Label(title);
			((VisualElement)val2).AddToClassList("action-title");
			val.Add((VisualElement)(object)val2);
			string currentKey = binding.GetKey?.Invoke() ?? string.Empty;
			List<string> list = new List<string> { string.Empty };
			foreach (DialogueMethodDescriptor method in DialogueMethodCatalog.GetMethods(kind))
			{
				list.Add(method.Key);
			}
			if (!string.IsNullOrWhiteSpace(currentKey) && !DialogueMethodCatalog.TryGetMethod(kind, currentKey, out var _))
			{
				list.Add(currentKey);
			}
			int num = list.FindIndex((string key) => string.Equals(key, currentKey, StringComparison.Ordinal));
			if (num < 0)
			{
				num = 0;
			}
			PopupField<string> val3 = new PopupField<string>(((int)kind == 0) ? "Action" : "Condition", list, num, (Func<string, string>)FormatKey, (Func<string, string>)FormatKey);
			val.Add((VisualElement)(object)val3);
			VisualElement parametersRoot = new VisualElement();
			parametersRoot.AddToClassList("method-parameters");
			val.Add(parametersRoot);
			INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)val3, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
			{
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				string selectedKey = evt.newValue ?? string.Empty;
				context.ApplyEdit($"{kind} 메서드 변경", (Action)delegate
				{
					//IL_0029: Unknown result type (might be due to invalid IL or missing references)
					binding.SetKey?.Invoke(selectedKey);
					DialogueMethodDescriptor descriptor4;
					List<DialogueArgumentData> obj2 = (DialogueMethodCatalog.TryGetMethod(kind, selectedKey, out descriptor4) ? DialogueArgumentCodec.CreateDefaultArguments(descriptor4) : new List<DialogueArgumentData>());
					binding.SetArguments?.Invoke(obj2);
					binding.SetLegacyParameter?.Invoke(string.Empty);
					RedrawParameters();
				});
			});
			RedrawParameters();
			return val;
			string FormatKey(string key)
			{
				//IL_0014: Unknown result type (might be due to invalid IL or missing references)
				if (string.IsNullOrWhiteSpace(key))
				{
					return "None";
				}
				DialogueMethodDescriptor descriptor3;
				return DialogueMethodCatalog.TryGetMethod(kind, key, out descriptor3) ? descriptor3.DisplayName : ("<Missing> " + key);
			}
			void RedrawParameters()
			{
				//IL_004d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0075: Unknown result type (might be due to invalid IL or missing references)
				//IL_008f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0099: Expected O, but got Unknown
				//IL_0104: Unknown result type (might be due to invalid IL or missing references)
				//IL_010e: Expected O, but got Unknown
				//IL_011b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0120: Unknown result type (might be due to invalid IL or missing references)
				//IL_012e: Expected O, but got Unknown
				//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
				//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
				//IL_01b4: Expected O, but got Unknown
				//IL_01b7: Expected O, but got Unknown
				//IL_0204: Unknown result type (might be due to invalid IL or missing references)
				//IL_020e: Expected O, but got Unknown
				//IL_028b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0295: Expected O, but got Unknown
				parametersRoot.Clear();
				string text = binding.GetKey?.Invoke() ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(text))
				{
					if (!DialogueMethodCatalog.TryGetMethod(kind, text, out var descriptor2))
					{
						parametersRoot.Add((VisualElement)new HelpBox($"'{text}'에 등록된 {kind} 메서드를 찾을 수 없습니다. " + "기존 값은 다른 메서드를 선택하기 전까지 보존됩니다.", (HelpBoxMessageType)3));
					}
					else
					{
						List<DialogueArgumentData> list2 = binding.GetArguments?.Invoke();
						string text2 = binding.GetLegacyParameter?.Invoke() ?? string.Empty;
						string text3 = default(string);
						if (!DialogueArgumentCodec.TryValidateArguments((IReadOnlyList<DialogueArgumentData>)list2, text2, descriptor2, ref text3))
						{
							parametersRoot.Add((VisualElement)new HelpBox("저장된 파라미터가 현재 메서드 시그니처와 다릅니다.\n" + text3, (HelpBoxMessageType)3));
							Button val4 = new Button((Action)delegate
							{
								context.ApplyEdit("메서드 파라미터 재구성", (Action)delegate
								{
									List<DialogueArgumentData> obj = DialogueArgumentCodec.RebuildArguments((IReadOnlyList<DialogueArgumentData>)(binding.GetArguments?.Invoke()), descriptor2, true);
									binding.SetArguments?.Invoke(obj);
									binding.SetLegacyParameter?.Invoke(string.Empty);
									RedrawParameters();
								});
							})
							{
								text = "Rebuild Parameters (Preserve Compatible Values)"
							};
							parametersRoot.Add((VisualElement)(object)val4);
						}
						else if ((list2 == null || list2.Count == 0) && descriptor2.SerializedParameters.Count == 1 && descriptor2.SerializedParameters[0].ParameterType == typeof(string))
						{
							TextField val5 = new TextField(descriptor2.SerializedParameters[0].DisplayName);
							((BaseField<string>)val5).value = text2;
							TextField val6 = val5;
							INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)val6, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
							{
								context.ApplyEdit("레거시 string 파라미터 변경", (Action)delegate
								{
									binding.SetLegacyParameter?.Invoke(evt.newValue);
								});
							});
							parametersRoot.Add((VisualElement)(object)val6);
						}
						else
						{
							if (descriptor2.SerializedParameters.Count != 0)
							{
								foreach (DialogueParameterDescriptor parameter in descriptor2.SerializedParameters)
								{
									DialogueArgumentData val7 = list2?.FirstOrDefault((DialogueArgumentData candidate) => candidate != null && string.Equals(candidate.ParameterId, parameter.ParameterId, StringComparison.Ordinal));
									if (val7 == null)
									{
										parametersRoot.Add((VisualElement)new HelpBox("'" + parameter.ParameterId + "' 파라미터 데이터가 없습니다.", (HelpBoxMessageType)3));
									}
									else
									{
										parametersRoot.Add(CreateArgumentField(context, val7, parameter));
									}
								}
								return;
							}
							parametersRoot.Add((VisualElement)new HelpBox("그래프에 저장할 파라미터가 없습니다.", (HelpBoxMessageType)1));
						}
					}
				}
			}
		}

		private static VisualElement CreateArgumentField(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
		{
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Expected O, but got Unknown
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_017b: Unknown result type (might be due to invalid IL or missing references)
			//IL_017d: Unknown result type (might be due to invalid IL or missing references)
			//IL_017f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0181: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a0: Expected I4, but got Unknown
			//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0208: Expected O, but got Unknown
			//IL_020b: Expected O, but got Unknown
			//IL_04e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_0500: Unknown result type (might be due to invalid IL or missing references)
			//IL_0508: Unknown result type (might be due to invalid IL or missing references)
			//IL_0519: Expected O, but got Unknown
			//IL_051c: Expected O, but got Unknown
			//IL_01cb: Expected O, but got Unknown
			//IL_01ce: Expected O, but got Unknown
			//IL_0252: Unknown result type (might be due to invalid IL or missing references)
			//IL_0257: Unknown result type (might be due to invalid IL or missing references)
			//IL_0268: Expected O, but got Unknown
			//IL_026b: Expected O, but got Unknown
			//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_03be: Expected O, but got Unknown
			//IL_03c4: Expected O, but got Unknown
			//IL_04c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c8: Expected O, but got Unknown
			//IL_0495: Unknown result type (might be due to invalid IL or missing references)
			//IL_049c: Expected O, but got Unknown
			//IL_0556: Unknown result type (might be due to invalid IL or missing references)
			//IL_055d: Expected O, but got Unknown
			//IL_040f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0414: Unknown result type (might be due to invalid IL or missing references)
			//IL_042b: Expected O, but got Unknown
			//IL_0431: Expected O, but got Unknown
			//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c8: Expected O, but got Unknown
			//IL_02cb: Expected O, but got Unknown
			//IL_0306: Unknown result type (might be due to invalid IL or missing references)
			//IL_030b: Unknown result type (might be due to invalid IL or missing references)
			//IL_032b: Expected O, but got Unknown
			//IL_032c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0333: Expected O, but got Unknown
			//IL_0339: Expected O, but got Unknown
			object value = default(object);
			string text = default(string);
			if (!DialogueArgumentCodec.TryDecode(argument, parameter, ref value, ref text))
			{
				return (VisualElement)new HelpBox(text, (HelpBoxMessageType)3);
			}
			string label = parameter.DisplayName;
			string text2 = null;
			Type parameterType = parameter.ParameterType;
			if (parameterType == typeof(string))
			{
				text2 = "String";
			}
			else if (parameterType == typeof(int))
			{
				text2 = "Int";
			}
			else if (parameterType == typeof(float))
			{
				text2 = "Float";
			}
			else if (parameterType == typeof(bool))
			{
				text2 = "Bool";
			}
			else if (parameterType.IsEnum)
			{
				text2 = "Enum<" + parameterType.Name + ">";
			}
			else if (typeof(Object).IsAssignableFrom(parameterType))
			{
				text2 = "Object<" + parameterType.Name + ">";
			}
			if (text2 != null && ArgumentDrawerRegistry.TryGet(text2, out var factory))
			{
				return factory(context, argument, parameter);
			}
			DialogueArgumentKind kind = parameter.Kind;
			DialogueArgumentKind val = kind;
			switch ((int)val)
			{
			case 0:
			{
				TextField val15 = new TextField(label);
				((BaseField<string>)val15).value = ((string)value) ?? string.Empty;
				TextField val16 = val15;
				INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)val16, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
				{
					RecordScalar(evt.newValue);
				});
				return (VisualElement)(object)val16;
			}
			case 1:
			{
				Toggle val6 = new Toggle(label);
				((BaseField<bool>)val6).value = (bool)value;
				Toggle val7 = val6;
				INotifyValueChangedExtensions.RegisterValueChangedCallback<bool>((INotifyValueChanged<bool>)(object)val7, (EventCallback<ChangeEvent<bool>>)delegate(ChangeEvent<bool> evt)
				{
					//IL_001d: Unknown result type (might be due to invalid IL or missing references)
					RecordScalar(DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, parameter.Kind));
				});
				return (VisualElement)(object)val7;
			}
			case 2:
			{
				if (parameter.ParameterType == typeof(int))
				{
					IntegerField val8 = new IntegerField(label, 1000);
					((BaseField<int>)val8).value = (int)value;
					IntegerField val9 = val8;
					INotifyValueChangedExtensions.RegisterValueChangedCallback<int>((INotifyValueChanged<int>)(object)val9, (EventCallback<ChangeEvent<int>>)delegate(ChangeEvent<int> evt)
					{
						//IL_001d: Unknown result type (might be due to invalid IL or missing references)
						RecordScalar(DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, parameter.Kind));
					});
					return (VisualElement)(object)val9;
				}
				if (parameter.ParameterType == typeof(long))
				{
					LongField val10 = new LongField(label, 1000);
					((BaseField<long>)val10).value = (long)value;
					LongField val11 = val10;
					INotifyValueChangedExtensions.RegisterValueChangedCallback<long>((INotifyValueChanged<long>)(object)val11, (EventCallback<ChangeEvent<long>>)delegate(ChangeEvent<long> evt)
					{
						//IL_001d: Unknown result type (might be due to invalid IL or missing references)
						RecordScalar(DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, parameter.Kind));
					});
					return (VisualElement)(object)val11;
				}
				TextField val12 = new TextField(label);
				((BaseField<string>)val12).value = argument.SerializedValue ?? string.Empty;
				((TextInputBaseField<string>)val12).isDelayed = true;
				TextField field3 = val12;
				INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)field3, (EventCallback<ChangeEvent<string>>)delegate(ChangeEvent<string> evt)
				{
					if (CanDecode(evt.newValue))
					{
						RecordScalar(evt.newValue);
					}
					else
					{
						((BaseField<string>)(object)field3).SetValueWithoutNotify(argument.SerializedValue ?? string.Empty);
					}
				});
				return (VisualElement)(object)field3;
			}
			case 3:
			{
				if (parameter.ParameterType == typeof(float))
				{
					FloatField val13 = new FloatField(label, 1000);
					((BaseField<float>)val13).value = (float)value;
					FloatField field2 = val13;
					INotifyValueChangedExtensions.RegisterValueChangedCallback<float>((INotifyValueChanged<float>)(object)field2, (EventCallback<ChangeEvent<float>>)delegate(ChangeEvent<float> evt)
					{
						//IL_0027: Unknown result type (might be due to invalid IL or missing references)
						string serializedValue3 = DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, parameter.Kind);
						if (CanDecode(serializedValue3))
						{
							RecordScalar(serializedValue3);
						}
						else
						{
							((BaseField<float>)(object)field2).SetValueWithoutNotify((float)value);
						}
					});
					return (VisualElement)(object)field2;
				}
				DoubleField val14 = new DoubleField(label, 1000);
				((BaseField<double>)val14).value = (double)value;
				DoubleField field = val14;
				INotifyValueChangedExtensions.RegisterValueChangedCallback<double>((INotifyValueChanged<double>)(object)field, (EventCallback<ChangeEvent<double>>)delegate(ChangeEvent<double> evt)
				{
					//IL_0027: Unknown result type (might be due to invalid IL or missing references)
					string serializedValue2 = DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, parameter.Kind);
					if (CanDecode(serializedValue2))
					{
						RecordScalar(serializedValue2);
					}
					else
					{
						((BaseField<double>)(object)field).SetValueWithoutNotify((double)value);
					}
				});
				return (VisualElement)(object)field;
			}
			case 4:
			{
				Enum @enum = (Enum)value;
				if (parameter.ParameterType.IsDefined(typeof(FlagsAttribute), inherit: false))
				{
					EnumFlagsField val4 = new EnumFlagsField(label, @enum);
					INotifyValueChangedExtensions.RegisterValueChangedCallback<Enum>((INotifyValueChanged<Enum>)(object)val4, (EventCallback<ChangeEvent<Enum>>)delegate(ChangeEvent<Enum> evt)
					{
						//IL_0018: Unknown result type (might be due to invalid IL or missing references)
						RecordScalar(DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, parameter.Kind));
					});
					return (VisualElement)(object)val4;
				}
				EnumField val5 = new EnumField(label, @enum);
				INotifyValueChangedExtensions.RegisterValueChangedCallback<Enum>((INotifyValueChanged<Enum>)(object)val5, (EventCallback<ChangeEvent<Enum>>)delegate(ChangeEvent<Enum> evt)
				{
					//IL_0018: Unknown result type (might be due to invalid IL or missing references)
					RecordScalar(DialogueArgumentCodec.SerializeScalar((object)evt.newValue, parameter.ParameterType, parameter.Kind));
				});
				return (VisualElement)(object)val5;
			}
			case 5:
			{
				ObjectField val2 = new ObjectField(label)
				{
					objectType = parameter.ParameterType,
					allowSceneObjects = false
				};
				((BaseField<Object>)val2).value = argument.ObjectValue;
				ObjectField val3 = val2;
				INotifyValueChangedExtensions.RegisterValueChangedCallback<Object>((INotifyValueChanged<Object>)(object)val3, (EventCallback<ChangeEvent<Object>>)delegate(ChangeEvent<Object> evt)
				{
					context.ApplyEdit(label + " 파라미터 변경", (Action)delegate
					{
						argument.SerializedValue = string.Empty;
						argument.ObjectValue = evt.newValue;
					});
				});
				return (VisualElement)(object)val3;
			}
			default:
				return (VisualElement)new HelpBox("'" + parameter.ParameterType.Name + "' 파라미터를 그릴 수 없습니다.", (HelpBoxMessageType)3);
			}
			bool CanDecode(string serializedValue)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Unknown result type (might be due to invalid IL or missing references)
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0034: Unknown result type (might be due to invalid IL or missing references)
				//IL_0039: Unknown result type (might be due to invalid IL or missing references)
				//IL_0040: Unknown result type (might be due to invalid IL or missing references)
				//IL_0052: Expected O, but got Unknown
				DialogueArgumentData val17 = new DialogueArgumentData
				{
					ParameterId = argument.ParameterId,
					DeclaredTypeId = argument.DeclaredTypeId,
					Kind = argument.Kind,
					SerializedValue = serializedValue,
					ObjectValue = argument.ObjectValue
				};
				object obj = default(object);
				string text3 = default(string);
				return DialogueArgumentCodec.TryDecode(val17, parameter, ref obj, ref text3);
			}
			void RecordScalar(string serializedValue)
			{
				context.ApplyEdit(label + " 파라미터 변경", (Action)delegate
				{
					argument.SerializedValue = serializedValue;
					argument.ObjectValue = null;
				});
			}
		}
	}
}
