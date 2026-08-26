using System;
using System.Collections.Generic;

namespace UniversalGraph
{
    /// <summary>노드에서 선택한 Attribute 메서드 키와 파라미터 값을 함께 저장</summary>
    [Serializable]
    public sealed class MethodCallData
    {
        /// <summary>실행할 Attribute 메서드를 찾는 키</summary>
        public string Key = string.Empty;

        /// <summary>메서드 파라미터에 대한 정보들</summary>
        public List<MethodArgumentData> Arguments = new();
    }
}
