using System;

namespace UniversalGraph
{
	[Serializable]
	public class DialogueChoiceData
	{
		public string PortName;

		public string ChoiceText;

        /// <summary>이 선택지 버튼을 화면에 보여줄까 말까? (버튼 노출 조건)</summary>
        public MethodCallData VisibilityCondition = new();

        /// <summary>
        /// 이 선택지 버튼을 눌렀을 때 무슨 일을 할까?(버튼 클릭 액션)
        /// </summary>
        public MethodCallData ChoiceEvent = new();
	}
}
