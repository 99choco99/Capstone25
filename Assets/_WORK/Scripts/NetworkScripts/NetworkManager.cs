using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SocketManager))]
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    [HideInInspector] public APIManager API;
    [HideInInspector] public SocketManager socket;

    [SerializeField] private GameObject playerPrefab;
    private readonly PlayerRepository repository = new();

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            API = new APIManager();
            socket = GetComponent<SocketManager>();

            socket.OnCurrentPlayersReceived += SpawnCurrentPlayers;
            socket.OnRemotePlayerJoined += RemotePlayerSpawn;
            socket.OnRemotePlayerLeft += RemotePlayerDespawn;

        } else
        {
            Destroy(gameObject);
        }
    }

    //Scene 상태 업데이트
    public async void JoinRoom(ServerPlayerData data, string targetSceneName = null)
    {
        string targetScene = string.IsNullOrEmpty(targetSceneName) ? SceneName.Main : targetSceneName;
        repository.ClearAllPlayers();
        await GameManager.Instance.ChangeScene(targetScene);

        data.currentSceneName = targetScene;
        Player.LocalPlayer.gameObject.name = data.nickname;
        socket.EmitJoinScene(data, targetScene);
    }

    //다른 플레이어 스폰
    public void RemotePlayerSpawn(NetworkPlayerData data)
    {
        if (repository.HasPlayer(data.id)) { return; }
        GameObject newPlayer = GameObject.Instantiate(playerPrefab);

        if (newPlayer.TryGetComponent(out Player playerComponent))
        {
            playerComponent.Init(false);
        }

        newPlayer.transform.SetLocalPositionAndRotation(data.position.ToVector3(), data.rotation.ToQuaternion());
        repository.AddPlayer(data.id, newPlayer);
    }

    //다른 플레이어 디스폰
    public void RemotePlayerDespawn(string id)
    {
        repository.RemovePlayer(id);
    }

    public GameObject GetPlayer(string id)
    {
        return id == API.userId ? Player.LocalPlayer?.gameObject : repository.GetPlayer(id);
    }

    //현재 들어와있는 PlayerObj 스폰
    public void SpawnCurrentPlayers(List<NetworkPlayerData> RemotePlayers)
    {
        foreach (NetworkPlayerData RemotePlayer in RemotePlayers)
        {
            if (RemotePlayer.id != API.userId)
            {
                RemotePlayerSpawn(RemotePlayer);
            }
        }
    }
}
