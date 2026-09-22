using System;
using UnityEngine;
using UniversalGraph;
using UniversalGraph.Samples;

/// <summary>서버 연결과 관계없이 게임 시작·씬 이동·로컬 저장을 담당합니다.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private QuestSetup questSetup;
    [SerializeField] private QuestContainer combatQuest;
    public IQuestController QuestController => questSetup;

    [SerializeField] private GameObject playerPrefab;

    private bool isChangingScene;
    private bool isLocalGame;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        PlayerSpawner.Init(playerPrefab);
    }

    /// <summary>저장된 성장, 인벤토리를 불러와 Main에서 시작합니다. 전투 도중 이어하기는 하지 않습니다.</summary>
    public async Awaitable StartGame()
    {
        DataManager.Instance.LoadLocalData();
        await ChangeScene(SceneName.Main);
        Player.LocalPlayer.Stats.RestoreHealth(Player.LocalPlayer.Stats.MaxHp.GetValue());
        isLocalGame = true;
        SaveGame();
    }

    public async Awaitable Retry()
    {
        QuestManager.ResetQuest(Instance.QuestController, combatQuest.QuestId);
        await ChangeScene(SceneName.Main);
        Player.LocalPlayer.Stats.RestoreHealth(Player.LocalPlayer.Stats.MaxHp.GetValue());
        SaveGame();

    }


    [DialogueAction("SceneChagne")]
    public static void ChangeSceneAttribute(string sceneName)
    {
        ChangeSceneFromDialogue(sceneName);
    }


    [QuestAction("AddExp", Owner = QuestMethodOwner.Global)]
    public static void GiveQuestExp(int amount)
    {
        Player.LocalPlayer.Stats.AddExp(amount);
    }


    private static async void ChangeSceneFromDialogue(string sceneName)
    {
        try
        {
            await Instance.ChangeScene(sceneName);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    /// <summary>현재 기록을 유지하며 씬을 바꾸고, 새 씬의 SpawnPoint에 플레이어를 생성</summary>
    public async Awaitable ChangeScene(string sceneName)
    {
        if (isChangingScene)
            throw new InvalidOperationException("이미 씬을 이동 중입니다.");
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
            throw new ArgumentException($"빌드 목록에 없는 씬입니다: {sceneName}", nameof(sceneName));

        isChangingScene = true;
        try
        {
            DataManager.Instance.CapturePlayerData();
            PlayerSpawner.Instance.ClearLocalPlayer();

            await LoadingScene.LoadScene(sceneName);

            PlayerSpawner.Instance.LocalPlayerSpawn();
        }
        finally
        {
            isChangingScene = false;
        }

        if (isLocalGame) SaveGame();
    }

    /// <summary>현재 플레이어와 인벤토리를 저장</summary>
    public bool SaveGame()
    {
        if (!isLocalGame) return false;

        try
        {
            DataManager.Instance.CapturePlayerData();
            DataManager.Instance.SaveLocalData();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"로컬 저장 실패: {exception.Message}");
            return false;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
