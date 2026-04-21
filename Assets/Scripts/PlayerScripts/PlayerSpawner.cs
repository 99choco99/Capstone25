using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    private PlayerRepository repository;

    public void Init(PlayerRepository repository)
    {
        this.repository = repository;
    }

    //플레이어 스폰
    public void LocalPlayerSpawn(PlayerData data)
    {
        if (repository.HasPlayer(data.id)) { return; }
        GameObject Player = Instantiate(playerPrefab);
        Player.transform.SetLocalPositionAndRotation(data.ToVector3(), Quaternion.identity);
        repository.AddPlayer(data.id, Player);
    }

    //다른 플레이어 스폰
    public void RemotePlayerSpawn(NetworkPlayerData data)
    {
        if (repository.HasPlayer(data.id)) { return; }
        GameObject newPlayer = Instantiate(playerPrefab);
        newPlayer.transform.SetLocalPositionAndRotation(data.position.ToVector3(), data.rotation.ToQuaternion());
        repository.AddPlayer(data.id, newPlayer);
    }

    //다른 플레이어 디스폰
    public void RemotePlayerDespawn(string id)
    {
        repository.RemovePlayer(id);
    }
}
