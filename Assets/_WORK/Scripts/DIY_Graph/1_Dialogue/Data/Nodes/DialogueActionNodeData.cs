using System;

namespace UniversalGraph
{
	[Serializable]
	public sealed class DialogueActionNodeData : NodeBaseData
	{
		/// <summary>
		/// 실행할 이벤트
		/// </summary>
		public MethodCallData Event = new();
	}
}
