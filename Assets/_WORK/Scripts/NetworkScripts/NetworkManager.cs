using UnityEngine;

[RequireComponent(typeof(SocketManager))]
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    [HideInInspector] public APIManager API;
    [HideInInspector] public SocketManager socket;

    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            API = new APIManager();
            socket = GetComponent<SocketManager>();

            PlayerSpawner.Init(playerPrefab, socket);

        } else
        {
            Destroy(gameObject);
        }
    }

    //Scene 상태 업데이트
    public async void JoinRoom(PlayerData data, string targetSceneName = null)
    {
        string targetScene = string.IsNullOrEmpty(targetSceneName) ? SceneName.Main : targetSceneName;
        if (PlayerSpawner.Instance != null)
        {
            PlayerSpawner.Instance.ClearAllPlayers();
        }

        await LoadingScene.LoadScene(targetScene);

        if (PlayerSpawner.Instance != null)
        {
            data.currentSceneName = targetScene;
            PlayerSpawner.Instance.LocalPlayerSpawn(data);
        }

        socket.EmitJoinScene(data, targetScene);
    }
}
