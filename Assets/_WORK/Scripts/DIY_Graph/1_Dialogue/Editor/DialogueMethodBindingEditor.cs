using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalGraph.Editor;

namespace UniversalGraph.Dialogue.Editor
{
    /// <summary>
    /// Draws an action or condition binding and its serialized arguments in a node inspector.
    /// </summary>
    internal static class DialogueMethodBindingEditor
    {
        /// <summary>Creates the binding editor for one node action or choice action.</summary>
        public static VisualElement Create(
            NodeInspectorContext context,
            string title,
            DialogueMethodKind kind,
            DialogueMethodBindingAccessor binding)
        {
            var root = new VisualElement();
            root.AddToClassList("action-editor");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("action-title");
            root.Add(titleLabel);

            string currentKey = binding.GetKey?.Invoke() ?? string.Empty;
            var choices = new List<string> { string.Empty };
            choices.AddRange(DialogueMethodCatalog.GetMethods(kind).Select(method => method.Key));
            if (!string.IsNullOrWhiteSpace(currentKey) && !choices.Contains(currentKey, StringComparer.Ordinal))
            {
                choices.Add(currentKey);
            }

            int selectedIndex = Math.Max(0, choices.FindIndex(key => string.Equals(key, currentKey, StringComparison.Ordinal)));
            var keyField = new PopupField<string>(
                kind == DialogueMethodKind.Action ? "Action" : "Condition",
                choices,
                selectedIndex,
                FormatKey,
                FormatKey);
            root.Add(keyField);

            var parametersRoot = new VisualElement();
            parametersRoot.AddToClassList("method-parameters");
            root.Add(parametersRoot);

            keyField.RegisterValueChangedCallback(change =>
            {
                string selectedKey = change.newValue ?? string.Empty;
                context.ApplyEdit($"Change {kind} method", () =>
                {
                    binding.SetKey?.Invoke(selectedKey);
                    binding.SetArguments?.Invoke(
                        DialogueMethodCatalog.TryGetMethod(kind, selectedKey, out DialogueMethodDescriptor descriptor)
                            ? DialogueArgumentCodec.CreateDefaultArguments(descriptor)
                            : new List<DialogueArgumentData>());
                    binding.SetLegacyParameter?.Invoke(string.Empty);
                    RedrawParameters();
                });
            });

            RedrawParameters();
            return root;

            string FormatKey(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return "None";
                }

                return DialogueMethodCatalog.TryGetMethod(kind, key, out DialogueMethodDescriptor descriptor)
                    ? descriptor.DisplayName
                    : $"<Missing> {key}";
            }

            void RedrawParameters()
            {
                parametersRoot.Clear();

                string key = binding.GetKey?.Invoke() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(key))
                {
                    return;
                }

                if (!DialogueMethodCatalog.TryGetMethod(kind, key, out DialogueMethodDescriptor descriptor))
                {
                    parametersRoot.Add(new HelpBox(
                        $"'{key}' is not registered. The saved key is kept until another method is selected.",
                        HelpBoxMessageType.Error));
                    return;
                }

                List<DialogueArgumentData> arguments = binding.GetArguments?.Invoke();
                string legacyParameter = binding.GetLegacyParameter?.Invoke() ?? string.Empty;
                if (!DialogueArgumentCodec.TryValidateArguments(arguments, legacyParameter, descriptor, out string error))
                {
                    parametersRoot.Add(new HelpBox($"Saved arguments do not match the current method signature.\n{error}", HelpBoxMessageType.Error));
                    parametersRoot.Add(new Button(() =>
                    {
                        context.ApplyEdit("Rebuild dialogue arguments", () =>
                        {
                            binding.SetArguments?.Invoke(DialogueArgumentCodec.RebuildArguments(binding.GetArguments?.Invoke(), descriptor, preserveCompatibleValues: true));
                            binding.SetLegacyParameter?.Invoke(string.Empty);
                            RedrawParameters();
                        });
                    })
                    {
                        text = "Rebuild Parameters (Preserve Compatible Values)"
                    });
                    return;
                }

                if ((arguments == null || arguments.Count == 0)
                    && descriptor.SerializedParameters.Count == 1
                    && descriptor.SerializedParameters[0].ParameterType == typeof(string))
                {
                    CreateLegacyStringField(descriptor.SerializedParameters[0], legacyParameter);
                    return;
                }

                if (descriptor.SerializedParameters.Count == 0)
                {
                    parametersRoot.Add(new HelpBox("This method does not require authorable arguments.", HelpBoxMessageType.Info));
                    return;
                }

                foreach (DialogueParameterDescriptor parameter in descriptor.SerializedParameters)
                {
                    DialogueArgumentData argument = arguments?.FirstOrDefault(candidate => candidate != null
                        && string.Equals(candidate.ParameterId, parameter.ParameterId, StringComparison.Ordinal));
                    parametersRoot.Add(argument == null
                        ? new HelpBox($"Missing saved value for parameter '{parameter.ParameterId}'.", HelpBoxMessageType.Error)
                        : CreateArgumentField(context, argument, parameter));
                }
            }

            void CreateLegacyStringField(DialogueParameterDescriptor parameter, string legacyParameter)
            {
                var field = new TextField(parameter.DisplayName)
                {
                    value = legacyParameter
                };
                field.RegisterValueChangedCallback(change =>
                {
                    context.ApplyEdit("Change legacy string parameter", () => binding.SetLegacyParameter?.Invoke(change.newValue));
                });
                parametersRoot.Add(field);
            }
        }

        /// <summary>Creates the appropriate UI control for one validated serialized argument.</summary>
        private static VisualElement CreateArgumentField(
            NodeInspectorContext context,
            DialogueArgumentData argument,
            DialogueParameterDescriptor parameter)
        {
            if (!DialogueArgumentCodec.TryDecode(argument, parameter, out object value, out string error))
            {
                return new HelpBox(error, HelpBoxMessageType.Error);
            }

            if (TryGetRegisteredDrawer(parameter, out DrawerFactory drawer))
            {
                return drawer(context, argument, parameter);
            }

            switch (parameter.Kind)
            {
                case DialogueArgumentKind.Integer:
                    return CreateIntegerField(context, argument, parameter, value);

                case DialogueArgumentKind.FloatingPoint:
                    return CreateFloatingPointField(context, argument, parameter, value);

                default:
                    return new HelpBox($"No inspector drawer is available for '{parameter.ParameterType.Name}'.", HelpBoxMessageType.Error);
            }
        }

        private static bool TryGetRegisteredDrawer(DialogueParameterDescriptor parameter, out DrawerFactory drawer)
        {
			drawer = null;
            string codecId = null;
            if (parameter.ParameterType == typeof(string))
            {
                codecId = "String";
            }
            else if (parameter.ParameterType == typeof(bool))
            {
                codecId = "Bool";
            }
            else if (parameter.ParameterType == typeof(int))
            {
                codecId = "Int";
            }
            else if (parameter.ParameterType == typeof(float))
            {
                codecId = "Float";
            }
            else if (parameter.ParameterType.IsEnum)
            {
                codecId = ArgumentDrawerRegistry.GetCodecId("Enum", parameter.ParameterType);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(parameter.ParameterType))
            {
                codecId = ArgumentDrawerRegistry.GetCodecId("Object", parameter.ParameterType);
            }

			return codecId != null && ArgumentDrawerRegistry.TryGet(codecId, out drawer);
        }

        private static VisualElement CreateIntegerField(
            NodeInspectorContext context,
            DialogueArgumentData argument,
            DialogueParameterDescriptor parameter,
            object value)
        {
            if (parameter.ParameterType == typeof(long))
            {
                var field = new LongField(parameter.DisplayName) { value = (long)value };
                field.RegisterValueChangedCallback(change => RecordScalar(context, argument, parameter, change.newValue));
                return field;
            }

            return CreateValidatedTextField(context, argument, parameter);
        }

        private static VisualElement CreateFloatingPointField(
            NodeInspectorContext context,
            DialogueArgumentData argument,
            DialogueParameterDescriptor parameter,
            object value)
        {
            if (parameter.ParameterType == typeof(double))
            {
                var field = new DoubleField(parameter.DisplayName) { value = (double)value };
                field.RegisterValueChangedCallback(change => RecordScalar(context, argument, parameter, change.newValue));
                return field;
            }

            return CreateValidatedTextField(context, argument, parameter);
        }

        private static VisualElement CreateValidatedTextField(
            NodeInspectorContext context,
            DialogueArgumentData argument,
            DialogueParameterDescriptor parameter)
        {
            var field = new TextField(parameter.DisplayName)
            {
                value = argument.SerializedValue ?? string.Empty,
                isDelayed = true
            };
            field.RegisterValueChangedCallback(change =>
            {
                if (CanDecode(argument, parameter, change.newValue))
                {
                    RecordScalar(context, argument, parameter, change.newValue);
                }
                else
                {
                    field.SetValueWithoutNotify(argument.SerializedValue ?? string.Empty);
                }
            });
            return field;
        }

        private static bool CanDecode(DialogueArgumentData original, DialogueParameterDescriptor parameter, string serializedValue)
        {
            var candidate = new DialogueArgumentData
            {
                ParameterId = original.ParameterId,
                DeclaredTypeId = original.DeclaredTypeId,
                Kind = original.Kind,
                SerializedValue = serializedValue,
                ObjectValue = original.ObjectValue
            };
            return DialogueArgumentCodec.TryDecode(candidate, parameter, out _, out _);
        }

        private static void RecordScalar(
            NodeInspectorContext context,
            DialogueArgumentData argument,
            DialogueParameterDescriptor parameter,
            object value)
        {
            string serializedValue = DialogueArgumentCodec.SerializeScalar(value, parameter.ParameterType, parameter.Kind);
            RecordScalar(context, argument, parameter, serializedValue);
        }

        private static void RecordScalar(
            NodeInspectorContext context,
            DialogueArgumentData argument,
            DialogueParameterDescriptor parameter,
            string serializedValue)
        {
            context.ApplyEdit($"Change {parameter.DisplayName}", () =>
            {
                argument.SerializedValue = serializedValue;
                argument.ObjectValue = null;
            });
        }
    }
}
