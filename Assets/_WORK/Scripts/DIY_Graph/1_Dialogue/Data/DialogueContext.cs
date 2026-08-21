using UnityEngine;

namespace UniversalGraph
{
	public class DialogueContext
	{
		public GameObject Speaker { get; }

		public GameObject Interactor { get; }

		public DialogueContext(GameObject speaker, GameObject interactor)
		{
			Speaker = speaker;
			Interactor = interactor;
		}
	}
}
