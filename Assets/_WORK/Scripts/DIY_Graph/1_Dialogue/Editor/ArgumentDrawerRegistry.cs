using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>
    /// Provides UI Toolkit drawers for the serializable argument types supported by dialogue methods.
    /// The registry is editor-only; values are still encoded by <see cref="DialogueArgumentCodec"/>.
    /// </summary>
    public static class ArgumentDrawerRegistry
    {
        private static readonly Dictionary<string, DrawerFactory> factories = new Dictionary<string, DrawerFactory>(StringComparer.Ordinal);

        static ArgumentDrawerRegistry()
        {
            Register("String", CreateStringField);
            Register("Int", CreateIntField);
            Register("Float", CreateFloatField);
            Register("Bool", CreateBoolField);
        }

        /// <summary>Registers or replaces a drawer for a codec identifier.</summary>
        public static void Register(string codecId, DrawerFactory factory)
        {
            if (string.IsNullOrWhiteSpace(codecId))
            {
                throw new ArgumentException("A codec identifier is required.", nameof(codecId));
            }

            factories[codecId] = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>Returns a drawer, creating an enum or Unity object drawer on first use.</summary>
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

        /// <summary>Builds the type-specific identifier used for dynamically generated drawers.</summary>
        public static string GetCodecId(string prefix, Type type)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("A codec prefix is required.", nameof(prefix));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
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

        private static VisualElement CreateStringField(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
        {
            var field = new TextField(parameter.DisplayName)
            {
                value = argument.SerializedValue ?? string.Empty
            };

            field.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit($"Change {parameter.DisplayName}", () =>
                {
                    argument.SerializedValue = change.newValue;
                    argument.ObjectValue = null;
                });
            });

            return field;
        }

        private static VisualElement CreateIntField(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
        {
            int.TryParse(argument.SerializedValue, out int value);
            var field = new IntegerField(parameter.DisplayName)
            {
                value = value
            };

            field.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit($"Change {parameter.DisplayName}", () =>
                {
                    argument.SerializedValue = DialogueArgumentCodec.SerializeScalar(change.newValue, parameter.ParameterType, DialogueArgumentKind.Integer);
                    argument.ObjectValue = null;
                });
            });

            return field;
        }

        private static VisualElement CreateFloatField(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
        {
            float.TryParse(argument.SerializedValue, out float value);
            var field = new FloatField(parameter.DisplayName)
            {
                value = value
            };

            field.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit($"Change {parameter.DisplayName}", () =>
                {
                    argument.SerializedValue = DialogueArgumentCodec.SerializeScalar(change.newValue, parameter.ParameterType, DialogueArgumentKind.FloatingPoint);
                    argument.ObjectValue = null;
                });
            });

            return field;
        }

        private static VisualElement CreateBoolField(NodeInspectorContext context, DialogueArgumentData argument, DialogueParameterDescriptor parameter)
        {
            bool.TryParse(argument.SerializedValue, out bool value);
            var field = new Toggle(parameter.DisplayName)
            {
                value = value
            };

            field.RegisterValueChangedCallback(change =>
            {
                context.ApplyEdit($"Change {parameter.DisplayName}", () =>
                {
                    argument.SerializedValue = DialogueArgumentCodec.SerializeScalar(change.newValue, parameter.ParameterType, DialogueArgumentKind.Boolean);
                    argument.ObjectValue = null;
                });
            });

            return field;
        }

        /// <summary>Registers an enum UI drawer for a supported enum argument type.</summary>
		public static void RegisterEnumDrawer<TEnum>(string codecId) where TEnum : struct, Enum
        {
            Register(codecId, (context, argument, parameter) =>
            {
                if (!Enum.TryParse(argument.SerializedValue, out TEnum currentValue))
                {
                    currentValue = default;
                }

                Enum currentEnum = (Enum)(object)currentValue;
                if (typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false))
                {
                    var field = new EnumFlagsField(parameter.DisplayName, currentEnum);
                    field.RegisterValueChangedCallback(change => RecordEnumChange(context, argument, parameter, change.newValue));
                    return field;
                }

                var enumField = new EnumField(parameter.DisplayName, currentEnum);
                enumField.RegisterValueChangedCallback(change => RecordEnumChange(context, argument, parameter, change.newValue));
                return enumField;
            });
        }

        /// <summary>Registers an asset-only object picker for a Unity object argument type.</summary>
        public static void RegisterObjectDrawer<TObject>(string codecId) where TObject : UnityEngine.Object
        {
            Register(codecId, (context, argument, parameter) =>
            {
                var field = new ObjectField(parameter.DisplayName)
                {
                    objectType = typeof(TObject),
                    allowSceneObjects = false,
                    value = argument.ObjectValue as TObject
                };

                field.RegisterValueChangedCallback(change =>
                {
                    context.ApplyEdit($"Change {parameter.DisplayName}", () =>
                    {
                        argument.SerializedValue = string.Empty;
                        argument.ObjectValue = change.newValue;
                    });
                });

                return field;
            });
        }

        private static void RecordEnumChange(
            NodeInspectorContext context,
            DialogueArgumentData argument,
            DialogueParameterDescriptor parameter,
            Enum value)
        {
            context.ApplyEdit($"Change {parameter.DisplayName}", () =>
            {
                argument.SerializedValue = DialogueArgumentCodec.SerializeScalar(value, parameter.ParameterType, DialogueArgumentKind.Enum);
                argument.ObjectValue = null;
            });
        }
    }
}
