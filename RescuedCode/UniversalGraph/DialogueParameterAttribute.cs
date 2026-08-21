using System;
using UnityEngine.Scripting;

namespace UniversalGraph
{
	[RequireAttributeUsages]
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
	public sealed class DialogueParameterAttribute : Attribute
	{
		public string Id { get; }

		public DialogueParameterAttribute(string id)
		{
			Id = id;
		}
	}
}
