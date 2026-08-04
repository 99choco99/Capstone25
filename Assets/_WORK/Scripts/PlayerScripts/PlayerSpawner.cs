using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner
{
    public static PlayerSpawner Instance { get; private set; }


    private GameObject playerPrefab;
    private List<Transform> SpawnPoints = new List<Transform>();
    private PlayerRepository repository;

    public static void Init(GameObject prefab, SocketManager socket)
    {
        if (Instance == null) Instance = new PlayerSpawner(prefab, socket);
    }

    private PlayerSpawner(GameObject playerPrefab, SocketManager socket)
    {
        this.playerPrefab = playerPrefab;
        repository = new PlayerRepository();

        socket.OnCurrentPlayersReceived += SpawnCurrentPlayers;
        socket.OnRemotePlayerJoined += RemotePlayerSpawn;
        socket.OnRemotePlayerLeft += RemotePlayerDespawn;
    }
    public void RegisterSpawnPoint(Transform point)
    {
        if (!SpawnPoints.Contains(point))
        {
            SpawnPoints.Add(point);
        }
    }

    public void UnregisterSpawnPoint(Transform point)
    {
        if (SpawnPoints.Contains(point))
        {
            SpawnPoints.Remove(point);
        }
    }

    //플레이어 스폰
    public void LocalPlayerSpawn(PlayerData data)
    {
        if (repository.HasPlayer(data.id)) { return; }
        GameObject PlayerObj = GameObject.Instantiate(playerPrefab);

        if (PlayerObj.TryGetComponent<Player>(out Player playerComponent))
        {
            playerComponent.Init(true);
        }

        Vector3 finalPoistion = Vector3.zero;
        Quaternion finalRotation = Quaternion.identity;

        if(SpawnPoints.Count > 0) { 
            int randomIndex = Random.Range(0, SpawnPoints.Count);
            Transform selectedPoint = SpawnPoints[randomIndex];
            
            finalPoistion = selectedPoint.position;
            finalRotation = selectedPoint.rotation;
        }

        PlayerObj.transform.SetLocalPositionAndRotation(finalPoistion, finalRotation);
        repository.AddPlayer(data.id, PlayerObj);
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

    public void ClearAllPlayers()
    {
        repository.ClearAllPlayers();
    }

    public GameObject GetPlayer(string id)
    {
        return repository.GetPlayer(id);
    }

    //현재 들어와있는 PlayerObj 스폰
    public void SpawnCurrentPlayers(List<NetworkPlayerData> RemotePlayers)
    {
        foreach (NetworkPlayerData RemotePlayer in RemotePlayers)
        {
            if (RemotePlayer.id != DataManager.Instance.Server_PlayerData.id)
            {
                RemotePlayerSpawn(RemotePlayer);
            }
        }
    }
}
