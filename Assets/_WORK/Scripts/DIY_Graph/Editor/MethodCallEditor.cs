using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalGraph.Editor
{
    /// <summary>
    /// 노드 인스펙터에 메서드 선택 드롭다운과 해당 메서드의 파라미터 입력 UI 전체를 만드는 클래스
    /// </summary>
    public static class MethodCallEditor
    {
        /// <summary>인스펙터에서 대화 진행시 실행할 함수를 바인드 할 수 있도록 칸을 생성하는 함수</summary>
        public static VisualElement Create(NodeInspectorEditHandler editHandler, string title, MethodCallData methodCall, IReadOnlyList<MethodDescriptor> methods, Action onKeyChanged = null)
        {
            VisualElement root = new ();
            root.AddToClassList("action-editor");

            Label titleLabel = new (title);
            titleLabel.AddToClassList("action-title");
            root.Add(titleLabel);

            //노드가 새거면 Empty, 아니면 불러오는거니까 노드의 것을 GetKey
            string currentKey = methodCall.Key;
            IReadOnlyList<MethodDescriptor> availableMethods = methods ?? Array.Empty<MethodDescriptor>();
            
            List<string> methodKeys = new () { string.Empty };
            methodKeys.AddRange(availableMethods.Where(method => method != null).Select(method => method.Key));
            if (!string.IsNullOrWhiteSpace(currentKey) && !methodKeys.Contains(currentKey))
            {
                methodKeys.Add(currentKey);
            }

            //드롭다운 생성
            int selectedIndex = Math.Max(0, methodKeys.FindIndex(key => key == currentKey));
            PopupField<string> keyField = new ("Method", methodKeys, selectedIndex, FormatKey, FormatKey);
            root.Add(keyField);

            //선택된 메서드의 파라미터 값을 넣을 공간 마련
            VisualElement parametersRoot = new ();
            parametersRoot.AddToClassList("method-parameters");
            root.Add(parametersRoot);

            //드롭다운으로 설정된 메서드를 노드 데이터에 set
            keyField.RegisterValueChangedCallback(change =>
            {
                string selectedKey = change.newValue ?? string.Empty;
                editHandler.ApplyDataEdit("Change method", () =>
                {
                    MethodDescriptor descriptor = FindMethod(selectedKey);
                    methodCall.Key = selectedKey;
                    methodCall.Arguments = descriptor != null ? MethodArgumentCodec.CreateDefaultArguments(descriptor) : new List<MethodArgumentData>();
                    onKeyChanged?.Invoke();
                    RedrawParametersUI();
                });
            });

            RedrawParametersUI();
            return root;

            //드롭다운에서 보여주는 방식을 정의
            string FormatKey(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return "None";
                }

                MethodDescriptor descriptor = FindMethod(key);
                return descriptor != null ? descriptor.DisplayName : $"<등록되지 않음> {key}";
            }

            void RedrawParametersUI()
            {
                parametersRoot.Clear();

                string key = methodCall.Key;
                if (string.IsNullOrWhiteSpace(key)) { return; }
                MethodDescriptor descriptor = FindMethod(key);
                if (descriptor == null)
                {
                    parametersRoot.Add(new HelpBox(
                        $"'{key}'은(는) Attribute 메서드로 등록되지 않았습니다. 외부 연결 키일 수 있으므로 값을 유지합니다.", HelpBoxMessageType.Warning));
                    return;
                }

                List<MethodArgumentData> arguments = methodCall.Arguments;
                if (!MethodArgumentCodec.TryValidateArguments(arguments, descriptor, out string error))
                {
                    parametersRoot.Add(new HelpBox($"저장된 인수가 현재 메서드 시그니처와 일치하지 않습니다.\n{error}", HelpBoxMessageType.Error));
                    parametersRoot.Add(new Button(() =>
                    {
                        editHandler.ApplyDataEdit("Rebuild method arguments", () =>
                        {
                            methodCall.Arguments = MethodArgumentCodec.RebuildArguments(methodCall.Arguments, descriptor, preserveCompatibleValues: true);
                            RedrawParametersUI();
                        });
                    })
                    {
                        text = "파라미터 다시 만들기 (호환되는 값 유지)"
                    });
                    return;
                }

                if (descriptor.SerializedParameters.Count == 0)
                {
                    parametersRoot.Add(new HelpBox("이 메서드에는 그래프에서 입력할 인수가 없습니다.", HelpBoxMessageType.Info));
                    return;
                }

                foreach (MethodParameterDescriptor parameter in descriptor.SerializedParameters)
                {
                    MethodArgumentData argument = arguments?.FirstOrDefault(candidate => candidate != null
                        && candidate.ParameterId == parameter.ParameterId);
                    parametersRoot.Add(argument == null
                        ? new HelpBox($"파라미터 '{parameter.ParameterId}'에 저장된 값이 없습니다.", HelpBoxMessageType.Error)
                        : CreateArgumentField(editHandler, argument, parameter));
                }
            }

            MethodDescriptor FindMethod(string key)
            {
                return availableMethods.FirstOrDefault(method => method != null && method.Key == key);
            }
        }

        /// <summary>검증된 직렬화 인수 하나에 맞는 UI 입력 필드를 만듭니다.</summary>
        private static VisualElement CreateArgumentField(NodeInspectorEditHandler editHandler, MethodArgumentData argument, MethodParameterDescriptor parameter)
        {
            if (!MethodArgumentCodec.TryDecode(argument, parameter, out object value, out string error))
            {
                return new HelpBox(error, HelpBoxMessageType.Error);
            }

            if (TryGetRegisteredDrawer(parameter, out DrawerFactory drawer))
            {
                return drawer(editHandler, argument, parameter, value);
            }

            return new HelpBox($"'{parameter.ParameterType.Name}' 타입을 표시할 인스펙터 입력 요소가 없습니다.", HelpBoxMessageType.Error);
        }

        private static bool TryGetRegisteredDrawer(MethodParameterDescriptor parameter, out DrawerFactory drawer)
        {
            string codecId = null;
            switch (parameter.Kind)
            {
            case MethodArgumentKind.String:
                codecId = "String";
                break;
            case MethodArgumentKind.Boolean:
                codecId = "Bool";
                break;
            case MethodArgumentKind.Integer:
                codecId = "Int";
                break;
            case MethodArgumentKind.FloatingPoint:
                codecId = "Float";
                break;
            case MethodArgumentKind.Enum:
                codecId = ArgumentDrawerRegistry.GetCodecId("Enum", parameter.ParameterType);
                break;
            case MethodArgumentKind.UnityObject:
                codecId = ArgumentDrawerRegistry.GetCodecId("Object", parameter.ParameterType);
                break;
            }

			return ArgumentDrawerRegistry.TryGet(codecId, out drawer);
        }
    }
}
