using System;
using UnityEngine;

namespace UniversalGraph
{
	[Serializable]
	public sealed class DialogueWaitSignalNodeData : NodeBaseData
	{
		[SerializeField]
		private string signalKey = string.Empty;

		public string SignalKey
		{
			get => signalKey;
			set => signalKey = value?.Trim() ?? string.Empty;
		}
	}
}
