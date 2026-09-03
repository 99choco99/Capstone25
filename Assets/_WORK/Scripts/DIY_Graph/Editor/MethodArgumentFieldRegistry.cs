using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalGraph.Editor
{
    public delegate VisualElement MethodArgumentFieldFactory(NodeInspectorEditHandler editHandler, MethodArgumentData argument, MethodParameterDescriptor descriptor, object initialValue);

    /// <summary>
    /// 메서드가 지원하는 직렬화 인수 타입에 맞는 UI Toolkit 입력 필드를 제공합니다.
    /// 이 등록부는 에디터 전용이며 실제 값 변환은 <see cref="MethodArgumentCodec"/>이 담당합니다.
    /// </summary>
    public static class MethodArgumentFieldRegistry
    {
        private static readonly Dictionary<MethodArgumentKind, MethodArgumentFieldFactory> factories = new();

        static MethodArgumentFieldRegistry()
        {
            Register(MethodArgumentKind.String, CreateStringField);
            Register(MethodArgumentKind.Boolean, CreateBoolField);
            Register(MethodArgumentKind.Integer, CreateIntField);
            Register(MethodArgumentKind.FloatingPoint, CreateFloatField);
            Register(MethodArgumentKind.Enum, CreateEnumField);
            Register(MethodArgumentKind.UnityObject, CreateObjectField);
        }

        /// <summary>파라미터 종류에 맞는 입력 필드 생성기를 등록하거나 교체합니다.</summary>
        public static void Register(MethodArgumentKind kind, MethodArgumentFieldFactory factory)
        {
            factories[kind] = factory ?? throw new ArgumentNullException(nameof(factory), "인수 입력 요소 생성 함수가 필요합니다.");
        }

        /// <summary>파라미터 종류에 맞는 입력 필드 생성기를 반환합니다.</summary>
        public static bool TryGet(MethodArgumentKind kind, out MethodArgumentFieldFactory factory)
        {
            return factories.TryGetValue(kind, out factory);
        }

        private static VisualElement CreateStringField(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
            MethodParameterDescriptor descriptor, object initialValue)
        {
            var field = new TextField(descriptor.DisplayName)
            {
                value = (string)initialValue
            };

			field.RegisterValueChangedCallback(change =>
			{
				if (!ApplyArgumentChange(editHandler, argument, descriptor, change.newValue))
				{
					field.SetValueWithoutNotify(change.previousValue);
				}
			});

            return field;
        }

        private static VisualElement CreateIntField(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
            MethodParameterDescriptor descriptor, object initialValue)
        {
            var field = new IntegerField(descriptor.DisplayName)
            {
                value = (int)initialValue
            };

			field.RegisterValueChangedCallback(change =>
			{
				if (!ApplyArgumentChange(editHandler, argument, descriptor, change.newValue))
				{
					field.SetValueWithoutNotify(change.previousValue);
				}
			});

            return field;
        }

        private static VisualElement CreateFloatField(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
            MethodParameterDescriptor descriptor, object initialValue)
        {
            var field = new FloatField(descriptor.DisplayName)
            {
                value = (float)initialValue
            };

			field.RegisterValueChangedCallback(change =>
			{
				if (!ApplyArgumentChange(editHandler, argument, descriptor, change.newValue))
				{
					field.SetValueWithoutNotify(change.previousValue);
				}
			});

            return field;
        }

        private static VisualElement CreateBoolField(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
            MethodParameterDescriptor descriptor, object initialValue)
        {
            var field = new Toggle(descriptor.DisplayName)
            {
                value = (bool)initialValue
            };

			field.RegisterValueChangedCallback(change =>
			{
				if (!ApplyArgumentChange(editHandler, argument, descriptor, change.newValue))
				{
					field.SetValueWithoutNotify(change.previousValue);
				}
			});

            return field;
        }

        /// <summary>enum 인수 타입에 맞는 입력 필드를 만듭니다.</summary>
		private static VisualElement CreateEnumField(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
            MethodParameterDescriptor descriptor, object initialValue)
        {
            Enum currentEnum = (Enum)initialValue;
			if (descriptor.ParameterType.IsDefined(typeof(FlagsAttribute), inherit: false))
			{
				var field = new EnumFlagsField(descriptor.DisplayName, currentEnum);
				field.RegisterValueChangedCallback(change =>
				{
					if (!ApplyArgumentChange(editHandler, argument, descriptor, change.newValue))
					{
						field.SetValueWithoutNotify(change.previousValue);
					}
				});
				return field;
			}

			var enumField = new EnumField(descriptor.DisplayName, currentEnum);
			enumField.RegisterValueChangedCallback(change =>
			{
				if (!ApplyArgumentChange(editHandler, argument, descriptor, change.newValue))
				{
					enumField.SetValueWithoutNotify(change.previousValue);
				}
			});
			return enumField;
        }

        /// <summary>Unity 객체 인수 타입에 에셋만 선택할 수 있는 객체 선택기를 만듭니다.</summary>
        private static VisualElement CreateObjectField(NodeInspectorEditHandler editHandler, MethodArgumentData argument,
            MethodParameterDescriptor descriptor, object initialValue)
        {
            var field = new ObjectField(descriptor.DisplayName)
            {
                objectType = descriptor.ParameterType,
                allowSceneObjects = false,
                value = initialValue as UnityEngine.Object
            };

			field.RegisterValueChangedCallback(change =>
			{
				if (!ApplyArgumentChange(editHandler, argument, descriptor, change.newValue))
				{
					field.SetValueWithoutNotify(change.previousValue);
				}
			});

            return field;
        }

		private static bool ApplyArgumentChange(
			NodeInspectorEditHandler editHandler,
			MethodArgumentData argument,
			MethodParameterDescriptor descriptor,
			object value)
		{
			bool succeeded = false;
			editHandler.ApplyDataEdit($"Change {descriptor.DisplayName}", () =>
			{
				succeeded = MethodArgumentCodec.TryEncodeArgumentData(argument, descriptor, value, out string error);
				if (!succeeded)
				{
					Debug.LogError(error);
				}
			});
			return succeeded;
		}
    }
}
