using System;

namespace UniversalGraph
{
	[Serializable]
	public sealed class WaitSignalNodeData : NodeBaseData
	{
		public string SignalKey;

		public string GetNormalizedSignalKey()
		{
			return NormalizeSignalKey(SignalKey);
		}

		public static string NormalizeSignalKey(string signalKey)
		{
			return signalKey?.Trim() ?? string.Empty;
		}
	}
}
