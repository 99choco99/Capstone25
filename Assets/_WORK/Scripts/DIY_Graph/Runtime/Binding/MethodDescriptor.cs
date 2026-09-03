using System;
using System.Collections.Generic;
using System.Reflection;

namespace UniversalGraph
{
    /// <summary>Attribute가 붙은 메서드 한 개의 공통 설명서</summary>
    public abstract class MethodDescriptor
    {
        protected MethodDescriptor(
            string key,
            MethodKind kind,
            Type declaringType,
            string methodName,
            bool isStatic,
            MethodInfo method,
            MethodParameterDescriptor[] parameters,
            GeneratedMethodInvoker generatedInvoker)
        {
            Key = key;
            Kind = kind;
            DeclaringType = declaringType;
            MethodName = methodName;
            IsStatic = isStatic;
            MethodInfo = method;
            Parameters = parameters ?? Array.Empty<MethodParameterDescriptor>();

            List<MethodParameterDescriptor> serializedParameters = new ();
            foreach (MethodParameterDescriptor descriptor in Parameters)
            {
                if (descriptor.Source == MethodParameterSource.Serialized)
                {
                    serializedParameters.Add(descriptor);
                }
            }
            SerializedParameters = serializedParameters;

            GeneratedInvoker = generatedInvoker;
            DisplayName = $"{Key}  {DeclaringType?.Name}.{MethodName}";
        }

        //================================ 메서드 식별(어떤 메서드인지) =====================================
        /// <summary>메서드를 찾기 위한 고유 키</summary>
        public string Key { get; }

        /// <summary>메서드가 Action인지 Condition인지 구분</summary>
        public MethodKind Kind { get; }


        //================================ 원본 메서드 정보 =====================================

        /// <summary>메서드가 선언된 클래스 타입</summary>
        public Type DeclaringType { get; }

        /// <summary>메서드 이름</summary>
        public string MethodName { get; }

        /// <summary>static 메서드인지?</summary>
        public bool IsStatic { get; }

        //================================ 파라미터 정의 =====================================
        /// <summary>메서드의 전체 파라미터 정보</summary>
        public IReadOnlyList<MethodParameterDescriptor> Parameters { get; }

        /// <summary>그래프에서 입력한 파라미터 정보</summary>
        public IReadOnlyList<MethodParameterDescriptor> SerializedParameters { get; }



        //================================ 호출 방식 =====================================

        /// <summary>Reflection 으로 호출</summary>
        public MethodInfo MethodInfo { get; }

        /// <summary>Generator가 미리 만든 직접 호출 함수</summary>
        internal GeneratedMethodInvoker GeneratedInvoker { get; }


        //================================ 에디터 표시용 =====================================

        /// <summary>그래프 드롭다운에서 표시될 이름</summary>
        public string DisplayName { get; protected set; }

        /// <summary>로그에 사용할 클래스 전체 이름과 메서드 이름을 반환</summary>
        public string QualifiedMethodName => $"{DeclaringType?.FullName}.{MethodName}";
    }
}
