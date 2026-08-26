using System;
using UnityEngine;

namespace UniversalGraph
{
	/// <summary>대화 그래프 에셋 안의 이름 있는 시작점 하나를 가리키는 이식 가능한 참조입니다.</summary>
	[Serializable]
	public struct DialogueReference
	{
		[Tooltip("재생할 Dialogue 그래프 에셋입니다.")]
		public DialogueContainer GraphAsset;

		[Tooltip("이름이 지정된 그래프 진입점입니다. 비어 있으면 기본 Entry를 사용합니다.")]
		[SerializeField]
		private string entryId;

		/// <summary>값을 읽고 저장할 때 빈 ID는 기본 Entry로 바꾸고 앞뒤 공백을 제거합니다.</summary>
		public string EntryId
		{
			get => string.IsNullOrWhiteSpace(entryId) ? DialogueStartNodeData.DefaultEntryId : entryId.Trim();
			set => entryId = string.IsNullOrWhiteSpace(value) ? DialogueStartNodeData.DefaultEntryId : value.Trim();
		}

		/// <summary>값을 정규화한 대화 참조를 만듭니다.</summary>
		public DialogueReference(DialogueContainer graphAsset, string entryId)
		{
			GraphAsset = graphAsset;
			this.entryId = string.IsNullOrWhiteSpace(entryId)
				? DialogueStartNodeData.DefaultEntryId
				: entryId.Trim();
		}
	}
}
