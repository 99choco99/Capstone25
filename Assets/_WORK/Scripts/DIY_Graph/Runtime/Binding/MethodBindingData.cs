using System;
using System.Collections.Generic;

namespace UniversalGraph
{
    /// <summary>그래프 노드와 Attribute 메서드를 연결하는 키와 전달 인수를 저장</summary>
    [Serializable]
    public sealed class MethodBindingData
    {
        /// <summary>실행할 Attribute 메서드를 찾는 키</summary>
        public string Key = string.Empty;

        /// <summary>메서드에 전달할 인수 목록</summary>
        public List<MethodArgumentData> Arguments = new();
    }
}
