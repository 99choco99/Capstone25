using System;
using UnityEngine;

namespace UniversalGraph
{
	[Serializable]
	public sealed class DialogueArgumentData
	{
		public string ParameterId;

		public string DeclaredTypeId;

		public DialogueArgumentKind Kind;

		public string SerializedValue;

		public UnityEngine.Object ObjectValue;
	}
}

