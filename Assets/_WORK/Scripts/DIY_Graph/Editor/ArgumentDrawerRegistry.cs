using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalGraph.Editor
{
    /// <summary>
    /// 메서드가 지원하는 직렬화 인수 타입에 맞는 UI Toolkit 입력 필드를 제공합니다.
    /// 이 등록부는 에디터 전용이며 실제 값 변환은 <see cref="MethodArgumentCodec"/>이 담당합니다.
    /// </summary>
    public static class ArgumentDrawerRegistry
    {
        private static readonly Dictionary<string, DrawerFactory> factories = new Dictionary<string, DrawerFactory>();

        static ArgumentDrawerRegistry()
        {
            Register("String", CreateStringField);
            Register("Int", CreateIntField);
            Register("Float", CreateFloatField);
            Register("Bool", CreateBoolField);
        }

        /// <summary>Codec 식별자에 맞는 입력 필드 생성기를 등록하거나 교체합니다.</summary>
        public static void Register(string codecId, DrawerFactory factory)
        {
            if (string.IsNullOrWhiteSpace(codecId))
            {
                throw new ArgumentException("코덱 식별자가 필요합니다.", nameof(codecId));
            }

            factories[codecId] = factory ?? throw new ArgumentNullException(nameof(factory), "인수 입력 요소 생성 함수가 필요합니다.");
        }

        /// <summary>입력 필드 생성기를 반환하며 enum과 Unity 객체 타입은 처음 요청할 때 만듭니다.</summary>
        public static bool TryGet(string codecId, out DrawerFactory factory)
        {
            if (string.IsNullOrWhiteSpace(codecId))
            {
                factory = null;
                return false;
            }

            if (factories.TryGetValue(codecId, out factory))
            {
                return true;
            }

            if (TryGetTypeArgument(codecId, "Enum<", out Type enumType) && enumType.IsEnum)
            {
                RegisterEnumDrawerMethod.MakeGenericMethod(enumType).Invoke(null, new object[] { codecId });
                return factories.TryGetValue(codecId, out factory);
            }

            if (TryGetTypeArgument(codecId, "Object<", out Type objectType)
                && typeof(UnityEngine.Object).IsAssignableFrom(objectType))
            {
                RegisterObjectDrawerMethod.MakeGenericMethod(objectType).Invoke(null, new object[] { codecId });
                return factories.TryGetValue(codecId, out factory);
            }

            factory = null;
            return false;
        }

        /// <summary>동적으로 생성하는 입력 필드에 사용할 타입별 식별자를 만듭니다.</summary>
        public static string GetCodecId(string prefix, Type type)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("코덱 접두사가 필요합니다.", nameof(prefix));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type), "코덱 식별자를 만들 파라미터 타입이 필요합니다.");
            }

            return $"{prefix}<{type.AssemblyQualifiedName}>";
        }

        private static MethodInfo RegisterEnumDrawerMethod => typeof(ArgumentDrawerRegistry).GetMethod(
            nameof(RegisterEnumDrawer),
            BindingFlags.Static | BindingFlags.Public);

        private static MethodInfo RegisterObjectDrawerMethod => typeof(ArgumentDrawerRegistry).GetMethod(
            nameof(RegisterObjectDrawer),
            BindingFlags.Static | BindingFlags.Public);

        private static bool TryGetTypeArgument(string codecId, string prefix, out Type type)
        {
            type = null;
            if (!codecId.StartsWith(prefix, StringComparison.Ordinal) || !codecId.EndsWith(">", StringComparison.Ordinal))
            {
                return false;
            }

            string typeName = codecId.Substring(prefix.Length, codecId.Length - prefix.Length - 1);
            type = FindType(typeName);
            return type != null;
        }

        private static Type FindType(string typeName)
        {
            Type type = Type.GetType(typeName, throwOnError: false);
            if (type != null)
            {
                return type;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static VisualElement CreateStringField(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
            MethodParameterDescriptor parameter, object decodedValue)
        {
            var field = new TextField(parameter.DisplayName)
            {
                value = (string)decodedValue
            };

            field.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit($"Change {parameter.DisplayName}", () =>
                {
                    argument.SerializedValue = change.newValue;
                    argument.ObjectValue = null;
                });
            });

            return field;
        }

        private static VisualElement CreateIntField(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
            MethodParameterDescriptor parameter, object decodedValue)
        {
            var field = new IntegerField(parameter.DisplayName)
            {
                value = (int)decodedValue
            };

            field.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit($"Change {parameter.DisplayName}", () =>
                {
                    argument.SerializedValue = MethodArgumentCodec.SerializeScalar(change.newValue, parameter.ParameterType, MethodArgumentKind.Integer);
                    argument.ObjectValue = null;
                });
            });

            return field;
        }

        private static VisualElement CreateFloatField(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
            MethodParameterDescriptor parameter, object decodedValue)
        {
            var field = new FloatField(parameter.DisplayName)
            {
                value = (float)decodedValue
            };

            field.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit($"Change {parameter.DisplayName}", () =>
                {
                    argument.SerializedValue = MethodArgumentCodec.SerializeScalar(change.newValue, parameter.ParameterType, MethodArgumentKind.FloatingPoint);
                    argument.ObjectValue = null;
                });
            });

            return field;
        }

        private static VisualElement CreateBoolField(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
            MethodParameterDescriptor parameter, object decodedValue)
        {
            var field = new Toggle(parameter.DisplayName)
            {
                value = (bool)decodedValue
            };

            field.RegisterValueChangedCallback(change =>
            {
                editHandler.ApplyDataEdit($"Change {parameter.DisplayName}", () =>
                {
                    argument.SerializedValue = MethodArgumentCodec.SerializeScalar(change.newValue, parameter.ParameterType, MethodArgumentKind.Boolean);
                    argument.ObjectValue = null;
                });
            });

            return field;
        }

        /// <summary>지원하는 enum 인수 타입의 UI 입력 필드를 등록합니다.</summary>
		public static void RegisterEnumDrawer<TEnum>(string codecId) where TEnum : struct, Enum
        {
			Register(codecId, (editHandler, argument, parameter, decodedValue) =>
            {
                Enum currentEnum = (Enum)decodedValue;
                if (typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false))
                {
                    var field = new EnumFlagsField(parameter.DisplayName, currentEnum);
                    field.RegisterValueChangedCallback(change => RecordEnumChange(editHandler, argument, parameter, change.newValue));
                    return field;
                }

                var enumField = new EnumField(parameter.DisplayName, currentEnum);
                enumField.RegisterValueChangedCallback(change => RecordEnumChange(editHandler, argument, parameter, change.newValue));
                return enumField;
            });
        }

        /// <summary>Unity 객체 인수 타입에 에셋만 선택할 수 있는 객체 선택기를 등록합니다.</summary>
        public static void RegisterObjectDrawer<TObject>(string codecId) where TObject : UnityEngine.Object
        {
			Register(codecId, (editHandler, argument, parameter, decodedValue) =>
            {
                var field = new ObjectField(parameter.DisplayName)
                {
                    objectType = typeof(TObject),
                    allowSceneObjects = false,
                    value = decodedValue as TObject
                };

                field.RegisterValueChangedCallback(change =>
                {
                    editHandler.ApplyDataEdit($"Change {parameter.DisplayName}", () =>
                    {
                        argument.SerializedValue = string.Empty;
                        argument.ObjectValue = change.newValue;
                    });
                });

                return field;
            });
        }

        private static void RecordEnumChange(
            NodeInspectorEditHandler editHandler,
            MethodArgumentData argument,
            MethodParameterDescriptor parameter,
            Enum value)
        {
            editHandler.ApplyDataEdit($"Change {parameter.DisplayName}", () =>
            {
                argument.SerializedValue = MethodArgumentCodec.SerializeScalar(value, parameter.ParameterType, MethodArgumentKind.Enum);
                argument.ObjectValue = null;
            });
        }
    }
}
