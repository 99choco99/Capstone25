using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState
{
    Gameplay,   // 일반 플레이 상태
    UIMode,     // UI가 열려 플레이어 조작이 멈춘 상태
    Paused      // 일시정지 상태
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    [Header("BGM 설정")]
    [SerializeField] private string mainBgmKey = "BGM_Main";
    [SerializeField] private string combatBgmKey = "BGM_Combat";

    private HashSet<Enemy> enemiesInCombatWithMe = new HashSet<Enemy>();


    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private PlayerMoveSync playerMoveSync;

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

        ChangeState(GameState.Gameplay);
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
        Debug.Log(RemotePlayers.Count);
        foreach(NetworkPlayerData RemotePlayer in RemotePlayers)
        {
            if(RemotePlayer.id != DataManager.Instance.Server_PlayerData.id)
            {
                playerSpawner.RemotePlayerSpawn(RemotePlayer);
            }
        }
    }


    public void ChangeState(GameState state)
    {
        if(CurrentState == state) { return; }
        CurrentState = state;

        switch (state)
        {
            case GameState.Gameplay:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case GameState.UIMode:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }

        OnGameStateChanged?.Invoke(CurrentState);
    }

    public GameState GetGameState()
    {
        return CurrentState;
    }

    public void RegisterEnemyInCombat(Enemy enemy)
    {
        if (enemy == null || !enemiesInCombatWithMe.Add(enemy)) return;

        if (enemiesInCombatWithMe.Count == 1)
        {
            SoundManager.Instance.PlayBGM(combatBgmKey);
        }
    }

    public void UnregisterEnemyInCombat(Enemy enemy)
    {
        // 등록된 적이 아니거나, Enemy가 null이면 무시
        if (enemy == null || !enemiesInCombatWithMe.Remove(enemy)) return;

        // "나"와 싸우는 적이 1명에서 0명이 되는 순간
        if (enemiesInCombatWithMe.Count == 0)
        {
            SoundManager.Instance.PlayBGM(mainBgmKey);
        }
    }

}
