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


    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void GameStart(PlayerData data)
    {
        playerSpawner.Init();

        string targetScene = string.IsNullOrEmpty(data.currentSceneName) ? SceneName.Main : data.currentSceneName;

        UpdateRoomState(data, targetScene);
    }

    //Scene 상태 업데이트
    public async void UpdateRoomState(PlayerData data, string targetScene)
    {
        await LoadingScene.LoadScene(targetScene);

        playerSpawner.ClearAllPlayers();

        data.currentSceneName = targetScene;
        playerSpawner.LocalPlayerSpawn(data);

        NetworkManager.instance.socket.EmitJoinScene(data, targetScene);
    }

}
