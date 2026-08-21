using System;
using UnityEngine.Scripting;

namespace UniversalGraph
{
	[RequireAttributeUsages]
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class DialogueActionAttribute : PreserveAttribute
	{
		public string Key { get; }

		public DialogueTarget Target { get; set; } = DialogueTarget.Speaker;


		public DialogueActionAttribute(string key)
		{
			Key = key;
		}
	}
}
