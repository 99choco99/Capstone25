using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    private List<Transform> SpawnPoints = new List<Transform>();
    private PlayerRepository repository;

    public void Init(PlayerRepository repository)
    {
        this.repository = repository;
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
        GameObject Player = Instantiate(playerPrefab);

        if (Player.TryGetComponent<Player>(out Player playerComponent))
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

        Player.transform.SetLocalPositionAndRotation(finalPoistion, finalRotation);
        repository.AddPlayer(data.id, Player);
    }

    //다른 플레이어 스폰
    public void RemotePlayerSpawn(NetworkPlayerData data)
    {
        if (repository.HasPlayer(data.id)) { return; }
        GameObject newPlayer = Instantiate(playerPrefab);

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
}
