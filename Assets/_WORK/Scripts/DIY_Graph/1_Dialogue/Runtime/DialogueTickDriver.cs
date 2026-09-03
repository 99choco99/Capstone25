using UnityEngine;

namespace UniversalGraph
{
	/// <summary>
	/// Unity 프레임 루프에서 시간 기반 대화 노드를 갱신합니다.
	/// <see cref="DialogueWaitNodeData"/>가 필요할 때만 객체를 생성합니다.
	/// </summary>
	internal sealed class DialogueTickDriver : MonoBehaviour
	{
		private const string DriverObjectName = "[UniversalGraph Runtime]";

		private static DialogueTickDriver instance;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			instance = null;
		}

		/// <summary>
		/// 대화가 시간을 기다리는 동안 유지되는 드라이버가 정확히 하나만 존재하도록 보장합니다.
		/// </summary>
		internal static void Ensure()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (instance == null)
			{
				instance = Object.FindAnyObjectByType<DialogueTickDriver>(FindObjectsInactive.Include);
			}

			if (instance != null)
			{
				instance.enabled = true;
				instance.gameObject.SetActive(true);
				return;
			}

			var driverObject = new GameObject(DriverObjectName)
			{
				hideFlags = HideFlags.HideInHierarchy
			};
			driverObject.AddComponent<DialogueTickDriver>();
		}

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				Destroy(gameObject);
				return;
			}
			instance = this;
			DontDestroyOnLoad(gameObject);
		}

		private void OnDestroy()
		{
			if (instance == this)
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





