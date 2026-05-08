using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] private PlayerSpawner playerSpawner;
    public PlayerSpawner PlayerSpawner { get { return playerSpawner; } }
    private PlayerRepository playerRepository;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            playerRepository = new PlayerRepository();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    public void GameStart(PlayerData data)
    {
        playerSpawner.Init(playerRepository);

        NetworkManager.instance.socket.OnCurrentPlayersReceived += HandleCurrentPlayers;
        NetworkManager.instance.socket.OnRemotePlayerJoined += playerSpawner.RemotePlayerSpawn;
        NetworkManager.instance.socket.OnRemotePlayerLeft += playerSpawner.RemotePlayerDespawn;

        string targetScene = string.IsNullOrEmpty(data.currentSceneName) ? SceneName.Main : data.currentSceneName;

        UpdateRoomState(data, targetScene);
    }

    //Scene 상태 업데이트
    public async void UpdateRoomState(PlayerData data, string targetScene)
    {
        await LoadingScene.LoadScene(targetScene);

        playerRepository.ClearAllPlayers();

        data.currentSceneName = targetScene;
        Debug.Log($"현재 활성화된 씬: {SceneManager.GetActiveScene().name}");
        playerSpawner.LocalPlayerSpawn(data);

        NetworkManager.instance.socket.EmitJoinScene(data, targetScene);
    }

    public void HandleCurrentPlayers(List<NetworkPlayerData> RemotePlayers)
    {
        foreach(NetworkPlayerData RemotePlayer in RemotePlayers)
        {
            if(RemotePlayer.id != DataManager.Instance.Server_PlayerData.id)
            {
                playerSpawner.RemotePlayerSpawn(RemotePlayer);
            }
        }
    }

}
