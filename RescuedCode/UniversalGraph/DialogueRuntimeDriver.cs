using UnityEngine;

namespace UniversalGraph
{
	internal sealed class DialogueRuntimeDriver : MonoBehaviour
	{
		private const string DriverObjectName = "[UniversalGraph Runtime]";

		private static DialogueRuntimeDriver instance;

		[RuntimeInitializeOnLoadMethod]
		private static void ResetStaticState()
		{
			instance = null;
		}

		internal static void Ensure()
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Expected O, but got Unknown
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Expected O, but got Unknown
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Expected O, but got Unknown
			if (Application.isPlaying && !((Object)instance != (Object)null))
			{
				instance = Object.FindAnyObjectByType<DialogueRuntimeDriver>((FindObjectsInactive)1);
				if (!((Object)instance != (Object)null))
				{
					GameObject val = new GameObject("[UniversalGraph Runtime]")
					{
						hideFlags = (HideFlags)1
					};
					val.AddComponent<DialogueRuntimeDriver>();
				}
			}
		}

		private void Awake()
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Expected O, but got Unknown
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Expected O, but got Unknown
			if ((Object)instance != (Object)null && (Object)instance != (Object)this)
			{
				Object.Destroy((Object)((Component)this).gameObject);
				return;
			}
			instance = this;
			Object.DontDestroyOnLoad((Object)((Component)this).gameObject);
		}

		private void OnDestroy()
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Expected O, but got Unknown
			//IL_0016: Expected O, but got Unknown
			if ((Object)instance == (Object)this)
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
