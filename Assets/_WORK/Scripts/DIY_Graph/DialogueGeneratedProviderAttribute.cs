using System;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace UniversalGraph
{
	[RequireAttributeUsages]
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed class DialogueGeneratedProviderAttribute : Attribute
	{
		public Type ProviderType { get; }

		public DialogueGeneratedProviderAttribute(Type providerType)
		{
			ProviderType = providerType;
		}
	}
}
