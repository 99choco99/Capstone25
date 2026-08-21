using UnityEngine;

namespace UniversalGraph
{
	/// <summary>
	/// Drives time-based dialogue nodes from Unity's frame loop.
	/// The object is created only when a <see cref="WaitNodeData"/> needs it.
	/// </summary>
	internal sealed class DialogueRuntimeDriver : MonoBehaviour
	{
		private const string DriverObjectName = "[UniversalGraph Runtime]";

		private static DialogueRuntimeDriver instance;

		[RuntimeInitializeOnLoadMethod]
		private static void ResetStaticState()
		{
			instance = null;
		}

		/// <summary>
		/// Ensures that exactly one persistent driver exists while a dialogue waits for time.
		/// </summary>
		internal static void Ensure()
		{
			if (Application.isPlaying && !(instance != null))
			{
				instance = Object.FindAnyObjectByType<DialogueRuntimeDriver>((FindObjectsInactive)1);
				if (!(instance != null))
				{
				GameObject val = new GameObject(DriverObjectName)
					{
						hideFlags = (HideFlags)1
					};
					val.AddComponent<DialogueRuntimeDriver>();
				}
			}
		}

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				UnityEngine.Object.Destroy(((Component)this).gameObject);
				return;
			}
			instance = this;
			UnityEngine.Object.DontDestroyOnLoad(((Component)this).gameObject);
		}

		private void OnDestroy()
		{
			if ((object)instance == (object)this)
			{
				instance = null;
			}
		}

		private void Update()
		{
			DialogueManager.Instance.Tick(Time.deltaTime, Time.unscaledDeltaTime);
		}
	}
}





