using System;
using UnityEngine;

namespace UniversalGraph
{
    /// <summary>노드에 저장되는 Attribute 메서드 파라미터 값 하나
	/// <para>그래프에서 직접 입력하는 파라미터마다 하나씩 생성됩니다.</para>
    [Serializable]
	public sealed class MethodArgumentData
	{
        /// <summary>
		/// 어느 파라미터의 값인지 구분
		/// </summary>
        public string ParameterId;

        /// <summary>
		/// 메서드가 요구했던 정확한 타입을 기록
		/// </summary>
        public string DeclaredTypeId;

        /// <summary>
        /// 값을 어떤 방식으로 저장하고 인스펙터에 무엇을 그릴지
        /// </summary>
        public MethodArgumentKind Kind;

        /// <summary>
        /// 값을 문자열 형태로 저장해
        /// </summary>
        public string SerializedValue;

        /// <summary>
        /// Unity 에셋 참조를 저장, 문자열로 넣을 수 없기 때문
        /// </summary>
		public UnityEngine.Object ObjectValue;
	}
}

