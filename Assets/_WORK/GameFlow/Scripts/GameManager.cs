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
    public bool IsChangingScene => isChangingScene;

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

    /// <summary>정상적인 기존 성장 기록을 불러와 Main에서 시작합니다.</summary>
    public async Awaitable ContinueGame()
    {
        ValidateSceneChange(SceneName.Main);
        DataManager.Instance.LoadSavedGame();
        await StartLocalGame();
    }

    /// <summary>성장과 실행 중 퀘스트 진행을 초기화하고 Main에서 새 게임을 시작합니다.</summary>
    public async Awaitable StartNewGame()
    {
        ValidateSceneChange(SceneName.Main);
        if (questSetup == null)
            throw new InvalidOperationException("새 게임을 시작할 QuestSetup이 연결되지 않았습니다.");

        DataManager.Instance.ResetLocalData();
        questSetup.QuestProgress.Clear();
        await StartLocalGame();
    }

    private async Awaitable StartLocalGame()
    {
        isLocalGame = false;
        await ChangeScene(SceneName.Main, false);
        isLocalGame = true;
        SaveGame();
    }

    public async Awaitable Retry()
    {
        ValidateSceneChange(SceneName.Main);
        QuestManager.ResetQuest(Instance.QuestController, combatQuest.QuestId);
        await ChangeScene(SceneName.Main);
    }

    public Awaitable Respawn()
    {
        return ChangeScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public async Awaitable ReturnToTitle()
    {
        ValidateSceneChange(SceneName.Login);
        isChangingScene = true;
        try
        {
            if (isLocalGame && !SaveGame())
                throw new InvalidOperationException("진행 기록을 저장하지 못해 타이틀 이동을 취소했습니다.");

            DialogueManager.Instance.CancelConversation();
            MainUIManager.Instance.ReleasePlayerUI();
            PlayerSpawner.Instance.ClearLocalPlayer();
            isLocalGame = false;
            await LoadingScene.LoadScene(SceneName.Login);
        }
        finally
        {
            isChangingScene = false;
        }
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
    public Awaitable ChangeScene(string sceneName)
    {
        return ChangeScene(sceneName, true);
    }

    private void ValidateSceneChange(string sceneName)
    {
        if (isChangingScene)
            throw new InvalidOperationException("이미 씬을 이동 중입니다.");
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
            throw new ArgumentException($"빌드 목록에 없는 씬입니다: {sceneName}", nameof(sceneName));
    }

    private async Awaitable ChangeScene(string sceneName, bool capturePlayerData)
    {
        ValidateSceneChange(sceneName);

        isChangingScene = true;
        try
        {
            if (capturePlayerData)
                DataManager.Instance.CapturePlayerData();
            PlayerSpawner.Instance.ClearLocalPlayer();

            await LoadingScene.LoadScene(sceneName);

            PlayerSpawner.Instance.LocalPlayerSpawn();
            Player.LocalPlayer.Stats.RestoreHealth(Player.LocalPlayer.Stats.MaxHp.GetValue());
        }
        finally
        {
            isChangingScene = false;
        }

        if (isLocalGame)
            SaveGame();
    }

    /// <summary>현재 플레이어의 성장 기록을 저장</summary>
    public bool SaveGame()
    {
        if (!isLocalGame)
            return false;

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
        if (paused)
            SaveGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
