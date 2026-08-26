using System;
using UnityEngine.Scripting;

namespace UniversalGraph
{
	/// <summary>bool 반환 메서드를 그래프에서 선택할 수 있는 Dialogue Condition으로 공개합니다.</summary>
	[RequireAttributeUsages]
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class DialogueConditionAttribute : PreserveAttribute
	{
		public string Key { get; }

		/// <summary>인스턴스 메서드를 찾을 대상입니다. Global 대상은 static 메서드여야 합니다.</summary>
		public DialogueTarget Target { get; set; } = DialogueTarget.Speaker;

		/// <summary>프로젝트 전체에서 고정적으로 사용할 키를 가진 Condition Attribute를 만듭니다.</summary>
		public DialogueConditionAttribute(string key)
		{
			Key = key;
		}
	}
}
