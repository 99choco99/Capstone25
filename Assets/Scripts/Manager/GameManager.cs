using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

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

}
