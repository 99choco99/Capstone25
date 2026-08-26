using System;
using System.Collections.Generic;

namespace UniversalGraph
{
    /// <summary>표시할 선택지 목록을 저장</summary>
    [Serializable]
    public sealed class DialogueChoiceNodeData : NodeBaseData
    {
        /// <summary>표시 가능한 선택지가 없을 때 등 사용하는 출력 포트 이름</summary>
        public const string DefaultPortName = "Default";

        /// <summary>한 번에 함께 보여줄 선택지 목록</summary>
        public List<DialogueChoiceData> Choices = new();
    }
}
