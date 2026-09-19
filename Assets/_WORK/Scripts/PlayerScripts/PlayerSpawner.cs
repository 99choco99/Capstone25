using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner
{
    public static PlayerSpawner Instance { get; private set; }


    private GameObject playerPrefab;
    private List<Transform> SpawnPoints = new();

    public static void Init(GameObject prefab)
    {
        if (Instance == null) Instance = new PlayerSpawner(prefab);
    }

    private PlayerSpawner(GameObject playerPrefab)
    {
        this.playerPrefab = playerPrefab;
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
    public void LocalPlayerSpawn()
    {
        if (Player.LocalPlayer != null) { return; }
        Vector3 finalPoistion = Vector3.zero;
        Quaternion finalRotation = Quaternion.identity;

        if(SpawnPoints.Count > 0) { 
            int randomIndex = Random.Range(0, SpawnPoints.Count);
            Transform selectedPoint = SpawnPoints[randomIndex];
            
            finalPoistion = selectedPoint.position;
            finalRotation = selectedPoint.rotation;
        }

        GameObject PlayerObj = GameObject.Instantiate(playerPrefab, finalPoistion, finalRotation);
        if (PlayerObj.TryGetComponent<Player>(out Player playerComponent))
        {
            playerComponent.Init(true);
        }
    }

    /// <summary>씬 이동 전에 현재 로컬 플레이어를 제거합니다.</summary>
    public void ClearLocalPlayer()
    {
        if (Player.LocalPlayer != null)
            GameObject.Destroy(Player.LocalPlayer.gameObject);
    }
}
