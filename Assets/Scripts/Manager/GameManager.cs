using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Gameplay,   // 일반 플레이 상태
    UIMode,     // UI가 열려 플레이어 조작이 멈춘 상태
    Paused      // 일시정지 상태
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private HashSet<Enemy> enemiesInCombat = new HashSet<Enemy>();
    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    [Header("BGM 설정")]
    [SerializeField] private string mainBgmKey = "BGM_Main";
    [SerializeField] private string combatBgmKey = "BGM_Combat";

    private HashSet<Enemy> enemiesInCombatWithMe = new HashSet<Enemy>();

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
