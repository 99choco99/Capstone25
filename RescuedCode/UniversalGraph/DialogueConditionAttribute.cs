using System;
using UnityEngine.Scripting;

namespace UniversalGraph
{
	[RequireAttributeUsages]
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class DialogueConditionAttribute : PreserveAttribute
	{
		public string Key { get; }

		public DialogueTarget Target { get; set; } = DialogueTarget.Speaker;


		public DialogueConditionAttribute(string key)
		{
			Key = key;
		}
	}
}
