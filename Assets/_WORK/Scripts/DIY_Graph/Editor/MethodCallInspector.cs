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
    public static class MethodCallInspector
    {
		/// <summary>인스펙터에서 실행할 Attribute 메서드를 선택하고 인수를 입력할 수 있는 칸을 생성</summary>
        public static VisualElement Create(NodeInspectorEditHandler editHandler, string title, MethodCallData methodCall, IReadOnlyList<MethodDescriptor> methods, Action onMethodChanged = null)
        {
            VisualElement root = new ();

            Label titleLabel = new (title);
            root.Add(titleLabel);

            if (methodCall == null)
            {
				root.Add(new HelpBox($"'{title}' 메서드 호출 데이터가 없습니다.", HelpBoxMessageType.Error));
                return root;
            }

            //노드가 새거면 Empty, 아니면 불러오는거니까 노드의 것을 GetKey
            string currentKey = methodCall.Key;
            IReadOnlyList<MethodDescriptor> availableMethods = methods ?? Array.Empty<MethodDescriptor>();
            
            List<string> keys = new () { string.Empty };
            keys.AddRange(availableMethods.Where(method => method != null).Select(method => method.Key));
            if (!string.IsNullOrWhiteSpace(currentKey) && !keys.Contains(currentKey))
            {
                keys.Add(currentKey);
            }

            //드롭다운 생성
            int index = Math.Max(0, keys.FindIndex(key => key == currentKey));
            PopupField<string> keyField = new ("Method", keys, index, FormatKey, FormatKey);
            root.Add(keyField);

            //선택된 메서드의 파라미터 값을 넣을 공간 마련
            VisualElement argumentsRoot = new ();
            root.Add(argumentsRoot);

            //드롭다운으로 설정된 메서드를 노드 데이터에 set
            keyField.RegisterValueChangedCallback(change =>
            {
                string selectedKey = change.newValue ?? string.Empty;
                editHandler.ApplyDataEdit("Change method", () =>
                {
                    MethodDescriptor descriptor = FindMethod(selectedKey);
                    methodCall.Key = selectedKey;
                    methodCall.Arguments = descriptor != null ? MethodArgumentCodec.CreateDefaultArgumentData(descriptor) : new List<MethodArgumentData>();
                    onMethodChanged?.Invoke();
                    RedrawArgumentFields();
                });
            });

            RedrawArgumentFields();
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

            void RedrawArgumentFields()
            {
                argumentsRoot.Clear();

                string key = methodCall.Key;
                if (string.IsNullOrWhiteSpace(key)) { return; }
                MethodDescriptor descriptor = FindMethod(key);
                if (descriptor == null)
                {
                    argumentsRoot.Add(new HelpBox(
                        $"'{key}'은(는) Attribute 메서드로 등록되지 않았습니다. 외부 연결 키일 수 있으므로 값을 유지합니다.", HelpBoxMessageType.Warning));
                    return;
                }

                List<MethodArgumentData> savedArguments = methodCall.Arguments;
                if (!MethodArgumentCodec.TryDecodeAllArgumentData(savedArguments, descriptor, out object[] values, out string error))
                {
                    argumentsRoot.Add(new HelpBox($"저장된 인수가 현재 메서드 시그니처와 일치하지 않습니다.\n{error}", HelpBoxMessageType.Error));
                    argumentsRoot.Add(new Button(() =>
                    {
                        editHandler.ApplyDataEdit("Repair method arguments", () =>
                        {
                            methodCall.Arguments = MethodArgumentCodec.RepairArguments(methodCall.Arguments, descriptor);
                            RedrawArgumentFields();
                        });
                    })
                    {
                        text = "인수 다시 만들기 (호환되는 값 유지)"
                    });
                    return;
                }

                if (descriptor.SerializedParameters.Count == 0)
                {
                    argumentsRoot.Add(new HelpBox("이 메서드에는 그래프에서 입력할 인수가 없습니다.", HelpBoxMessageType.Info));
                    return;
                }

                foreach (MethodParameterDescriptor parameterDescriptor in descriptor.SerializedParameters)
                {
                    MethodArgumentData argument = savedArguments?.FirstOrDefault(candidate => candidate != null
                        && candidate.ParameterId == parameterDescriptor.ParameterId);
                    argumentsRoot.Add(argument == null
                        ? new HelpBox($"인수 '{parameterDescriptor.ParameterId}'가 저장되어 있지 않습니다.", HelpBoxMessageType.Error)
                        : CreateArgumentField(editHandler, argument, parameterDescriptor, values[parameterDescriptor.ParameterIndex]));
                }
            }

            MethodDescriptor FindMethod(string key)
            {
                return availableMethods.FirstOrDefault(method => method != null && method.Key == key);
            }
        }

        /// <summary>검증된 직렬화 인수 하나에 맞는 UI 입력 필드를 만듭니다.</summary>
        private static VisualElement CreateArgumentField(NodeInspectorEditHandler editHandler, MethodArgumentData argument, MethodParameterDescriptor descriptor, object value)
        {
            if (MethodArgumentFieldRegistry.TryGet(descriptor.ArgumentKind, out MethodArgumentFieldFactory fieldFactory))
            {
                return fieldFactory(editHandler, argument, descriptor, value);
            }

            return new HelpBox($"'{descriptor.ParameterType.Name}' 타입을 표시할 인스펙터 입력 요소가 없습니다.", HelpBoxMessageType.Error);
        }
    }
}
